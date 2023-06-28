namespace IcedTasks

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.ExceptionServices

type private Async =
    static member inline map f x =
        async.Bind(x, (fun v -> async.Return(f v)))

type AsyncEx =
    static member inline AwaitAwaiter(awaiter: 'Awaiter) =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            Awaiter.OnCompleted(
                awaiter,
                (fun () ->
                    try
                        cont (Awaiter.GetResult awaiter)
                    with
                    | :? TaskCanceledException as ce -> ccont ce
                    | :? OperationCanceledException as ce -> ccont ce
                    | :? AggregateException as ae ->
                        if ae.InnerExceptions.Count = 1 then
                            econt ae.InnerExceptions.[0]
                        else
                            econt ae
                    | e -> econt e
                )
            )
        )

    static member inline AwaitAwaitable(awaitable: 'Awaitable) =
        AsyncEx.AwaitAwaiter(Awaitable.GetAwaiter awaitable)

    static member AwaitTask(task: Task) : Async<unit> =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            if task.IsCompleted then
                if task.IsFaulted then
                    let e = task.Exception

                    if e.InnerExceptions.Count = 1 then
                        econt e.InnerExceptions.[0]
                    else
                        econt e
                elif task.IsCanceled then
                    ccont (TaskCanceledException(task))
                else
                    cont ()
            else
                task.ContinueWith(
                    (fun (task: Task) ->
                        if task.IsFaulted then
                            let e = task.Exception

                            if e.InnerExceptions.Count = 1 then
                                econt e.InnerExceptions.[0]
                            else
                                econt e
                        elif task.IsCanceled then
                            ccont (TaskCanceledException(task))
                        else
                            cont ()
                    ),
                    TaskContinuationOptions.ExecuteSynchronously
                )
                |> ignore
        )

    static member AwaitTask(task: Task<'T>) : Async<'T> =
        Async.FromContinuations(fun (cont, econt, ccont) ->

            if task.IsCompleted then
                if task.IsFaulted then
                    let e = task.Exception

                    if e.InnerExceptions.Count = 1 then
                        econt e.InnerExceptions.[0]
                    else
                        econt e
                elif task.IsCanceled then
                    ccont (TaskCanceledException(task))
                else
                    cont task.Result
            else
                task.ContinueWith(
                    (fun (task: Task<'T>) ->
                        if task.IsFaulted then
                            let e = task.Exception

                            if e.InnerExceptions.Count = 1 then
                                econt e.InnerExceptions.[0]
                            else
                                econt e
                        elif task.IsCanceled then
                            ccont (TaskCanceledException(task))
                        else
                            cont task.Result
                    ),
                    TaskContinuationOptions.ExecuteSynchronously
                )
                |> ignore
        )
#if NETSTANDARD2_1

    /// <summary>
    /// Return an asynchronous computation that will check if ValueTask is completed or wait for
    /// the given task to complete and return its result.
    /// </summary>
    /// <param name="vTask">The task to await.</param>
    static member inline AwaitValueTask(vTask: ValueTask<_>) : Async<_> =
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vTask.IsCompletedSuccessfully then
            async.Return vTask.Result
        else
            AsyncEx.AwaitTask(vTask.AsTask())


    /// <summary>
    /// Return an asynchronous computation that will check if ValueTask is completed or wait for
    /// the given task to complete and return its result.
    /// </summary>
    /// <param name="vTask">The task to await.</param>
    static member inline AwaitValueTask(vTask: ValueTask) : Async<unit> =
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vTask.IsCompletedSuccessfully then
            async.Return()
        else
            AsyncEx.AwaitTask(vTask.AsTask())

#endif

[<AutoOpen>]
module AsyncExtensions =

    type Microsoft.FSharp.Control.Async with

        static member inline TryFinallyAsync(comp: Async<'T>, deferred) : Async<'T> =

            let finish (compResult, deferredResult) (cont, (econt: exn -> unit), ccont) =
                match (compResult, deferredResult) with
                | (Choice1Of3 x, Choice1Of3()) -> cont x
                | (Choice2Of3 compExn, Choice1Of3()) -> econt compExn
                | (Choice3Of3 compExn, Choice1Of3()) -> ccont compExn
                | (Choice1Of3 _, Choice2Of3 deferredExn) -> econt deferredExn
                | (Choice2Of3 compExn, Choice2Of3 deferredExn) ->
                    econt
                    <| new AggregateException(compExn, deferredExn)
                | (Choice3Of3 compExn, Choice2Of3 deferredExn) -> econt deferredExn
                | (_, Choice3Of3 deferredExn) ->
                    econt
                    <| new Exception("Unexpected cancellation.", deferredExn)

            let startDeferred compResult (cont, econt, ccont) =
                Async.StartWithContinuations(
                    deferred,
                    (fun () -> finish (compResult, Choice1Of3()) (cont, econt, ccont)),
                    (fun exn -> finish (compResult, Choice2Of3 exn) (cont, econt, ccont)),
                    (fun exn -> finish (compResult, Choice3Of3 exn) (cont, econt, ccont))
                )

            let startComp ct (cont, econt, ccont) =
                Async.StartWithContinuations(
                    comp,
                    (fun x -> startDeferred (Choice1Of3(x)) (cont, econt, ccont)),
                    (fun exn -> startDeferred (Choice2Of3 exn) (cont, econt, ccont)),
                    (fun exn -> startDeferred (Choice3Of3 exn) (cont, econt, ccont)),
                    ct
                )

            async {
                let! ct = Async.CancellationToken
                return! Async.FromContinuations(startComp ct)
            }


/// Class for AsyncEx functionality
type AsyncExBuilder() =

    member inline _.Zero() = async.Zero()

    member inline _.Delay([<InlineIfLambda>] generator: unit -> Async<'j>) = async.Delay generator

    member inline _.Return(value: 'i) = async.Return value

    member inline _.ReturnFrom(computation: Async<_>) = async.ReturnFrom computation

    member inline _.Bind(computation: Async<'f>, [<InlineIfLambda>] binder: 'f -> Async<'f0>) =
        async.Bind(computation, binder)

#if NETSTANDARD2_1

    member inline _.TryFinallyAsync
        (
            computation: Async<'ok>,
            [<InlineIfLambda>] compensation: unit -> ValueTask
        ) : Async<'ok> =

        let compensation = async {
            let vTask = compensation ()
            return! Async.AwaitValueTask vTask
        }

        Async.TryFinallyAsync(computation, compensation)

    member inline this.Using
        (
            resource: #IAsyncDisposable,
            [<InlineIfLambda>] (binder: 'c -> Async<'ok>)
        ) =
        this.TryFinallyAsync(
            binder resource,
            (fun () ->
                if not (isNull (box resource)) then
                    resource.DisposeAsync()
                else
                    ValueTask()
            )
        )

#endif
    member inline _.While([<InlineIfLambda>] guard: unit -> bool, computation: Async<unit>) =
        async.While(guard, computation)

    member inline _.For(sequence: seq<'e>, [<InlineIfLambda>] body: 'e -> Async<unit>) =
        async.For(sequence, body)

    member inline _.Combine(computation1, computation2) =
        async.Combine(computation1, computation2)

    member inline _.TryFinally
        (
            computation: Async<'a>,
            [<InlineIfLambda>] (compensation: unit -> unit)
        ) =
        async.TryFinally(computation, compensation)

    member inline _.TryWith
        (
            computation: Async<'a>,
            [<InlineIfLambda>] (catchHandler: exn -> Async<'a>)
        ) =
        async.TryWith(computation, catchHandler)

    member inline _.Source(async: Async<'a>) = async

    member inline _.Source(seq: #seq<_>) = seq

    // Required because SRTP can't determine the type of the awaiter
    //     Candidates:
    //  - Task.GetAwaiter() : Runtime.CompilerServices.TaskAwaiter
    //  - Task.GetAwaiter() : Runtime.CompilerServices.TaskAwaiter<string>F# Compiler43
    member inline _.Source(task: Task<_>) = AsyncEx.AwaitTask task

[<AutoOpen>]
module Extensions =
    open FSharp.Core.CompilerServices

    type AsyncExBuilder with

        member inline _.Using(resource: #IDisposable, [<InlineIfLambda>] binder) =
            async.Using(resource, binder)

        [<NoEagerConstraintApplication>]
        member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
            when Awaiter<'Awaiter, 'TResult1>>
            (getAwaiter: 'Awaiter)
            =
            getAwaiter
            |> AsyncEx.AwaitAwaiter

        [<NoEagerConstraintApplication>]
        member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
            when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
            (task: 'Awaitable)
            =
            task
            |> Awaitable.GetAwaiter
            |> AsyncEx.AwaitAwaiter

    let asyncEx = new AsyncExBuilder()


module Tests =
#if NETSTANDARD2_1
    type DisposableAsync() =
        interface IAsyncDisposable with
            member this.DisposeAsync() = ValueTask()

    let disposeAsyncTest = asyncEx {
        use foo = new DisposableAsync()
        return ()
    }

    let valueTaskTest = asyncEx {
        let! ct = ValueTask<string> "LOL"
        return ct
    }

    let valueTaskTest2 = asyncEx {
        let! ct = ValueTask()
        return ct
    }
#endif

    type Disposable() =
        interface IDisposable with
            member this.Dispose() = ()

    let disposeTest = asyncEx {
        use foo = new Disposable()
        return ()
    }

    let taskTest = asyncEx {
        let! ct = Task.FromResult "LOL"
        return ct
    }

    let taskTest2 = asyncEx {
        let! ct = (Task.FromResult() :> Task)
        return ct
    }

    let yieldTasktest = asyncEx {
        let! ct = Task.Yield()
        return ct
    }

    let awaiterTest = asyncEx {
        let! ct = (Task.FromResult "LOL").GetAwaiter()
        return ct
    }
