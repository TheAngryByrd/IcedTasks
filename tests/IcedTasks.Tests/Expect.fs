namespace IcedTasks.Tests

open System
open System.Threading.Tasks
open IcedTasks

module TestHelpers =
    let makeDisposable (callback) =
        { new System.IDisposable with
            member this.Dispose() = callback ()
        }

    let makeAsyncDisposable (callback) =
        { new System.IAsyncDisposable with
            member this.DisposeAsync() = callback ()
        }

module Expect =
    open Expecto

    /// Expects the passed function to throw `'texn`.
    [<RequiresExplicitTypeArguments>]
    let throwsTAsync<'texn when 'texn :> exn> f message =
        async {
            let! thrown =
                async {
                    try
                        do! f ()
                        return ValueNone
                    with e ->
                        return ValueSome e
                }

            match thrown with
            | ValueSome e when e.GetType().IsAssignableFrom typeof<'texn> ->
                failtestf
                    "%s. Expected f to throw an exn of type %s, but one of type %s was thrown."
                    message
                    (typeof<'texn>.FullName)
                    (e.GetType().FullName)
            | ValueSome _ -> ()
            | _ -> failtestf "%s. Expected f to throw." message
        }

    [<RequiresExplicitTypeArguments>]
    let throwsTask<'texn when 'texn :> exn> f message =
        throwsTAsync<'texn>
            (f
             >> Async.AwaitTask)
            message
        |> Async.StartImmediateAsTask

#if !NETSTANDARD2_0
    [<RequiresExplicitTypeArguments>]
    let throwsValueTask<'texn when 'texn :> exn> (f: unit -> ValueTask<unit>) message =
        throwsTAsync<'texn>
            (f
             >> Async.AwaitValueTask)
            message
        |> Async.StartImmediateAsTask
        |> ValueTask<unit>

#endif
type Expect =

    static member CancellationRequested(operation: Async<_>) =
        Expect.throwsTAsync<OperationCanceledException>
            (fun () -> operation)
            "Should have been cancelled"

#if !NETSTANDARD2_0
    static member CancellationRequested(operation: ValueTask<unit>) =
        Expect.CancellationRequested(Async.AwaitValueTask operation)
        |> Async.AsValueTask
#endif
    static member CancellationRequested(operation: Task<_>) =
        Expect.CancellationRequested(Async.AwaitTask operation)
        |> Async.StartImmediateAsTask

    static member CancellationRequested(operation: ColdTask<_>) =
        Expect.CancellationRequested(Async.AwaitColdTask operation)
        |> Async.AsColdTask

    static member CancellationRequested(operation: CancellableTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableTask operation)
        |> Async.AsCancellableTask

#if !NETSTANDARD2_0
    static member CancellationRequested(operation: CancellableValueTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableValueTask operation)
        |> Async.AsCancellableValueTask
#endif

open TimeProviderExtensions
open System.Runtime.CompilerServices

[<Extension>]
type ManualTimeProviderExtensions =

    [<Extension>]
    static member ForwardTimeAsync(this: ManualTimeProvider, time) =
        task {
            this.Advance(time)
            //https://github.com/dotnet/runtime/issues/85326
            do! Task.Yield()
            do! Task.Delay(5)
        }


module CustomAwaiter =

    type CustomAwaiter<'T>(onGetResult, onIsCompleted) =

        member this.GetResult() : 'T = onGetResult ()
        member this.IsCompleted: bool = onIsCompleted ()

        interface ICriticalNotifyCompletion with
            member this.UnsafeOnCompleted(continuation) = failwith "Not Implemented"
            member this.OnCompleted(continuation: Action) : unit = failwith "Not Implemented"
