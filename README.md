# IcedTasks

## What is IcedTasks?

This library contains additional [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) for the [task CE](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) utilizing the [Resumable Code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) introduced [in F# 6.0](https://devblogs.microsoft.com/dotnet/whats-new-in-fsharp-6/#making-f-faster-and-more-interopable-with-task).

- `ValueTask<'T>` - This utilizes .NET's [ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) (which is essentially a [Discriminated Union](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) of `'Value | Task<'Value>`) for possibly better performance in synchronous scenarios. Similar to [F#'s Task Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions)

- `ColdTask<'T>` - Alias for `unit -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async).

- `CancellableTask<'T>` - Alias for `CancellationToken -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `CancellableValueTask<'T>` - Alias for `CancellationToken -> ValueTask<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `ParallelAsync<'T>` - Utilizes the [applicative syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) to allow parallel execution of [Async<'T> expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions). See [this discussion](https://github.com/dotnet/fsharp/discussions/11043) as to why this is a separate computation expression.

- `AsyncEx<'T>` - Slight variation of F# async semantics described further below with examples.


### Differences at a glance

| Computation Expression<sup>1</sup> | Library<sup>2</sup> | TFM<sup>3</sup> | Hot/Cold<sup>4</sup> | Multiple Awaits <sup>5</sup> | Multi-start<sup>6</sup> | Tailcalls<sup>7</sup> | CancellationToken propagation<sup>8</sup> | Cancellation checks<sup>9</sup> | Parallel when using and!<sup>10</sup> | use IAsyncDisposable <sup>11</sup> |
|------------------------------------|---------------------|-----------------|----------------------|------------------------------|-------------------------|-----------------------|-------------------------------------------|---------------------------------|---------------------------------------|------------------------------------|
| F# Async                           | FSharp.Core         | netstandard2.0  | Cold                 | Multiple                     | multiple                | tailcalls             | implicit                                  | implicit                        | No                                    | No                                 |
| F# AsyncEx                         | IcedTasks           | netstandard2.0  | Cold                 | Multiple                     | multiple                | tailcalls             | implicit                                  | implicit                        | No                                    | Yes                                |
| F# ParallelAsync                   | IcedTasks           | netstandard2.0  | Cold                 | Multiple                     | multiple                | tailcalls             | implicit                                  | implicit                        | Yes                                   | No                                 |
| F# Task/C# Task                    | FSharp.Core         | netstandard2.0  | Hot                  | Multiple                     | once-start              | no tailcalls          | explicit                                  | explicit                        | No                                    | Yes                                |
| F# ValueTask                       | IcedTasks           | netstandard2.1  | Hot                  | Once                         | once-start              | no tailcalls          | explicit                                  | explicit                        | Yes                                   | Yes                                |
| F# ColdTask                        | IcedTasks           | netstandard2.0  | Cold                 | Multiple                     | multiple                | no tailcalls          | explicit                                  | explicit                        | Yes                                   | Yes                                |
| F# CancellableTask                 | IcedTasks           | netstandard2.0  | Cold                 | Multiple                     | multiple                | no tailcalls          | implicit                                  | implicit                        | Yes                                   | Yes                                |
| F# CancellableValueTask            | IcedTasks           | netstandard2.1  | Cold                 | Once                         | multiple                | no tailcalls          | implicit                                  | implicit                        | Yes                                   | Yes                                |

- <sup>1</sup> - [Computation Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
- <sup>2</sup> - Which [Nuget](https://www.nuget.org/) package do they come from
- <sup>3</sup> - Which [Target Framework Moniker](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) these are available in
- <sup>4</sup> - Hot refers to the asynchronous code block already been started and will eventually produce a value. Cold refers to the asynchronous code block that is not started and must be started explicitly by caller code. See [F# Async Tutorial](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async) and [Asynchronous C# and F# (II.): How do they differ?](http://tomasp.net/blog/async-csharp-differences.aspx/) for more info.
- <sup>5</sup> - [ValueTask Awaiting patterns](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/#valid-consumption-patterns-for-valuetasks)
- <sup>6</sup> - Multi-start refers to being able to start the asynchronous code block again.  See [FAQ on Task Start](https://devblogs.microsoft.com/pfxteam/faq-on-task-start/#:~:text=Question%3A%20Can%20I%20call%20Start,will%20result%20in%20an%20exception.) for more info.
- <sup>7</sup> - Allows use of `let rec` with the computation expression. See [Tail call Recursion](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/recursive-functions-the-rec-keyword#tail-recursion) for more info.
- <sup>8</sup> - `CancellationToken` is propagated to all types the support implicit `CancellatationToken` passing. Calling `cancellableTask { ... }` nested inside `async { ... }` (or any of those combinations) will use the `CancellationToken` from when the code was started.
- <sup>9</sup> - Cancellation will be checked before binds and runs.
- <sup>10</sup> - Allows parallel execution of the asynchronous code using the [Applicative Syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) in computation expressions. 
- <sup>11</sup> - Allows `use` of `IAsyncDisposable` with the computation expression. See [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable) for more info.

## Why should I use this?


### AsyncEx

AsyncEx is similar to Async except in the following ways:

1. Allows `use` for [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)

    ```fsharp
    open IcedTasks
    let fakeDisposable = { new IAsyncDisposable with member __.DisposeAsync() = ValueTask.CompletedTask }

    let myAsyncEx = asyncEx {
        use! _ = fakeDisposable
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

Make sure the following **requirements** are installed on your system:

- [dotnet SDK](https://www.microsoft.com/net/download/core) 7.0 or higher

or

- [VSCode Dev Container](https://code.visualstudio.com/docs/remote/containers)

---

### Environment Variables

- `CONFIGURATION` will set the [configuration](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x#options) of the dotnet commands.  If not set, it will default to Release.
  - `CONFIGURATION=Debug ./build.sh` will result in `-c` additions to commands such as in `dotnet build -c Debug`
- `GITHUB_TOKEN` will be used to upload release notes and Nuget packages to GitHub.
  - Be sure to set this before releasing
- `DISABLE_COVERAGE` Will disable running code coverage metrics.  AltCover can have [severe performance degradation](https://github.com/SteveGilham/altcover/issues/57) so it's worth disabling when looking to do a quicker feedback loop.
  - `DISABLE_COVERAGE=1 ./build.sh`

---

### Building

```sh
> build.cmd <optional buildtarget> // on windows
$ ./build.sh  <optional buildtarget>// on unix
```

The bin of your library should look similar to:

```bash
$ tree src/MyCoolNewLib/bin/
src/MyCoolNewLib/bin/
└── Debug
    └── net50
        ├── MyCoolNewLib.deps.json
        ├── MyCoolNewLib.dll
        ├── MyCoolNewLib.pdb
        └── MyCoolNewLib.xml

```

---

### Build Targets

- `Clean` - Cleans artifact and temp directories.
- `DotnetRestore` - Runs [dotnet restore](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- [`DotnetBuild`](#Building) - Runs [dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `DotnetTest` - Runs [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `GenerateCoverageReport` - Code coverage is run during `DotnetTest` and this generates a report via [ReportGenerator](https://github.com/danielpalme/ReportGenerator).
- `WatchTests` - Runs [dotnet watch](https://docs.microsoft.com/en-us/aspnet/core/tutorials/dotnet-watch?view=aspnetcore-3.0) with the test projects. Useful for rapid feedback loops.
- `GenerateAssemblyInfo` - Generates [AssemblyInfo](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.applicationservices.assemblyinfo?view=netframework-4.8) for libraries.
- `DotnetPack` - Runs [dotnet pack](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-pack). This includes running [Source Link](https://github.com/dotnet/sourcelink).
- `SourceLinkTest` - Runs a Source Link test tool to verify Source Links were properly generated.
- `PublishToNuGet` - Publishes the NuGet packages generated in `DotnetPack` to NuGet via [paket push](https://fsprojects.github.io/Paket/paket-push.html).
- `GitRelease` - Creates a commit message with the [Release Notes](https://fake.build/apidocs/v5/fake-core-releasenotes.html) and a git tag via the version in the `Release Notes`.
- `GitHubRelease` - Publishes a [GitHub Release](https://help.github.com/en/articles/creating-releases) with the Release Notes and any NuGet packages.
- `FormatCode` - Runs [Fantomas](https://github.com/fsprojects/fantomas) on the solution file.
- `BuildDocs` - Generates Documentation from `docsSrc` and the [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/) from your libraries in `src`.
- `WatchDocs` - Generates documentation and starts a webserver locally.  It will rebuild and hot reload if it detects any changes made to `docsSrc` files, libraries in `src`, or the `docsTool` itself.
- `ReleaseDocs` - Will stage, commit, and push docs generated in the `BuildDocs` target.
- [`Release`](#Releasing) - Task that runs all release type tasks such as `PublishToNuGet`, `GitRelease`, `ReleaseDocs`, and `GitHubRelease`. Make sure to read [Releasing](#Releasing) to setup your environment correctly for releases.

---

### Releasing

- [Start a git repo with a remote](https://help.github.com/articles/adding-an-existing-project-to-github-using-the-command-line/)

```sh
git add .
git commit -m "Scaffold"
git remote add origin https://github.com/user/MyCoolNewLib.git
git push -u origin master
```

- [Create your NuGeT API key](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys)
  - [Add your NuGet API key to paket](https://fsprojects.github.io/Paket/paket-config.html#Adding-a-NuGet-API-key)

  ```sh
  paket config add-token "https://www.nuget.org" 4003d786-cc37-4004-bfdf-c4f3e8ef9b3a
  ```

  - or set the environment variable `NUGET_TOKEN` to your key

- [Create a GitHub OAuth Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
  - You can then set the environment variable `GITHUB_TOKEN` to upload release notes and artifacts to github
  - Otherwise it will fallback to username/password

- Then update the `CHANGELOG.md` with an "Unreleased" section containing release notes for this version, in [KeepAChangelog](https://keepachangelog.com/en/1.1.0/) format.

NOTE: Its highly recommend to add a link to the Pull Request next to the release note that it affects. The reason for this is when the `RELEASE` target is run, it will add these new notes into the body of git commit. GitHub will notice the links and will update the Pull Request with what commit referenced it saying ["added a commit that referenced this pull request"](https://github.com/TheAngryByrd/MiniScaffold/pull/179#ref-commit-837ad59). Since the build script automates the commit message, it will say "Bump Version to x.y.z". The benefit of this is when users goto a Pull Request, it will be clear when and which version those code changes released. Also when reading the `CHANGELOG`, if someone is curious about how or why those changes were made, they can easily discover the work and discussions.

Here's an example of adding an "Unreleased" section to a `CHANGELOG.md` with a `0.1.0` section already released.

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

[Unreleased]: https://github.com/user/MyCoolNewLib.git/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/user/MyCoolNewLib.git/releases/tag/v0.1.0
```

- You can then use the `Release` target, specifying the version number either in the `RELEASE_VERSION` environment
  variable, or else as a parameter after the target name.  This will:
  - update `CHANGELOG.md`, moving changes from the `Unreleased` section into a new `0.2.0` section
    - if there were any prerelease versions of 0.2.0 in the changelog, it will also collect their changes into the final 0.2.0 entry
  - make a commit bumping the version:  `Bump version to 0.2.0` and adds the new changelog section to the commit's body
  - publish the package to NuGet
  - push a git tag
  - create a GitHub release for that git tag

macOS/Linux Parameter:

```sh
./build.sh Release 0.2.0
```

macOS/Linux Environment Variable:

```sh
RELEASE_VERSION=0.2.0 ./build.sh Release
```
