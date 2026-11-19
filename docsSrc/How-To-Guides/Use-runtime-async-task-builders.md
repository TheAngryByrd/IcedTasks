---
title: Use the runtime-async task builders
category: How To Guides
categoryindex: 2
index: 12
---

# How to use the runtime-async task builders

This guide shows how to use the runtime-async builders of IcedTasks: `task`, `backgroundTask`, `valueTask`, `coldTask`, `backgroundColdTask`, `cancellableTask`, `backgroundCancellableTask`, and `cancellableValueTask`.

In a runtime-async method, the .NET 11 runtime suspends and resumes the method. The F# compiler does not generate a state machine for the method. Runtime async is a preview feature of F# 11. For the design and the limits, see [Runtime-async task builders](../Explanations/Runtime-async-task-builders.html).

## Before you start

You must have:

- The .NET 11 SDK.
- A project that targets `net11.0` or later.
- A version of IcedTasks that contains the `net11.0` build.

The program must run on .NET 11 or later.

## 1. Set up the project file

1. Set the target framework to `net11.0`.
2. Set `LangVersion` to `preview`.
3. Add the IcedTasks package.

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>preview</LangVersion>
</PropertyGroup>
```

The `net11.0` build of IcedTasks depends on FSharp.Core 11.0.101 or later. The .NET 11 SDK gives this version by default. If your project sets an older FSharp.Core version, the restore stops with error NU1605. To correct the error, remove the version reference or set FSharp.Core 11.0.101 or later.

## 2. Open the namespace

Open `IcedTasks.Polyfill.RuntimeTask` in the module that must use the builders.

```fsharp
module Orders =
    open System.IO
    open IcedTasks.Polyfill.RuntimeTask

    let readLength (path: string) =
        task {
            let! text = File.ReadAllTextAsync path
            return text.Length
        }
```

In this module, the eight builder names now give the runtime-async builders. The `open` replaces the FSharp.Core `task` and `backgroundTask` builders, and the IcedTasks builders with the same names. Code in other modules continues to use the builders that it opens.

Put the `open` in a module, not at the top of a large file. Then the reader can see which code uses runtime async.

Always open the namespace. Most `Source` members of the builders are extension members in the namespace, so they work only after the `open`. If you write the full name of a builder without the `open`, only the `Source` members on the builder type work. For `task`, only binds of `Task<'T>` work:

```fsharp
open System.Threading.Tasks

let answer () =
    IcedTasks.Polyfill.RuntimeTask.RuntimeTasks.task {
        let! value = Task.FromResult 41
        return value + 1
    }
```

A bind of a `Task`, a `ValueTask`, or an `Async` in that expression stops with error FS0193. To keep the `open` small, put the code in its own module.

## 3. Write the computation expressions

Write the computation expressions as you write them with the other task builders.

```fsharp
module Orders =
    open System.Collections.Generic
    open System.IO
    open System.Threading.Tasks
    open IcedTasks.Polyfill.RuntimeTask

    let readBoth (first: string) (second: string) =
        task {
            let! a = File.ReadAllTextAsync first
            and! b = File.ReadAllTextAsync second
            return a.Length + b.Length
        }

    let readOrEmpty (path: string) =
        task {
            try
                return! File.ReadAllTextAsync path
            with :? FileNotFoundException ->
                return ""
        }

    let copyLines (lines: IAsyncEnumerable<string>) (path: string) =
        backgroundTask {
            use writer = new StreamWriter(path)

            for line in lines do
                do! writer.WriteLineAsync line
        }

    let getCached (cache: Dictionary<string, string>) (load: string -> Task<string>) (key: string) =
        valueTask {
            match cache.TryGetValue key with
            | true, value -> return value
            | false, _ ->
                let! value = load key
                cache[key] <- value
                return value
        }

    let addOne (work: Async<int>) =
        task {
            let! value = work
            return value + 1
        }
```

The builders accept these values in `let!`, `do!`, `return!`, and `and!`:

- `Task<'T>`, `Task`, `ValueTask<'T>`, and `ValueTask`.
- `Async<'T>`. The builder starts it with `Async.StartImmediateAsTask`.
- A cold task, `unit -> Task<'T>` or `unit -> Task`. The builder starts it.
- An awaiter or an awaitable value, for example `Task.Yield()`.

`use` and `use!` dispose `IAsyncDisposable` and `IDisposable` values. `for` works with `seq<'T>` and `IAsyncEnumerable<'T>`.

Use `backgroundTask` for library code that must not continue on the caller `SynchronizationContext`. For `and!` order and failure rules, see [Runtime-async task builders](../Explanations/Runtime-async-task-builders.html).

## 4. Write cold and cancellable computations

