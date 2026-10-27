# 0002. Start `Async` values with `Async.StartImmediateAsTask`

- Status: accepted
- Date: 2022-03-23
- Source: https://github.com/TheAngryByrd/IcedTasks/issues/6, https://github.com/TheAngryByrd/IcedTasks/pull/7, https://github.com/TheAngryByrd/IcedTasks/pull/17
- Reason source: issue, PR description, and confirmed by maintainer on 2026-10-26 for #17

The builders must start an `Async` value when they bind it or run it with `and!`.
`Async.StartChild` and `Async.StartAsTask` run the work on a thread-pool thread.

We start `Async` values with `Async.StartImmediateAsTask`.
`parallelAsync` is `parallelAsyncUsingStartImmediateAsTask` (#7), and `parallelAsyncUsingStartChild` stays as an option.
The task builders start a bound `Async` with `Async.StartImmediateAsTask`. #17 replaced `Async.StartAsTask` there.

Issue #6 links a discussion that "seems to have issues with using `StartChild` as it uses the threadpool implicitly."
#7 says "Take away is `Async.StartImmediateAsTask` seems to out perform `Async.StartChild` which is probably due to `Async.StartChild` running the computation on the threadpool."
For #17, `Async.StartAsTask` moved the work to a thread-pool thread, with an extra thread switch, and ran it off the caller's `SynchronizationContext`.

## Consequences

The decision covers bound `Async` values and the `parallelAsync` operands.
The `AsCancellableValueTask` converters in `CancellableValueTask.fs` and `CancellablePoolingValueTask.fs` still use `Async.StartAsTask`.
