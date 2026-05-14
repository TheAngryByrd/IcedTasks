(**
---
title: Convert between async shapes
category: How To Guides
categoryindex: 2
index: 4
---

# How to convert between async shapes

Use these helpers at interop boundaries where one part of your code uses `Async<'T>` and another part uses `Task`, `ValueTask`, `ColdTask`, or a cancellable task shape.

| I have | I need | Use |
|---|---|---|
| `ValueTask<'T>` | `Async<'T>` | `Async.AwaitValueTask` |
| `Async<'T>` | `ValueTask<'T>` | `Async.AsValueTask` |
| `ColdTask<'T>` | `Async<'T>` | `Async.AwaitColdTask` |
| `Async<'T>` | `ColdTask<'T>` | `Async.AsColdTask` |
| `CancellableTask<'T>` | `Async<'T>` | `Async.AwaitCancellableTask` |
| `Async<'T>` | `CancellableTask<'T>` | `Async.AsCancellableTask` |
| `CancellableValueTask<'T>` | `Async<'T>` | `Async.AwaitCancellableValueTask` |
| `Async<'T>` | `CancellableValueTask<'T>` | `Async.AsCancellableValueTask` |
| `Task<'T>` or `ValueTask<'T>` | `Async<'T>` with AsyncEx task exception behavior | `AsyncEx.AwaitTask` or `AsyncEx.AwaitValueTask` |
| Custom awaitable | `Async<'T>` with AsyncEx | `AsyncEx.AwaitAwaitable` or `AsyncEx.AwaitAwaiter` |

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open IcedTasks
open IcedTasks.AsyncEx

(**
## Use `ValueTask` from `Async`

Use `Async.AwaitValueTask` when an `async { }` workflow needs to await a `ValueTask<'T>` or `ValueTask`.
Use `Async.AsValueTask` when an `Async<'T>` must be exposed to a `ValueTask<'T>` caller.

*)

let getCachedValue () = ValueTask<int> 42

let valueTaskInsideAsync =
    async {
        let! value =
            getCachedValue ()
            |> Async.AwaitValueTask

        return value + 1
    }

let asyncExposedAsValueTask =
    valueTaskInsideAsync
    |> Async.AsValueTask

(**
## Use `ColdTask` from `Async`

Use `Async.AwaitColdTask` when an `async { }` workflow needs to start and await cold task-shaped work.
Use `Async.AsColdTask` when an `Async<'T>` should be exposed as `unit -> Task<'T>`.

*)

let loadCold: ColdTask<int> = coldTask { return 10 }

let coldTaskInsideAsync =
    async {
        let! value =
            loadCold
            |> Async.AwaitColdTask

        return value * 2
    }

let asyncExposedAsColdTask =
    coldTaskInsideAsync
    |> Async.AsColdTask

(**
## Use cancellable task shapes from `Async`

Use `Async.AwaitCancellableTask` or `Async.AwaitCancellableValueTask` when the surrounding `Async<'T>` should provide the cancellation token.
The helper reads `Async.CancellationToken` and passes it into the cancellable operation.

*)

let loadCancellableTask: CancellableTask<int> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, ct)
        return 20
    }

let loadCancellableValueTask: CancellableValueTask<int> =
    cancellableValueTask {
        let! ct = CancellableValueTask.getCancellationToken ()
        do! Task.Delay(1, ct)
        return 30
    }

let cancellableTaskInsideAsync =
    async {
        let! value =
            loadCancellableTask
            |> Async.AwaitCancellableTask

        return value + 1
    }

let cancellableValueTaskInsideAsync =
    async {
        let! value =
            loadCancellableValueTask
            |> Async.AwaitCancellableValueTask

        return value + 1
    }

(**
## Expose `Async` as a cancellable task shape

Use `Async.AsCancellableTask` or `Async.AsCancellableValueTask` when a caller should provide a token later.
The returned function starts the original `Async<'T>` with the supplied token.

*)

let loadFromAsync =
    async {
        let! ct = Async.CancellationToken
        do! Async.Sleep 1
        ct.ThrowIfCancellationRequested()
        return 40
    }

let asyncExposedAsCancellableTask =
    loadFromAsync
    |> Async.AsCancellableTask

let asyncExposedAsCancellableValueTask =
    loadFromAsync
    |> Async.AsCancellableValueTask

(**
## Use AsyncEx for Task, ValueTask, and custom awaitables

`AsyncEx` provides await helpers with task exception behavior intended for IcedTasks interop.
In most code, the `asyncEx { }` builder can bind these values directly. Use the static helpers when you want an explicit conversion.

*)

let taskInsideAsyncEx =
    Task.FromResult 50
    |> AsyncEx.AwaitTask

let valueTaskInsideAsyncEx =
    ValueTask<int> 60
    |> AsyncEx.AwaitValueTask

let yieldInsideAsyncEx =
    Task.Yield()
    |> AsyncEx.AwaitAwaitable

type ImmediateAwaiter<'T>(value: 'T) =
    member _.IsCompleted = true
    member _.GetResult() = value
    member _.OnCompleted(_continuation: System.Action) = ()
    member _.UnsafeOnCompleted(_continuation: System.Action) = ()

    interface INotifyCompletion with
        member this.OnCompleted(continuation) = this.OnCompleted(continuation)

    interface ICriticalNotifyCompletion with
        member this.UnsafeOnCompleted(continuation) = this.UnsafeOnCompleted(continuation)

let customAwaiterInsideAsyncEx =
    ImmediateAwaiter 70
    |> AsyncEx.AwaitAwaiter

(**
## Run the samples

These calls are here so the examples are checked as complete executable code.

*)

let result1 =
    valueTaskInsideAsync
    |> Async.RunSynchronously

let result2 = asyncExposedAsValueTask.AsTask().GetAwaiter().GetResult()

let result3 =
    coldTaskInsideAsync
    |> Async.RunSynchronously

let result4 = asyncExposedAsColdTask().GetAwaiter().GetResult()

let result5 =
    cancellableTaskInsideAsync
    |> Async.RunSynchronously

let result6 =
    cancellableValueTaskInsideAsync
    |> Async.RunSynchronously

let result7 =
    asyncExposedAsCancellableTask CancellationToken.None
    |> Async.AwaitTask
    |> Async.RunSynchronously

let result8 =
    asyncExposedAsCancellableValueTask CancellationToken.None
    |> Async.AwaitValueTask
    |> Async.RunSynchronously

let result9 =
    taskInsideAsyncEx
    |> Async.RunSynchronously

let result10 =
    valueTaskInsideAsyncEx
    |> Async.RunSynchronously

let result11 =
    yieldInsideAsyncEx
    |> Async.RunSynchronously

let result12 =
    customAwaiterInsideAsyncEx
    |> Async.RunSynchronously

(**
## Prefer builders inside one async shape

Do not convert just to convert. If all of the code can live naturally inside one builder, keep it there.
Use the conversion helpers at API boundaries, when integrating with existing libraries, or when moving gradually between async shapes.

*)
