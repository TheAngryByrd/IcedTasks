(**
---
title: Use AsyncEx for .NET async interop
category: How To Guides
categoryindex: 2
index: 5
---

# How to use AsyncEx for .NET async interop

Use `asyncEx` when you want F# `Async<'T>` semantics and also need to bind modern .NET async shapes directly.
The workflow still returns `Async<'T>`, so you can run it with the usual `Async` APIs.

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

(**
Open `IcedTasks.AsyncEx` to bring the `asyncEx` builder into scope.

*)

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open IcedTasks.AsyncEx

(**
## Bind `Task`, `ValueTask`, and awaitables

Inside `asyncEx`, `let!` and `do!` can bind `Task<'T>`, `Task`, `ValueTask<'T>`, `ValueTask`, and awaitable values such as `Task.Yield()`.

*)

let getTaskValue () = Task.FromResult 21

let getValueTaskValue () = ValueTask<int> 21

let bindDotnetAsyncShapes =
    asyncEx {
        let! left = getTaskValue () // Uses Async.AwaitTask under the hood
        let! right = getValueTaskValue () // Uses Async.AwaitValueTask under the hood
        do! Task.Yield()

        return
            left
            + right
    }

let answer = Async.RunSynchronously bindDotnetAsyncShapes

(**
## Dispose `IAsyncDisposable` values

Use `use` for resources that implement `IAsyncDisposable`. Disposal runs when the workflow completes, raises, or is cancelled.

*)

type TrackedAsyncDisposable(disposed: bool ref) =
    interface IAsyncDisposable with
        member _.DisposeAsync() =
            disposed.Value <- true
            ValueTask()

let useAsyncDisposable =
    asyncEx {
        let disposed = ref false
        use _resource = new TrackedAsyncDisposable(disposed)
        return disposed
    }

let disposedAfterWorkflow =
    useAsyncDisposable
    |> Async.RunSynchronously
    |> fun disposed -> disposed.Value

(**
## Iterate `IAsyncEnumerable<'T>`

Use `for` directly with an `IAsyncEnumerable<'T>`.

*)

let numbers: IAsyncEnumerable<int> =
    { new IAsyncEnumerable<int> with
        member _.GetAsyncEnumerator(cancellationToken: CancellationToken) =
            let values = [|
                1
                2
                3
            |]

            let mutable index = -1

            { new IAsyncEnumerator<int> with
                member _.Current = values[index]

                member _.MoveNextAsync() =
                    cancellationToken.ThrowIfCancellationRequested()
                    index <- index + 1
                    ValueTask<bool>(index < values.Length)

                member _.DisposeAsync() = ValueTask()
            }
    }

let sumAsyncEnumerable =
    asyncEx {
        let mutable total = 0

        for number in numbers do
            total <-
                total
                + number

        return total
    }

let total = Async.RunSynchronously sumAsyncEnumerable

(**
## Shadow `async` when you want AsyncEx everywhere in a file

Open `IcedTasks.Polyfill.Async` when you want `async { ... }` to use the AsyncEx builder.
Use this sparingly because it intentionally shadows the FSharp.Core `async` builder in the current scope.

*)

module PolyfillExample =
    open IcedTasks.Polyfill.Async

    let workflow =
        async {
            let! value = Task.FromResult 42
            use _resource = new TrackedAsyncDisposable(ref false)
            return value
        }

    let result = Async.RunSynchronously workflow