Use `coldTask` for work that must not start until the caller calls it. Use `cancellableTask` or `cancellableValueTask` when the caller gives a `CancellationToken`.

```fsharp
module Downloads =
    open System.Net.Http
    open System.Threading
    open System.Threading.Tasks
    open IcedTasks
    open IcedTasks.Polyfill.RuntimeTask

    let getText (client: HttpClient) (url: string) =
        cancellableTask {
            let! ct = CancellableTask.getCancellationToken ()
            return! client.GetStringAsync(url, ct)
        }

    let getBoth (client: HttpClient) (first: string) (second: string) =
        cancellableTask {
            let! a = getText client first
            and! b = getText client second
            return a.Length + b.Length
        }

    let loadOnce (load: unit -> Task<string>) =
        coldTask {
            let! text = load ()
            return text.Trim()
        }
```

- `getText client url` returns `CancellationToken -> Task<string>`. The caller starts it with a token: `getText client url ct`.
- In `getBoth`, the builder gives the ambient token to both calls of `getText`. It starts both calls before it awaits the first one.
- The builder checks the token before each `let!`, `do!`, `return!`, and `and!`. If the caller cancels the token, the task is cancelled at the next check, or when an operation that uses the token stops.
- `loadOnce load` returns `unit -> Task<string>`. Each call runs the computation again.

`CancellableTask.getCancellationToken` comes from the `IcedTasks` namespace. The runtime-async builders bind it as they bind any `CancellationToken -> ValueTask<'T>` value.

## 5. Target net10.0 and net11.0 in one project

You can build one project for `net10.0` and `net11.0`.

1. Set `TargetFrameworks` to the two frameworks.
2. Set `LangVersion` to `preview` only for `net11.0`.

```xml
<PropertyGroup>
  <TargetFrameworks>net10.0;net11.0</TargetFrameworks>
  <LangVersion Condition="'$(TargetFramework)' == 'net11.0'">preview</LangVersion>
</PropertyGroup>
```

You do not need `#if` around the `open`. A `net10.0` project gets the `net9.0` build of IcedTasks. In that build, the namespace `IcedTasks.Polyfill.RuntimeTask` is empty. Thus, the same code uses the state-machine builders on `net10.0` and the runtime-async builders on `net11.0`. On `net10.0`, `task` and `backgroundTask` come from FSharp.Core, and the other builder names come from IcedTasks.

## 6. Make sure that a method is runtime async

The compiler sets `MethodImplAttributes.Async` on the method that runs the computation expression. To examine the flag, read it in the body:

```fsharp
open System.Reflection
open IcedTasks.Polyfill.RuntimeTask

let isRuntimeAsync () =
    task {
        let method = MethodBase.GetCurrentMethod()
        return method.MethodImplementationFlags.HasFlag MethodImplAttributes.Async
    }
```

On `net11.0`, the task from `isRuntimeAsync ()` gives `true` in Debug and Release builds. In a `net10.0` build, the same task gives `false`, because the code uses the FSharp.Core `task` builder.

You can also examine the IL. In the IL view of ILSpy 9.1, the method header of a runtime-async method shows `cil managed flag(2000)`. The value `0x2000` is `MethodImplAttributes.Async`.

## Troubleshooting

| Error or result | Cause | Correction |
| --- | --- | --- |
| `error FS3350: Feature 'runtime async' is not available in F# 11.0. Please use language version 'PREVIEW' or greater.` | `LangVersion` is not `preview`. | Set `<LangVersion>preview</LangVersion>` for `net11.0`. |
| `error FS3918: Runtime async suspension method 'Await' may only be called from a runtime async method.` | In Debug, this error comes with FS3350. | Correct FS3350. |
| `error NU1605: Detected package downgrade: FSharp.Core from 11.0.101 to ...` | The project sets a FSharp.Core version older than 11.0.101. | Use FSharp.Core 11.0.101 or later. |
| `error FS0039: The value, constructor, namespace or type 'task' is not defined.` | The code writes `IcedTasks.Polyfill.RuntimeTask.task`. | Open the namespace. |
| `error FS0193: The type 'Task' is not compatible with the type 'Task<'a>'` in a builder that you wrote with its full name | The extension `Source` members are not in scope. | Open the namespace. |
| A function name is missing from a stack trace in a Release build. | The optimizer moved a small function into its caller. | Put `[<MethodImpl(MethodImplOptions.NoInlining)>]` on the function. |

## Related

- [Runtime-async task builders](../Explanations/Runtime-async-task-builders.html) explains the design, the `and!` rules, and the limits.
- [Polyfill namespaces and shadowing](../Explanations/Polyfill-namespaces-and-shadowing.html) explains how a polyfill namespace replaces builder names.
- [Use background builders to avoid caller context](Use-background-builders-to-avoid-caller-context.html) explains `backgroundTask`.
