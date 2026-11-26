# 0016. Add a tail-recursive cancellable task builder

- Status: proposed
- Date: 2026-11-25
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/89
- Reason source: PR description, and measurements with a prototype on the .NET 10 runtime

In `cancellableTask`, `return!` is a bind. A recursive loop whose awaits complete synchronously adds stack frames for each iteration and keeps each state machine alive until the loop ends. About 1,000 iterations overflow a 512 KB stack. In `async`, `return!` is a tail call.

We add `tailRecursiveCancellableTask` (`src/IcedTasks/TailRecursiveCancellableTask.fs`) on `CancellableTaskBuilderBase`.

- One body is one step. `return!` of a `CancellableTask<'T>` writes the next step to a slot in the method builder, and `SetResult` gives that step in place of a result.
- `Run` returns `TailRecursiveCancellableTask<'T>`, a subclass of `FSharpFunc<CancellationToken, Task<'T>>`, so the value is a `CancellableTask<'T>`. Its `Invoke` runs the steps in one loop.
- `Combine`, `While`, `For`, `TryWith`, `TryFinally`, and `Using` run a pending tail call to completion inside their scope, as in `async`. They read the slot through `Unsafe.As`, as `MethodBuilder.AwaitUnsafeOnCompletedTyped` does. So the file is `net6.0` and later only.

## Considered options

- Change `cancellableTask`. A `Task<'T>` cannot give "continue with this computation", so the type of the code that `Run` accepts must change.
- A `while` loop in `cancellableTask`. This does not express mutual recursion or state that changes type.
- `Task.Yield()` in each iteration. This bounds the stack, not the heap.

## Consequences

- `return!` inside `try`, `use`, `for`, or `while` is not a tail call.
- On suspension, a struct awaiter is boxed.
- The builder has no `and!`.
