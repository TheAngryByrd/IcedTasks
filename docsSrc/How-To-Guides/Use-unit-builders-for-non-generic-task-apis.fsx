(**
---
title: Use unit builders for non-generic task APIs
category: How To Guides
categoryindex: 4
index: 11
---

# How to use unit builders for non-generic task APIs

Use the unit builders when an API requires a non-generic `Task` or `ValueTask`.

The most important case is `ValueTask`: `ValueTask<'T>` is not a subtype of `ValueTask`.
For example, [`IAsyncDisposable.DisposeAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable.disposeasync?view=netstandard-2.1#system-iasyncdisposable-disposeasync) must return non-generic `ValueTask`.

You can convert `ValueTask<'T>` with `ValueTask.toUnit`, but `valueTaskUnit` and `vTaskUnit` build the non-generic shape directly using the non-generic `AsyncValueTaskMethodBuilder`.

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System
open System.Threading
open System.Threading.Tasks
open IcedTasks

(**
## Implement `IAsyncDisposable.DisposeAsync`

`DisposeAsync` must return `ValueTask`, not `ValueTask<unit>`.
Use `valueTaskUnit` when the cleanup work is naturally written as a computation expression.

*)

type BufferedWriter() =
    let mutable flushed = false

    member _.Flushed = flushed

    member _.FlushAsync() =
        valueTaskUnit {
            do! Task.Delay 1
            flushed <- true
        }

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            valueTaskUnit {
                do! this.FlushAsync()
            }

(**
`vTaskUnit` is an alias for `valueTaskUnit`.

*)

let flushWithAlias () : ValueTask =
    vTaskUnit {
        do! Task.Delay 1
    }

(**
## Return non-generic `Task`

Use `taskUnit` when an API explicitly wants `Task`.
`Task<'T>` inherits from `Task`, but `taskUnit` states the return shape directly and uses the non-generic task method builder.

*)

type Worker() =
    member _.StopAsync(cancellationToken: CancellationToken) : Task =
        taskUnit {
            do! Task.Delay(1, cancellationToken)
        }

(**
Use `backgroundTaskUnit` when the API wants non-generic `Task` and the work should avoid the caller's synchronization context.
See [Use background builders to avoid caller context](Use-background-builders-to-avoid-caller-context.html) for the scheduler behavior.

*)

let writeAuditInBackground () : Task =
    backgroundTaskUnit {
        do! Task.Delay 1
    }

(**
## Convert when you already have a generic ValueTask

If you already have a `ValueTask<'T>`, use `ValueTask.toUnit` to discard the result and return non-generic `ValueTask`.
Prefer `valueTaskUnit` when you are authoring the computation and know the API needs `ValueTask`.

*)

let loadCount () =
    valueTask { return 42 }

let loadCountAsUnit () : ValueTask =
    loadCount ()
    |> ValueTask.toUnit

(**
## Run the examples

These calls make the samples complete and compiler-checked.

*)

let writer = new BufferedWriter()
let disposed = (writer :> IAsyncDisposable).DisposeAsync().AsTask().GetAwaiter().GetResult()
let flushed = writer.Flushed
let aliasResult = flushWithAlias().AsTask().GetAwaiter().GetResult()
let stopped = Worker().StopAsync(CancellationToken.None).GetAwaiter().GetResult()
let audit = writeAuditInBackground().GetAwaiter().GetResult()
let loadedAsUnit = loadCountAsUnit().AsTask().GetAwaiter().GetResult()

(**
## Choose the unit builder

| API needs | Use |
|---|---|
| `Task` | `taskUnit` |
| `Task` and background context behavior | `backgroundTaskUnit` |
| `ValueTask` | `valueTaskUnit` |
| `ValueTask`, short alias | `vTaskUnit` |

Use the generic builders when the API should return a meaningful value, such as `Task<'T>` or `ValueTask<'T>`.

*)
