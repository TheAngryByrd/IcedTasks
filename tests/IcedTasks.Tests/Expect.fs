namespace IcedTasks.Tests

open System
open System.Threading.Tasks
open IcedTasks

module TestHelpers =
    let makeDisposable () =
        { new System.IDisposable with
            member this.Dispose() = ()
        }

    let makeAsyncDisposable () =
        { new System.IAsyncDisposable with
            member this.DisposeAsync() = ValueTask.CompletedTask
        }

module Expect =
    open Expecto

    /// Expects the passed function to throw `'texn`.
    [<RequiresExplicitTypeArguments>]
    let throwsTAsync<'texn when 'texn :> exn> f message = async {
        let! thrown = async {
            try
                do! f ()
                return None
            with e ->
                return Some e
        }

        match thrown with
        | Some e when e.GetType().IsAssignableFrom typeof<'texn> ->
            failtestf
                "%s. Expected f to throw an exn of type %s, but one of type %s was thrown."
                message
                (typeof<'texn>.FullName)
                (e.GetType().FullName)
        | Some _ -> ()
        | _ -> failtestf "%s. Expected f to throw." message
    }


type Expect =

    static member CancellationRequested(operation: Async<_>) =
        Expect.throwsTAsync<OperationCanceledException>
            (fun () -> operation)
            "Should have been cancelled"


    static member CancellationRequested(operation: Task<_>) =
        Expect.CancellationRequested(Async.AwaitTask operation)
        |> Async.StartAsTask

    static member CancellationRequested(operation: ColdTask<_>) =
        Expect.CancellationRequested(Async.AwaitColdTask operation)
        |> Async.AsColdTask

    static member CancellationRequested(operation: CancellableTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableTask operation)
        |> Async.AsCancellableTask
