---
title: Tail-recursive cancellable task
category: Explanations
categoryindex: 3
index: 8
---

# Tail-recursive cancellable task

`tailRecursiveCancellableTask` builds a `CancellableTask<'T>`, as `cancellableTask` does. In `tailRecursiveCancellableTask`, `return!` of a `CancellableTask<'T>` is a tail call, as in `async`. The builder is in the `net6.0` and later builds.

## The problem

```fsharp
let rec loop (state: int) : CancellableTask<unit> =
    cancellableTask {
        let! message = receive ()
        return! loop (state + message)
    }
```

In `cancellableTask`, `return!` is a bind. When `receive` completes synchronously, each iteration runs inside the previous one. Each iteration adds stack frames and keeps its state machine alive until the loop ends. About 1,000 iterations overflow a 512 KB thread stack.

With `tailRecursiveCancellableTask` in place of `cancellableTask`, the same loop runs in constant stack and constant heap.

## Semantics

- `return!` of a `CancellableTask<'T>` is a tail call. `return!` of a `Task<'T>`, `ValueTask<'T>`, `Async<'T>`, or non-generic `CancellableTask` is a bind, as in `cancellableTask`.
- `return!` inside `try`, `use`, `for`, or `while` is not a tail call, as in `async`. The callee runs to completion inside that scope, so `with` sees its exceptions and `finally` runs after it. Keep the recursive call outside the scope:

```fsharp
let rec loop (state: int) : CancellableTask<unit> =
    tailRecursiveCancellableTask {
        let! message = receive ()
        let next = try handle state message with e -> state
        return! loop next
    }
```

- `return!` in a `with` handler is a tail call.
- Each step checks the token before it starts, as `cancellableTask` does.

## Costs

- `Combine`, `While`, `For`, `TryWith`, `TryFinally`, and `Using` check for a pending tail call.
- On suspension, a struct awaiter is boxed.
- The builder has no `and!`.

Use `cancellableTask` for other work. If the loop state has one type, a `while` loop in `cancellableTask` is also an option.
