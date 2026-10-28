# IcedTasks

## What is IcedTasks?

This library contains additional [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) for the [task CE](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) utilizing the [Resumable Code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) introduced [in F# 6.0](https://devblogs.microsoft.com/dotnet/whats-new-in-fsharp-6/#making-f-faster-and-more-interopable-with-task).

- `ValueTask<'T>` - This utilizes .NET's [ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) (which is essentially a [Discriminated Union](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) of `'Value | Task<'Value>`) for possibly better performance in synchronous scenarios. Similar to [F#'s Task Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions)
    - `valueTask`
    - `valueTaskUnit`
    - `poolingValueTask`

- `ColdTask<'T>` - Alias for `unit -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async).
    - `coldTask`

- `CancellableTask<'T>` - Alias for `CancellationToken -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).
    - `cancellableTask`
    - `backgroundCancellableTask`

- `CancellableValueTask<'T>` - Alias for `CancellationToken -> ValueTask<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).
    - `cancellableValueTask`
    - `cancellablePoolingValueTask`

- `ParallelAsync<'T>` - Utilizes the [applicative syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) to allow parallel execution of [Async<'T> expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions). See [this discussion](https://github.com/dotnet/fsharp/discussions/11043) as to why this is a separate computation expression.
    - `parallelAsync`

- `AsyncEx<'T>` - Variation of F# async semantics described further below with examples.
    - `asyncEx`
    - This can also be accessed under `IcedTasks.Polyfill.Async` which will [shadow](https://stackoverflow.com/a/39645319) the F# Async CE.
      - `async`

- `Task<'T>` - Polyfill for fixes to F# Task CE. Can be accessed under `IcedTasks.Polyfill.Task` which will [shadow](https://stackoverflow.com/a/39645319) the F# Task CE.
    - `task`
    - `backgroundTask`

- `Task` - a CE for a `Task` that has no return value.
    - `taskUnit`
    - `backgroundTaskUnit`

### Differences at a glance

See [Choosing a builder](https://www.jimmybyrd.me/IcedTasks/Explanations/Choosing-a-builder.html) for a builder-by-builder comparison, including aliases, background builders, unit builders, and .NET 6+ pooling builders.

## Why should I use this?


### AsyncEx

AsyncEx is similar to Async except in the following ways:

1. Allows `use` for [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)

    ```fsharp
    open IcedTasks
    let fakeDisposable () = { new IAsyncDisposable with member __.DisposeAsync() = ValueTask.CompletedTask }

    let myAsyncEx = asyncEx {
        use _ = fakeDisposable ()
        return 42
    }
    ````
2. Allows `let!/do!` against Tasks/ValueTasks/[any Awaitable](https://devblogs.microsoft.com/pfxteam/await-anything/)

    ```fsharp
    open IcedTasks
    let myAsyncEx = asyncEx {
        let! _ = task { return 42 } // Task<T>
        let! _ = valueTask { return 42 } // ValueTask<T>
        let! _ = Task.Yield() // YieldAwaitable
        return 42
    }
    ```
3. When Tasks throw exceptions they will use the behavior described in [Async.Await overload (esp. AwaitTask without throwing AggregateException](https://github.com/fsharp/fslang-suggestions/issues/840)


    ```fsharp
    let data = "lol"

    let inner = asyncEx {
        do!
            task {
                do! Task.Yield()
                raise (ArgumentException "foo")
                return data
            }
            :> Task
    }

    let outer = asyncEx {
        try
            do! inner
            return ()
        with
        | :? ArgumentException ->
            // Should be this exception and not AggregationException
            return ()
        | ex ->
            return raise (Exception("Should not throw this type of exception", ex))
    }
    ```

4. Use [IAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-8.0) with `for` keyword. This example uses [TaskSeq](https://github.com/fsprojects/FSharp.Control.TaskSeq) but you can use any `IAsyncEnumerable<T>`.


    ```fsharp
    open IcedTasks
    open FSharp.Control
    let myAsyncEx = asyncEx {
        let items = taskSeq {  // IAsyncEnumerable<T>
            yield 42
            do! Task.Delay(100)
            yield 1701
        }
        let mutable sum = 0
        for i in items do
            sum <- sum + i
        return sum
    }
    ```

### For [ValueTasks](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)

- F# doesn't currently have a `valueTask` computation expression. [Until this PR is merged.](https://github.com/dotnet/fsharp/pull/14755)


```fsharp
open IcedTasks

let myValueTask = task {
    let! theAnswer = valueTask { return 42 }
    return theAnswer
}
```

### For Cold & CancellableTasks
- You want control over when your tasks are started
- You want to be able to re-run these executable tasks
- You don't want to pollute your methods/functions with extra CancellationToken parameters
- You want the computation to handle checking cancellation before every bind.


### ColdTask

Short example:

```fsharp
open IcedTasks

let coldTask_dont_start_immediately = task {
    let mutable someValue = null
    let fooColdTask = coldTask { someValue <- 42 }
    do! Async.Sleep(100)
    // ColdTasks will not execute until they are called, similar to how Async works
    Expect.equal someValue null ""
    // Calling fooColdTask will start to execute it
    do! fooColdTask ()
    Expect.equal someValue 42 ""
}

```

### CancellableTask & CancellableValueTask

The examples show `cancellableTask` but `cancellableValueTask` can be swapped in.

Accessing the context's CancellationToken:

1. Binding against `CancellationToken -> Task<_>`

    ```fsharp
    let writeJunkToFile = 
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                // You can do! directly against a function with the signature of `CancellationToken -> Task<_>` to access the context's `CancellationToken`. This is slightly more performant.
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)
        }
    ```

2. Binding against `CancellableTask.getCancellationToken`

    ```fsharp
    let writeJunkToFile = 
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            // You can bind against `CancellableTask.getCancellationToken` to get the current context's `CancellationToken`.
            let! ct = CancellableTask.getCancellationToken ()
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
    ```

Short example:

```fsharp
let executeWriting = task {
    // CancellableTask is an alias for `CancellationToken -> Task<_>` so we'll need to pass in a `CancellationToken`.
    // For this example we'll use a `CancellationTokenSource` but if you were using something like ASP.NET, passing in `httpContext.RequestAborted` would be appropriate.
    use cts = new CancellationTokenSource()
    // call writeJunkToFile from our previous example
    do! writeJunkToFile cts.Token
}


```

### ParallelAsync

- When you want to execute multiple asyncs in parallel and wait for all of them to complete.

Short example:

```fsharp
open IcedTasks

let exampleHttpCall url = async {
    // Pretend we're executing an HttpClient call
    return 42
}

let getDataFromAFewSites = parallelAsync {
    let! result1 = exampleHttpCall "howManyPlantsDoIOwn"
    and! result2 = exampleHttpCall "whatsTheTemperature"
    and! result3 = exampleHttpCall "whereIsMyPhone"

    // Do something meaningful with results
    return ()
}

```

---

## Builds

GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/TheAngryByrd/IcedTasks/workflows/Build%20master/badge.svg)](https://github.com/TheAngryByrd/IcedTasks/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/TheAngryByrd/IcedTasks)](https://github.com/TheAngryByrd/IcedTasks/actions?query=branch%3Amaster) |

## NuGet

Package | Stable | Prerelease
--- | --- | ---
IcedTasks | [![NuGet Badge](https://buildstats.info/nuget/IcedTasks)](https://www.nuget.org/packages/IcedTasks/) | [![NuGet Badge](https://buildstats.info/nuget/IcedTasks?includePreReleases=true)](https://www.nuget.org/packages/IcedTasks/)

---

### Developing

Install one of these:

- The [.NET SDK](https://dotnet.microsoft.com/download) version that `global.json` specifies. The test projects also run on .NET 8.0 and .NET 9.0, so install those SDKs or runtimes too. CI installs the 8.x, 9.x, and 10.x SDKs.
- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers). The dev container installs .NET 10.0, 9.0, and 8.0.

---

### Environment Variables

- `CONFIGURATION` sets the [configuration](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build#options) of the dotnet commands. The default is `Debug`. When the `Publish` target runs, the default is `Release`.
  - `CONFIGURATION=Release ./build.sh` adds `-c Release` to commands such as `dotnet build`.
- `CI` makes the build run in CI mode. Restore retries on failure, `CheckFormatCode` only checks the format, and `PublishToNuGet` and `GitHubRelease` can run.
- `RELEASE_VERSION` sets the version for the `Release` target. See [Releasing](#releasing).
- `NUGET_TOKEN` is the API key that `PublishToNuGet` uses. The publish workflow gets a short-lived key through [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).
- `GITHUB_TOKEN` is the token that `GitHubRelease` uses to create the GitHub release and upload the NuGet packages.

---

### Building

```sh
> build.cmd <optional buildtarget> // on windows
$ ./build.sh  <optional buildtarget>// on unix
```

The default target is `DotnetPack`. It runs `Clean`, `DotnetRestore`, `CheckFormatCode`, `DotnetBuild`, `DotnetTest`, and `DotnetPack`. Off CI, `CheckFormatCode` formats the source files in place. The build writes the library to `src/IcedTasks/bin/<configuration>/<target framework>/IcedTasks.dll` for each target framework, and the packages to `dist/`.

---

### Build Targets

- `Clean` - Cleans the `bin`, `temp`, and `dist` directories, and the `bin` and `obj` directories of the source and test projects.
- `DotnetRestore` - Runs [dotnet restore](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore) on the solution file.
- `UpdateChangelog` - Moves the `Unreleased` changes in `CHANGELOG.md` into a new version entry. `Release` uses it.
- `CheckFormatCode` - On CI, runs [Fantomas](https://github.com/fsprojects/fantomas) with `--check` and fails if a file needs formatting. Locally, formats the code.
- [`DotnetBuild`](#building) - Runs [dotnet build](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) on the solution file.
- `FSharpAnalyzers` - Runs the F# analyzers from `packages/analyzers` on the source projects. The build does not create that directory. The analyzers also run after each build of `src`, and their warnings do not fail the build.
- `DotnetTest` - Runs [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test) on the solution file.
- `WatchTests` - Runs [dotnet watch](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch) test on each test project.
- `GenerateAssemblyInfo` - Generates `AssemblyInfo.fs` for the source projects.
- `DotnetPack` - Runs [dotnet pack](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack) and writes the packages to `dist/`.
- `PublishToNuGet` - Pushes the packages in `dist/` to NuGet with `dotnet nuget push`. It runs only on CI.
- `GitRelease` - Commits `CHANGELOG.md` and `AssemblyInfo.fs` with the release notes in the commit body, pushes the commit, and pushes a git tag for the version. It runs only on the `master` branch.
- `GitHubRelease` - Publishes a [GitHub Release](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases) with the release notes and the NuGet packages. It runs only on CI.
- `FormatCode` - Runs [Fantomas](https://github.com/fsprojects/fantomas) on all F# files.
- `BuildDocs` - Generates the documentation from `docsSrc` and the [XML documentation comments](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/xml-documentation) in `src`. The documentation pages reference the `Release` build, so set `CONFIGURATION=Release`.
- `CleanDocsCache` - Cleans the fsdocs cache. `BuildDocs` runs it first.
- `WatchDocs` - Generates the documentation and starts a local web server. It rebuilds when files in `docsSrc` or the projects in `src` change.
- [`Release`](#releasing) - Runs `UpdateChangelog`, `GenerateAssemblyInfo`, and `GitRelease`. Use it on a local machine.
- `Publish` - Runs the default target chain, `GenerateAssemblyInfo`, `PublishToNuGet`, and `GitHubRelease`. The publish workflow runs it on CI.

---

### Releasing

A release has two steps. A maintainer runs the `Release` target on a local machine. The pushed tag then starts the publish workflow, which runs the `Publish` target on CI.

1. Update `CHANGELOG.md`. Add an "Unreleased" section with the release notes for this version, in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

NOTE: Add a link to the pull request next to each release note. The `Release` target adds the new notes to the body of the git commit. GitHub then shows on the pull request that a commit referenced it, for example ["added a commit that referenced this pull request"](https://github.com/TheAngryByrd/MiniScaffold/pull/179#ref-commit-837ad59). The build script writes the commit message "Bump version to x.y.z". A reader of a pull request can then see which version released the change. A reader of the `CHANGELOG` can find the discussion behind each change.

This example adds an "Unreleased" section to a `CHANGELOG.md` that already has a released `0.1.0` section:

```markdown
## [Unreleased]

### Added
- Does cool stuff!

### Fixed
- Fixes that silly oversight

## [0.1.0] - 2017-03-17
First release

### Added
- This release already has lots of features

[Unreleased]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0
```

2. On the `master` branch, run the `Release` target. Give the version in the `RELEASE_VERSION` environment variable or as a parameter after the target name. The target does these steps:
  - It updates `CHANGELOG.md` and moves the changes from the `Unreleased` section into a new `0.2.0` section.
    - If the changelog has prerelease versions of 0.2.0, it also collects their changes into the final 0.2.0 entry.
  - It makes the commit `Bump version to 0.2.0` with the new changelog section in the commit body.
  - It pushes the commit and the git tag `v0.2.0`.

macOS/Linux Parameter:

```sh
./build.sh Release 0.2.0
```

macOS/Linux Environment Variable:

```sh
RELEASE_VERSION=0.2.0 ./build.sh Release
```

3. The tag push starts `.github/workflows/publish.yml`. The workflow gets a NuGet API key through [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) and runs `./build.sh Publish`. The `Publish` target builds, tests, and packs the library, publishes the packages to NuGet, and creates a GitHub release for the tag.
