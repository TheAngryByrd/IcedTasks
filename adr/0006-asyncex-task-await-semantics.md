# 0006. Give `asyncEx` task-await semantics that differ from FSharp.Core `async`

- Status: accepted
- Date: 2023-07-04
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/24, https://github.com/TheAngryByrd/IcedTasks/pull/24#discussion_r1556182882, https://github.com/TheAngryByrd/IcedTasks/pull/46
- Reason source: PR review on #24 that #46 cites, and confirmed by maintainer on 2026-10-26 for the unwrap

FSharp.Core `Async.AwaitTask` throws the `AggregateException` of a faulted task.
Thus `try ... with :? MyException` in `async` does not match the real exception.

`asyncEx` awaits a task through `AwaitAwaiter` in `src/IcedTasks/AsyncEx.fs`.
It unwraps an `AggregateException` that has one inner exception, and it passes the inner exception to the error continuation (#24).
It also passes a cancelled task to the error continuation. #24 used the cancel continuation, and #46 changed this.

The unwrap lets the handler see the same exception that `task` code sees.
For the cancelled task, a reviewer on #24 wrote: "My general conclusion is that we're better off having AwaitTaskCorrect, which has been very well battle proven, with specific failing tests covering any deviation from its entire behavior".
A code comment in `AwaitAwaiter` quotes FSharp.Core. The comment says that a task cancellation does not always mean that the whole `async` is cancelled.
