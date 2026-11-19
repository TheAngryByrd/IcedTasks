# 0013. Start the `asyncEx` compensation when the workflow is cancelled before the computation starts

- Status: accepted
- Date: 2026-11-18
- Source: https://github.com/TheAngryByrd/IcedTasks/issues/72
- Reason source: decision by the maintainer on 2026-11-18, after a review that found the nested disposal order problem

`Async.TryFinallyAsync` in `src/IcedTasks/AsyncEx.fs` implements `use` and `for` in `asyncEx`.
If the cancellation token is cancelled after `use` makes the resource and before the computation starts, the workflow cancels.
Before #72, the compensation (`DisposeAsync`) did not run in this case.

FSharp.Core checks the token before each public step that could reach the computation: `Delay`, `Bind`, and `Async.FromContinuations` (`MakeAsyncWithCancelCheck` in FSharp.Core `async.fs`).
`Async.TryCancelled` does not check the token on entry, but its handler is synchronous.
So no public `Async` step can wait for an asynchronous compensation in this window, at the outermost scope.
`AsyncPrimitives.MakeAsync` enters without a check, but FSharp.Core gives no public error continuation or trampoline for user code.

Decision:

- `Async.TryFinallyAsync` wraps its entry in `Async.TryCancelled`.
  The handler starts the compensation with `Async.StartWithContinuations` and `CancellationToken.None`.
- A `ConditionalWeakTable` maps each cancellation exception to its compensation state.
  A cancellation that already ran the compensation is not handled a second time.
  An early compensation is stored as a task under the cancellation exception.
- An enclosing `TryFinallyAsync` that receives that cancellation waits for the stored task before its own compensation.
  If the inner compensation failed, the enclosing workflow fails with that exception.
- At the outermost scope, nothing waits for the early compensation, and its exception is discarded.

Rejected options:

- Start the compensation and never wait for it.
  In a nested `use`, the outer resource can then close while the inner `DisposeAsync` still runs.
- Wait through `AsyncPrimitives.MakeAsync` at every scope.
  It needs FSharp.Core internals for exception routing and thread safety.
- Block the thread on the compensation in the handler.
  This is sync-over-async and can deadlock on a synchronization context.

Consequences:

- `TryFinallyAsync` is no longer `inline`, because it uses the private table.
  Code that an earlier version compiled has the old inlined body until it is compiled again.
- A `use!` resource is owned by the workflow only after its source returns it.
  If the source itself is cancelled first, the workflow never sees the resource and does not dispose it. FSharp.Core `async` has the same rule.
