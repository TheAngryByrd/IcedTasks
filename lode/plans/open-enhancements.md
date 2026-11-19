# Open enhancements

The open GitHub issues with the `enhancement` label are the open work. #3 and #4 also have the `help wanted` label. No roadmap file exists.

| Issue | Title | Request |
|---|---|---|
| [#4](https://github.com/TheAngryByrd/IcedTasks/issues/4) | Implement async2 | Finish the partial FSharp.Core `async2` implementation, which uses resumable code. |
| [#3](https://github.com/TheAngryByrd/IcedTasks/issues/3) | Tail Recursion | Add tail call support to the task builders, as `async` has. |
| [#74](https://github.com/TheAngryByrd/IcedTasks/issues/74) | asyncEx: an enclosing use can dispose before an early inner DisposeAsync completes | Track early compensations per execution, so an enclosing `use` or `for` in `asyncEx` waits for them. See [ADR 0013](../../adr/0013-asyncex-compensation-before-computation-starts.md). |

#3 and #4 list "Don't do it" as an alternative, and neither has a design. #74 describes a possible design with ambient per-execution state.

## Related code

The existing resumable builders use this `Run` shape, with a static path and a dynamic path:

```fsharp
// src/IcedTasks/CancellableTask.fs (CancellableTaskBuilder.Run)
        member inline _.Run(code: CancellableTaskBaseCode<'T, 'T, _>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskBaseStateMachineData<'T, _>, CancellableTask<'T>>
```

```mermaid
flowchart LR
    i4["#4 async2"] --> shape["resumable builder: Run, RunDynamic"]
    i3["#3 tail recursion"] --> bind["Bind and ReturnFrom of task builders"]
    shape --> tests["tests in all 4 projects"]
    bind --> tests
```

Related: [run and start paths](../builders/run-and-start-paths.md), [builder catalog](../builders/builder-catalog.md).
