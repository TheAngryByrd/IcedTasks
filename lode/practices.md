# Practices

## Code conventions

- `Directory.Build.props` sets `TreatWarningsAsErrors`. A new warning fails the build.
- `Directory.Build.props` sets `LangVersion` 8.0, and 9.0 with `Nullable` enabled for `net9.0` ([ADR 0011](../adr/0011-fsharp-core-per-target-framework.md)). Use the `Nullness.fs` types (`ExceptionNull`, `IDisposableNull`, `IAsyncDisposableNull`) for values that can be null. The comment in `src/IcedTasks/Nullness.fs` says that nullness works only for FSharp.Core 9 and later.
- Fantomas formats all F# code. A plain `dotnet build` runs Fantomas on F# projects outside `src/`. See [formatting and analyzers](build/formatting-and-analyzers.md).
- `.editorconfig` sets LF, 4-space indent, `max_line_length=100`, and `fsharp_multiline_bracket_style = stroustrup`.
- Central package management is on (`Directory.Packages.props`). Put versions there, not in a project file.
- A builder member that a builder needs for type inference goes on the builder type, not in an extension module. The code comment says "on the real type to allow type inference to work" (`src/IcedTasks/CancellableTask.fs`).
- Each resumable builder has a static path and a dynamic path. A change to one path needs the same change to the other. See [run and start paths](builders/run-and-start-paths.md).
- Do not put `[<InlineIfLambda>]` on resumable code parameters, such as `task1` and `task2` in `Combine` and `computation` in `TryWith`. Plain lambda parameters, such as `guard` and `compensation`, have it. Commit d89d479 removed it from resumable code: "This was causing excessive compile times without any performance benefits".
- Complete a struct method builder in place. In unoptimized code, an SRTP call `builder.SetResult()` on a `byref<'Builder>` completes a copy of the builder, so the task never completes when the body finishes before its first await. `MethodBuilder.SetResult` and `MethodBuilder.get_Task` in `TaskLike.fs` use explicit trait calls, such as `(^Builder: (member SetResult: unit -> unit) builder)`, which act on the builder in place. The calls with an argument, such as `SetResult(&builder, result)`, do not copy.
- Read `GetResult` of a completed non-generic `ValueTask` (`vtask.GetAwaiter().GetResult()`). `AsyncEx.AwaitValueTask`, `Async.AwaitValueTask`, and both `ofUnit` functions do this.
- Do not add an assembly-level `AutoOpen`, and keep each builder module in `namespace IcedTasks`. A public API change must update `tests/IcedTasks.Tests/PublicApi/IcedTasks-0.11.9-<tfm>.txt` ([ADR 0013](../adr/0013-restore-autoopen-modules-in-icedtasks-namespace.md)).
- Call `SetException` outside the `try ... with` unwind. The code links roslyn issue 26567 for this rule.

```fsharp
// src/IcedTasks/CancellableTask.fs
                        with exn ->
                            __stack_exn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match __stack_exn with
                        | null -> ()
                        | exn -> MethodBuilder.SetException(&sm.Data.MethodBuilder, exn)
```

## Gotchas

- With .NET SDK 10.0.400 and later, Debug builds take the dynamic path for every resumable builder. Test both Debug and Release.
- `./build.sh` uses Debug unless the `Publish` target runs or `CONFIGURATION` is set (`build/build.fs`).
- The docs pages reference `src/IcedTasks/bin/Release/net9.0/IcedTasks.dll`. Build Release before `BuildDocs`. See [fsdocs build](docs/fsdocs-build.md).
- The pooling builders exist only under `NET6_0_OR_GREATER`. Code that uses them must also compile for `netstandard2.0` and `netstandard2.1`, or sit under the same guard.
- `dotnet build IcedTasks.sln` also builds `build/`, the benchmarks, and `examples/ILSpySamples` (`IcedTasks.sln`).
- Untracked `.claude/worktrees/` copies of the repository can exist. Exclude them from searches.
- `CHANGELOG.md` uses Keep a Changelog. Add a PR link and credit to each entry, because the `Release` target puts the notes in the commit body (`README.md`, "Releasing").
- Changes that touch only tests, the build, or docs usually get no changelog entry. A change to how the package is published gets one.

## Change checklist

```mermaid
flowchart LR
    edit["Edit a builder"] --> both["Edit static and dynamic paths"]
    both --> tfm["Check net6.0+ guards and netstandard builds"]
    tfm --> tests["Run tests in Debug and Release"]
    tests --> fmt["Fantomas check passes"]
    fmt --> log["Add CHANGELOG entry with PR link"]
```

Related: [build summary](build/summary.md), [tests summary](tests/summary.md), [builders summary](builders/summary.md).
