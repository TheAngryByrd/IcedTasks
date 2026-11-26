---
title: Tail-recursive cancellable task
category: Explanations
categoryindex: 3
index: 8
---

# Tail-recursive cancellable task

`tailRecursiveCancellableTask` builds a `CancellableTask<'T>`, as `cancellableTask` does. The difference is `return!` of a `CancellableTask<'T>`: in `tailRecursiveCancellableTask`, it is a tail call. The builder is in the `net6.0` and later builds of IcedTasks.

## The problem

A recursive loop is the usual F# shape for a message loop or a state machine:

```fsharp
let rec loop (state: int) : CancellableTask<unit> =
    cancellableTask {
        let! message = receive ()
        return! loop (state + message)
    }
```

In `cancellableTask`, as in `task`, `return! loop (...)` is a bind: the body awaits the task of the inner call and then returns its result. When the awaits complete synchronously, for example because the message queue is not empty, no step suspends. Each iteration then runs inside the previous one:

- Each iteration adds stack frames. A loop over a queue of about 1,000 messages overflows a 512 KB thread stack.
- Each iteration keeps its state machine and its task alive until the loop ends, because each one waits for the next one. The live heap grows with the depth of the loop.

F# `async` does not have this problem: `return!` in `async` is a tail call. `cancellableTask` cannot make `return!` a tail call without a change to its result type, because a `Task<'T>` cannot say "continue with this other computation".

## What the builder does

```fsharp
let rec loop (state: int) : CancellableTask<unit> =
    tailRecursiveCancellableTask {
        let! message = receive ()
        return! loop (state + message)
    }
```

The body of a `tailRecursiveCancellableTask` is one step. A step completes with a result, or with the next step to run. `return!` of a `CancellableTask<'T>` does not await it. It gives the next step to the method builder and completes the body.

The value of the expression is a `CancellableTask<'T>`. When you call it with a token, it runs the steps in one loop. A step that completes synchronously adds no stack frame, and the loop does not keep a completed step. Thus the loop above runs in constant stack and constant heap, also for one million synchronous iterations.

`return!` of a `CancellableTask<'T>` that is not a `tailRecursiveCancellableTask` is also a tail call: the loop awaits that task as a step. `return!` of a `Task<'T>`, a `ValueTask<'T>`, an `Async<'T>`, or a non-generic `CancellableTask` is a bind, as in `cancellableTask`.

## Semantics

The builder keeps the semantics of `cancellableTask` and of `async` for code that runs after a sub-computation:

- In `try ... with`, `try ... finally`, `use`, `use!`, `for`, and `while`, and before code that follows an `if` or `match`, the builder runs a pending tail call to completion inside that scope. `with` sees the exceptions of the callee, `finally` and `Dispose` run after the callee, and each loop iteration runs its own callee.
- A `return!` in those positions is not a tail call. Each such iteration adds a stack frame when the callee completes synchronously, as in `async`. Put the recursive call outside the `try` or `use` to keep it a tail call:

```fsharp
let rec loop (state: int) : CancellableTask<unit> =
    tailRecursiveCancellableTask {
        let! message = receive ()

        let next =
            try
                handle state message
            with e ->
                log e
                state

        return! loop next
    }
```

- A `return!` in a `with` handler is a tail call. The handler runs outside the protected region.
- The ambient token flows through each step. A step checks the token before it starts, as `cancellableTask` does, and the result is a cancelled task when the token is cancelled.
- The value starts again on each call, as each cold builder value does.

## Costs

- `Combine`, `While`, `For`, `TryWith`, `TryFinally`, and `Using` check for a pending tail call after their sub-computation. This check is a small cost for each use of these constructs.
- Each step allocates its own `Task<TailRecursiveStep<'T>>` and state machine, as each `cancellableTask` call does. The per-iteration allocation is similar to `cancellableTask`.
- On suspension, the method builder gets the awaiter as an `ICriticalNotifyCompletion`, so a struct awaiter is boxed. The .NET method builders of the other builders get the awaiter with its own type.
- The builder has no `and!`.

## When to use it

Use `tailRecursiveCancellableTask` for a recursive loop whose awaits often complete synchronously: a message loop, an actor, a parser over a buffered stream, or a retry loop. Use `cancellableTask` for other cancellable work. A `while` loop in `cancellableTask` is an alternative when the loop has no state that changes type between iterations.
