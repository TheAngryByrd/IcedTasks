---
title: Recursion and stack safety
category: Explanations
categoryindex: 3
index: 6
---

# Recursion and stack safety

The cold builders make deep recursion safe when the recursive call is a bind.
This page describes which calls are safe and how to change a call that is not safe.

## Which calls are safe

The cold builders are `coldTask`, `cancellableTask`, `cancellableValueTask`, and `cancellablePoolingValueTask`.
A cold value does not start until something calls it.
When a builder binds a cold value, the builder starts the value.
The builder can then stop the start when the stack is low, go back to a shallow stack, and continue on the same thread.

A call such as `f ct` starts the value at once and returns a task.
The builder receives only that task, so this mechanism cannot operate.
Recursion through this call uses the stack at each level and can overflow the stack.

The hot builders, such as `task` and `valueTask`, start in `Run`.
Recursion through a hot builder can overflow the stack.

| Recursive call | Stack safe |
|---|---|
| `let! x = f (n - 1)` with a cold value | yes |
| `return! f (n - 1)` with a cold value | yes |
| `let! x = f (n - 1) ct` | no |
| `return! f (n - 1) ct` | no |
| `(f (n - 1) ()).Result` | no |
| Recursion in `task` or `valueTask` | no |

## How to change an unsafe call

In a cancellable builder, bind the value and do not give it the token.
The builder gives its token to the value.

```fsharp
let rec countDown n : CancellableTask<int> =
    cancellableTask {
        if n = 0 then
            return 0
        else
            // Do not write: return! countDown (n - 1) ct
            return! countDown (n - 1)
    }
```

In a `coldTask` builder, use `CancellableTask.toColdTask` to bind a `CancellableTask`.
A plain lambda such as `fun () -> f (n - 1) ct` does not help, because the lambda starts the value at once.

```fsharp
let rec coldSide n : ColdTask<int> =
    coldTask {
        if n = 0 then
            return 0
        else
            return!
                cancellableSide (n - 1)
                |> CancellableTask.toColdTask CancellationToken.None
    }

and cancellableSide n : CancellableTask<int> =
    cancellableTask { if n = 0 then return 0 else return! coldSide (n - 1) }
```

## Limits

A builder does not leave the stack when `SynchronizationContext.Current` is set, or when `TaskScheduler.Current` is not the default scheduler.
With a context or a scheduler, recursion can overflow the stack.

A tail `return!` does not overflow the stack, but each level keeps its memory until the recursion ends.
Do not use a tail `return!` for a loop that does not end.

## Stack traces

A failure on a chain of fewer than 64 levels keeps a frame for each level.
On a deeper chain, the stack trace has the first 64 levels and the last level.
This keeps the cost of a deep failure low.
