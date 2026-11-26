# 0016. Add a tail-recursive cancellable task builder

- Status: proposed
- Date: 2026-11-25
- Source: the pull request that adds `src/IcedTasks/TailRecursiveCancellableTask.fs`
- Reason source: PR description, and measurements with a prototype on the .NET 10 runtime

In `cancellableTask`, `return!` is a bind. A recursive loop whose awaits complete synchronously runs each iteration inside the previous one. It adds stack frames and keeps each state machine alive until the loop ends. A loop of about 1,000 synchronous iterations overflows a 512 KB thread stack. F# `async` does not have this problem, because `return!` in `async` is a tail call.

We add `tailRecursiveCancellableTask` (`src/IcedTasks/TailRecursiveCancellableTask.fs`), a builder on `CancellableTaskBuilderBase` whose `return!` of a `CancellableTask<'T>` is a tail call.

- The method builder is `TailRecursiveCancellableTaskMethodBuilder<'T>`: an `AsyncTaskMethodBuilder<TailRecursiveStep<'T>>` and a tail-call slot. The body of one computation expression is one step. `return!` of a `CancellableTask<'T>` writes the next step to the slot and completes the body. `SetResult` completes the step task with the next step when the slot is set, and with the result otherwise.
- `Run` returns a `TailRecursiveCancellableTask<'T>`, a sealed subclass of `FSharpFunc<CancellationToken, Task<'T>>`. It is a `CancellableTask<'T>` at run time. Its `Invoke` runs the chain of steps in one loop, so stack and heap stay constant. A `return!` of another `TailRecursiveCancellableTask<'T>` continues with its first step. A `return!` of any other `CancellableTask<'T>` awaits it as a step.
- `Combine`, `While`, `For`, `TryWith`, `TryFinally`, and `Using` hide the base members and run a pending tail call to completion inside their scope, so `try`, `finally`, `use`, and loops keep the semantics of `cancellableTask` and `async`. These members are generic in `'Builder` to keep the base signatures, and they read the slot through `Unsafe.As`, as `MethodBuilder.AwaitUnsafeOnCompletedTyped` in `TaskLike.fs` does. `Run` fixes `'Builder`, so the view is exact.
- The builder has a static path and a dynamic path (`RunDynamic`), as the other cold builders do.
- The result type is the existing `CancellableTask<'T>` alias, so the `CancellableTask` module functions and the `Source` members of the other builders accept the value.
- The file is under `#if NET6_0_OR_GREATER`, because `Unsafe.As` is not in `netstandard2.0` and `netstandard2.1` without a package reference.

## Why a separate builder

`cancellableTask` cannot make `return!` a tail call. Its body completes a `Task<'T>`, and a `Task<'T>` cannot say "continue with another computation". A tail call needs a step type in the method builder, and the type of the code that `Run` accepts changes with it. A separate builder keeps `cancellableTask` as it is.

## Considered options

- A `while` loop in `cancellableTask`. This works when the loop state has one type. It does not express mutual recursion or a loop whose state changes type, and it does not fix existing recursive code.
- `Task.Yield()` in each iteration. This bounds the stack but not the heap, and it moves each iteration to the thread pool.
- Make `return!` a tail call in `cancellableTask` itself. See "Why a separate builder".
- A `netstandard2.0` build. This needs `System.Runtime.CompilerServices.Unsafe` or a boxed copy of the method builder on each check for a pending tail call.

## Consequences

- A `return!` inside `try`, `use`, `for`, or `while` is not a tail call, as in `async`. The documentation shows how to keep the recursive call outside those scopes.
- On suspension, the method builder gets the awaiter as `ICriticalNotifyCompletion`, so a struct awaiter is boxed. `MethodBuilder.AwaitUnsafeOnCompletedTyped` handles only the .NET method builders with their own type.
- `Combine`, `While`, `For`, `TryWith`, `TryFinally`, and `Using` have a small extra cost for the check of the slot.
- The builder has no `and!`.
- The public API gains the types in `IcedTasks.TailRecursiveCancellableTasks`, listed in `tests/IcedTasks.Tests/PublicApi/additions-net6.0.txt`, `additions-net9.0.txt`, and `additions-net11.0.txt`.
