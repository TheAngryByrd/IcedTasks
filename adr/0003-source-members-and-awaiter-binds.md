# 0003. Convert bound values with `Source` members, and bind awaiters on their own path

- Status: accepted
- Date: 2022-12-01
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/12, https://github.com/TheAngryByrd/IcedTasks/pull/25
- Reason source: PR description

The builders bind many kinds of values: tasks, awaiters, `Async`, `ColdTask`, and `CancellableTask`.
A separate `Bind` overload for each kind makes each resumable builder large.

We convert bound values with `Source` member overloads in `LowPriority` and `HighPriority` modules (#12).
In the cancellable builders, `Source` returns an awaiter unchanged, and a `Bind` overload for `'Awaiter` takes it without a token function (#25).
The `Bind` overload for `CancellationToken -> 'Awaiter` handles the cancellable values.
`docsSrc/Explanations/Why-is-there-different-binds.md` explains the result for users.

#12 says "This uses the `Source` member overloads to provide a more stripped down version of the ColdTask and CancellableTask CEs."
#25 says "Previously I was using Source members to move `Awaiter/TaskAwaiter` to a `CancellationToken -> 'Awaiter/TaskAwaiter` but this was causing overhead."
