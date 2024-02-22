namespace IcedTasks.Tests

open System
open System.Threading.Tasks
open IcedTasks

module Task =
    /// Runs Task.Yield() `max` times. Useful for places where we want the scheduler to asynchronously yield but really fast.
    /// We run it max times to ensure it really gets async yielded.
    /// Alternative would be Task.Delay but can be slow.
    let yieldMany max =
        task {
            for _ = 0 to max do
                do! Task.Yield()
        }

module TestHelpers =
    open System.Threading

    let makeDisposable (callback) =
        { new System.IDisposable with
            member this.Dispose() = callback ()
        }

    let makeAsyncDisposable (callback) =
        { new System.IAsyncDisposable with
            member this.DisposeAsync() = callback ()
        }

    let setSyncContext newContext =
        let oldContext = SynchronizationContext.Current
        SynchronizationContext.SetSynchronizationContext newContext
        makeDisposable (fun () -> SynchronizationContext.SetSynchronizationContext oldContext)


module Expecto =
    open Expecto

    let environVarAsBoolOrDefault varName defaultValue =
        let truthyConsts = [
            "1"
            "Y"
            "YES"
            "T"
            "TRUE"
        ]

        try
            let envvar = (Environment.GetEnvironmentVariable varName).ToUpper()

            truthyConsts
            |> List.exists ((=) envvar)
        with _ ->
            defaultValue

    let isInCI () = environVarAsBoolOrDefault "CI" false

    let fsCheckConfig =
        if isInCI () then
            // these tests can be slow on CI so reduce the number of tests
            {
                FsCheckConfig.defaultConfig with
                    maxTest = 10
            }
        else
            FsCheckConfig.defaultConfig


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

    [<RequiresExplicitTypeArguments>]
    let throwsValueTask<'texn when 'texn :> exn> (f: unit -> ValueTask<unit>) message =
        throwsTAsync<'texn>
            (f
             >> Async.AwaitValueTask)
            message
        |> Async.StartImmediateAsTask
        |> ValueTask<unit>


type Expect =

    static member CancellationRequested(operation: Async<_>) =
        Expect.throwsTAsync<OperationCanceledException>
            (fun () -> operation)
            "Should have been cancelled"

    static member CancellationRequested(operation: ValueTask<unit>) =
        Expect.CancellationRequested(Async.AwaitValueTask operation)
        |> Async.AsValueTask

    static member CancellationRequested(operation: Task<_>) =
        Expect.CancellationRequested(Async.AwaitTask operation)
        |> Async.StartImmediateAsTask

    static member CancellationRequested(operation: ColdTask<_>) =
        Expect.CancellationRequested(Async.AwaitColdTask operation)
        |> Async.AsColdTask

    static member CancellationRequested(operation: CancellableTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableTask operation)
        |> Async.AsCancellableTask


    static member CancellationRequested(operation: CancellableValueTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableValueTask operation)
        |> Async.AsCancellableValueTask


open TimeProviderExtensions
open System.Runtime.CompilerServices

[<Extension>]
type ManualTimeProviderExtensions =

    [<Extension>]
    static member ForwardTimeAsync(this: ManualTimeProvider, time) =
        task {
            this.Advance(time)
            //https://github.com/dotnet/runtime/issues/85326
            do! Task.yieldMany 10
        }


module CustomAwaiter =

    type CustomAwaiter<'T>(onGetResult, onIsCompleted) =

        member this.GetResult() : 'T = onGetResult ()
        member this.IsCompleted: bool = onIsCompleted ()

        interface ICriticalNotifyCompletion with
            member this.UnsafeOnCompleted(continuation) = failwith "Not Implemented"
            member this.OnCompleted(continuation: Action) : unit = failwith "Not Implemented"


module AsyncEnumerable =
    open System.Collections.Generic
    open System.Threading

    type AsyncEnumerable<'T>(e: IEnumerable<'T>, beforeMoveNext: Func<_, ValueTask>) =

        member this.GetAsyncEnumerator(ct) =
            let enumerator = e.GetEnumerator()

            { new IAsyncEnumerator<'T> with
                member this.Current = enumerator.Current

                member this.MoveNextAsync() =
                    valueTask {
                        do! beforeMoveNext.Invoke(ct)
                        return enumerator.MoveNext()
                    }

                member this.DisposeAsync() = valueTaskUnit { enumerator.Dispose() }

            }

        interface IAsyncEnumerable<'T> with
            member this.GetAsyncEnumerator(ct: CancellationToken) = this.GetAsyncEnumerator(ct)

    let forXtoY<'T> x y beforeMoveNext =
        AsyncEnumerable([ x..y ], Func<_, _>(beforeMoveNext))
