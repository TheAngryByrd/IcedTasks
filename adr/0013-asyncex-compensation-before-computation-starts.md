# 0013. Start the `asyncEx` compensation when the workflow is cancelled before the computation starts

- Status: accepted
- Date: 2026-11-18
- Source: https://github.com/TheAngryByrd/IcedTasks/issues/72
- Reason source: decision by the maintainer on 2026-11-18, after two review rounds

`Async.TryFinallyAsync` in `src/IcedTasks/AsyncEx.fs` implements `use` and `for` in `asyncEx`.
If the cancellation token is cancelled after `use` makes the resource and before the computation starts, the workflow cancels.
Before #72, the compensation (`DisposeAsync`) did not run in this case.

FSharp.Core checks the token before each public step that could reach the computation: `Delay`, `Bind`, and `Async.FromContinuations` (`MakeAsyncWithCancelCheck` in FSharp.Core `async.fs`).
`Async.TryCancelled` does not check the token on entry, but its handler is synchronous.
So no public `Async` step can wait for an asynchronous compensation in this window.
`AsyncPrimitives.MakeAsync` enters without a check, but FSharp.Core gives no public error continuation or trampoline for user code.

Decision:

- `Async.TryFinallyAsync` wraps its entry in `Async.TryCancelled`.
- The handler starts the compensation with `Async.StartWithContinuations` and `CancellationToken.None`. Nothing waits for the rest of it, and its exception is discarded.
- A `ConditionalWeakTable` marks each cancellation exception that already ran the compensation, so the handler does not run it a second time. A null cancellation exception is never a key.

Rejected options:

- Store the early compensation under the cancellation exception, and let an enclosing `TryFinallyAsync` wait for it.
  The key is not tied to one execution: another run that passes the same exception object waits for this cleanup and gets its failure.
  An `Async.Parallel` branch that a faulting sibling cancels has a different exception object, so the parent does not wait.
  The wait also moved the enclosing compensation to a thread pool thread.
- Track early compensations per execution with ambient state (for example `AsyncLocal`) and wait in each enclosing scope.
  It is more complex, and it cannot make a synchronous `use` or `try ... finally` wait. https://github.com/TheAngryByrd/IcedTasks/issues/74 tracks it.
- Wait through `AsyncPrimitives.MakeAsync`. It needs FSharp.Core internals for exception routing and thread safety.
- Block the thread on the compensation in the handler. This is sync-over-async and can deadlock on a synchronization context.

Consequences:

- In a nested `use`, an enclosing resource can be disposed before an early inner `DisposeAsync` completes.
- A caller that sees the cancelled result cannot wait for an early `DisposeAsync` that is still running.
- `TryFinallyAsync` is no longer `inline`, because it uses the private table.
  Code that an earlier version compiled has the old inlined body until it is compiled again.
- A `use!` resource belongs to the workflow only after its source returns it.
  If the source itself is cancelled first, the workflow never sees the resource and does not dispose it. FSharp.Core `async` has the same rule.
