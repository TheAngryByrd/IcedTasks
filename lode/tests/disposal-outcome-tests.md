# DisposeAsync outcome tests

`tests/IcedTasks.Tests/AsyncDisposalOutcome.fs` holds a test matrix for the result of `use` on an `IAsyncDisposable` and `for` over an `IAsyncEnumerable`. `IcedTasks.Tests`, NS20, and NS21 compile it, after `Expect.fs`. Each test file of a builder that supports `use` of an `IAsyncDisposable` adds a `disposalOutcomeTests` list to its root `[<Tests>]` list. `ParallelAsyncTests.fs` has none, because `parallelAsync` forwards `Using` to `async`.

## Contract that the tests pin

- For the resumable builders, a `DisposeAsync` fault or cancellation decides the result of the builder value. It replaces the body result and the body exception. For `asyncEx`, see Expected results.
- The builder calls `DisposeAsync` exactly once and reads its result exactly once. `ObservedDisposalSource` counts `IValueTaskSource.GetResult` calls.
- The builder awaits a pending `DisposeAsync`. The test checks that the result is not complete when `OnCompleted` registers a continuation, and only then completes the source.
- A cancellable builder awaits the disposal also after the ambient token cancels. This pins `BindNoCancellation` in `CancellableTaskBuilderBase.TryFinallyAsync`.

## Matrix

Each case is one combination of site, outcome, completion, and body:

| Axis | Values |
|---|---|
| site | `use`, `for` (`for` is absent for `coldTask` and `backgroundColdTask`, which have no `IAsyncEnumerable` support) |
| outcome | `DisposalSucceeds`, `DisposalFaults`, `DisposalCancels` |
| completion | `CompletedDisposal` (source completes before start), `PendingDisposal` |
| body | `BodyCompletes`, `BodyThrows`, and `BodyCancels` for the cancellable builders |
| execution | `Static` (the builder), `Dynamic` (a test builder that takes `RunDynamic`), or `AsyncWorkflow` (`asyncEx`, which has no resumable code) |

`BodyCancels` cancels the ambient token and then binds a completed task, so the cancellable `Bind` throws. The test name gives each axis, for example `cancellableTask, Dynamic, for, DisposalFaults, pending disposal, BodyCancels`.

```fsharp
// tests/IcedTasks.Tests/CancellableTaskTests.fs
                (fun body cts resource ->
                    cancellableTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts
                        do! Task.CompletedTask
                    }
                    |> fun t -> t cts.Token :> Task
                )
```

`backgroundTask` has no dynamic test builder, so it has only `Static` cases. `TaskTests.fs`, `TaskBackgroundTests.fs`, and `ValueTaskTests.fs` define `RunDynamic` test builders for `taskUnit`, `backgroundTaskUnit`, and `valueTaskUnit`. The `Static` cases take the static path only in Release. See [run and start paths](../builders/run-and-start-paths.md).

## Expected results

- `taskExpectation` is the contract above, for all resumable builders.
- `asyncExpectation` is the `asyncEx` result. A dispose `OperationCanceledException` faults the result. A body exception and a dispose exception give one `AggregateException`. See [AsyncEx](../builders/asyncex-and-parallelasync.md).

## Regression tests next to the matrix

- `AsyncExTests.fs`: `use` and `use!` dispose the resource when the body throws before its first bind. `do!` of a completed `IValueTaskSource` ValueTask reads `GetResult` once.
- `ValueTaskTests.fs` and `PoolingValueTaskTests.fs`: `Async.AwaitValueTask` and `ofUnit` read `GetResult` of a completed non-generic ValueTask once. A faulted input fails the result and does not throw at the call. `AsyncExTests.fs` has the same check for `AsyncEx.AwaitValueTask`.
- `TaskTests.fs`, `TaskBackgroundTests.fs`, `ValueTaskTests.fs`: each unit builder completes on the dynamic path when its body completes synchronously. `MethodBuilder.SetResult` completes a non-generic builder, and `MethodBuilder.get_Task` before `SetResult` returns the task that `SetResult` completes. These tests can fail only in Debug, because the copy that they detect happens only in unoptimized code. See [practices](../practices.md).

```mermaid
flowchart LR
    case["case: site, outcome, completion, body"] --> src["ObservedDisposalSource"]
    src -->|"ValueTask from DisposeAsync"| builder["builder value"]
    builder --> obs["observe result task status"]
    src --> count["GetResult count = 1"]
    obs --> exp["taskExpectation or asyncExpectation"]
```

Related: [test helpers](expect-helpers.md), [tests summary](summary.md), [test projects and TFMs](test-projects-and-tfms.md).
