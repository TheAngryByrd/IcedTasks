namespace IcedTasks

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.ExceptionServices
open System.Collections.Generic

type private Async =
    static member inline map f x =
        async.Bind(x, (fun v -> async.Return(f v)))

/// <summary>
/// This contains many functions that implement Task throwing semantics differently than the current FSharp.Core. See <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
/// </summary>
type AsyncEx =

    /// <summary>
    /// Return an asynchronous computation that will wait for the given Awaiter to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaiter to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://stackoverflow.com/a/66815960">How to use awaitable inside async?</see> and <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member inline AwaitAwaiter(awaiter: 'Awaiter) =
        let inline handleFinished (onNext: 'a -> unit, onError: exn -> unit, awaiter) =
            try
                onNext (Awaiter.GetResult awaiter)
            with
            | :? AggregateException as ae when ae.InnerExceptions.Count = 1 ->
                onError ae.InnerExceptions.[0]
            | e ->
                // Why not handle TaskCanceledException/OperationCanceledException?
                // From https://github.com/dotnet/fsharp/blob/89e641108e8773e8d5731437a2b944510de52567/src/FSharp.Core/async.fs#L1228-L1231:
                // A cancelled task calls the exception continuation with TaskCanceledException, since it may not represent cancellation of
                // the overall async (they may be governed by different cancellation tokens, or
                // the task may not have a cancellation token at all).
                onError e

        Async.FromContinuations(fun (onNext, onError, onCancel) ->
            if Awaiter.IsCompleted awaiter then
                handleFinished (onNext, onError, awaiter)
            else
                Awaiter.UnsafeOnCompleted(
                    awaiter,
                    (fun () -> handleFinished (onNext, onError, awaiter))
                )
        )

    /// <summary>
    /// Return an asynchronous computation that will wait for the given Awaitable to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaitable to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://stackoverflow.com/a/66815960">How to use awaitable inside async?</see> and <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member inline AwaitAwaitable(awaitable: 'Awaitable) =
        AsyncEx.AwaitAwaiter(Awaitable.GetAwaiter awaitable)

    /// <summary>
    /// Return an asynchronous computation that will wait for the given Task to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaitable to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member AwaitTask(task: Task) : Async<unit> = AsyncEx.AwaitAwaitable(task)

    /// <summary>
    /// Return an asynchronous computation that will wait for the given Task to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaitable to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member AwaitTask(task: Task<'T>) : Async<'T> =
        AsyncEx.AwaitAwaiter(Awaitable.GetTaskAwaiter task)


    /// <summary>
    /// Return an asynchronous computation that will wait for the given ValueTask to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaitable to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member inline AwaitValueTask(vTask: ValueTask<_>) : Async<_> =
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vTask.IsCompletedSuccessfully then
            async.Return vTask.Result
        else
            AsyncEx.AwaitTask(vTask.AsTask())


    /// <summary>
    /// Return an asynchronous computation that will wait for the given Task to complete and return
    /// its result.
    /// </summary>
    /// <param name="awaiter">The Awaitable to await</param>
    ///
    /// <remarks>
    /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
    /// </remarks>
    static member inline AwaitValueTask(vTask: ValueTask) : Async<unit> =
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vTask.IsCompletedSuccessfully then
            async.Return()
        else
            AsyncEx.AwaitTask(vTask.AsTask())


/// <exclude/>
[<AutoOpen>]
module AsyncExtensions =

    type Microsoft.FSharp.Control.Async with

        /// <summary>Creates an Async that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        /// <remarks> <see href="http://www.fssnip.net/ru/title/Async-workflow-with-asynchronous-finally-clause">See this F# gist</see></remarks>
        /// <returns>An async with the result of the computation.</returns>
        static member inline TryFinallyAsync
            (
                computation: Async<'T>,
                compensation: Async<unit>
            ) : Async<'T> =

            let finish (compResult, deferredResult) (onNext, (onError: exn -> unit), onCancel) =
                match (compResult, deferredResult) with
                | (Choice1Of3 x, Choice1Of3()) -> onNext x
                | (Choice2Of3 compExn, Choice1Of3()) -> onError compExn
                | (Choice3Of3 compExn, Choice1Of3()) -> onCancel compExn
                | (Choice1Of3 _, Choice2Of3 deferredExn) -> onError deferredExn
                | (Choice2Of3 compExn, Choice2Of3 deferredExn) ->
                    onError
                    <| new AggregateException(compExn, deferredExn)
                | (Choice3Of3 compExn, Choice2Of3 deferredExn) -> onError deferredExn
                | (_, Choice3Of3 deferredExn) ->
                    onError
                    <| new Exception("Unexpected cancellation.", deferredExn)

            let startDeferred compResult (onNext, onError, onCancel) =
                Async.StartWithContinuations(
                    compensation,
                    (fun () -> finish (compResult, Choice1Of3()) (onNext, onError, onCancel)),
                    (fun exn -> finish (compResult, Choice2Of3 exn) (onNext, onError, onCancel)),
                    (fun exn -> finish (compResult, Choice3Of3 exn) (onNext, onError, onCancel))
                )

            let startComp ct (onNext, onError, onCancel) =
                Async.StartWithContinuations(
                    computation,
                    (fun x -> startDeferred (Choice1Of3(x)) (onNext, onError, onCancel)),
                    (fun exn -> startDeferred (Choice2Of3 exn) (onNext, onError, onCancel)),
                    (fun exn -> startDeferred (Choice3Of3 exn) (onNext, onError, onCancel)),
                    ct
                )

            async {
                let! ct = Async.CancellationToken
                return! Async.FromContinuations(startComp ct)
            }


/// <summary>Builds an asynchronous workflow using computation expression syntax.</summary>
/// <remarks>
/// The difference between the AsyncBuilder and AsyncExBuilder is follows:
/// <list type="bullet">
/// <item><description>Allows <c>use</c> on <see cref="T:System.IAsyncDisposable">System.IAsyncDisposable</see></description></item>
/// <item><description>Allows <c>let!</c> for Tasks, ValueTasks, and any Awaitable Type</description></item>
/// <item><description>When Tasks throw exceptions they will use the behavior described in <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see></description></item>
/// </list>
///
/// </remarks>
type AsyncExBuilder() =


    member inline _.Zero() = async.Zero()

    member inline _.Delay([<InlineIfLambda>] generator: unit -> Async<'j>) = async.Delay generator

    member inline _.Return(value: 'i) = async.Return value

    member inline _.ReturnFrom(computation: Async<_>) = async.ReturnFrom computation

    member inline _.Bind(computation: Async<'f>, [<InlineIfLambda>] binder: 'f -> Async<'f0>) =
        async.Bind(computation, binder)

    member inline this.TryFinallyAsync
        (
            computation: Async<'ok>,
            [<InlineIfLambda>] compensation: unit -> ValueTask
        ) : Async<'ok> =

        let compensation = this.Delay(fun () -> AsyncEx.AwaitValueTask(compensation ()))

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


    member inline internal _.WhileAsync(guard: Async<bool>, computation: Async<unit>) =
        async {
            let mutable keepGoing = true

            while keepGoing do
                let! guardResult = guard
                if guardResult then do! computation else keepGoing <- false
        }

    member inline this.For
        (
            sequence: IAsyncEnumerable<'e>,
            [<InlineIfLambda>] body: 'e -> Async<unit>
        ) =
        this.Bind(
            Async.CancellationToken,
            fun (ct: CancellationToken) ->
                this.Using(
                    sequence.GetAsyncEnumerator ct,
                    (fun enumerator ->
                        this.WhileAsync(
                            (this.Delay(fun () ->
                                enumerator.MoveNextAsync()
                                |> AsyncEx.AwaitValueTask
                            )),
                            (this.Delay(fun () -> body enumerator.Current))
                        )

                    )
                )
        )

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

/// <exclude/>
[<AutoOpen>]
module AsyncExExtensionsLowPriority =
    open FSharp.Core.CompilerServices

    type AsyncExBuilder with

        member inline _.Source(seq: #seq<_>) = seq

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
            |> AsyncEx.AwaitAwaitable

    /// <summary>Builds an asynchronous workflow using computation expression syntax.</summary>
    /// <remarks>
    /// The difference between the AsyncBuilder and AsyncExBuilder is follows:
    /// <list type="bullet">
    /// <item><description>Allows <c>use</c> on <see cref="T:System.IAsyncDisposable">System.IAsyncDisposable</see></description></item>
    /// <item><description>Allows <c>let!</c> for Tasks, ValueTasks, and any Awaitable Type</description></item>
    /// <item><description>When Tasks throw exceptions they will use the behavior described in <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see></description></item>
    /// <item><description>Allow <c>for</c> on <see cref="T:System.Collections.Generic.IAsyncEnumerable`1">System.Collections.Generic.IAsyncDisposable</see></description></item>
    /// </list>
    ///
    /// </remarks>
    let asyncEx = new AsyncExBuilder()

/// <exclude/>
[<AutoOpen>]
module AsyncExExtensionsHighPriority =

    type AsyncExBuilder with

        member inline _.Source(seq: #IAsyncEnumerable<_>) = seq

        // Required because SRTP can't determine the type of the awaiter
        //     Candidates:
        //  - Task.GetAwaiter() : Runtime.CompilerServices.TaskAwaiter
        //  - Task.GetAwaiter() : Runtime.CompilerServices.TaskAwaiter<string>F# Compiler43
        member inline _.Source(task: Task<_>) = AsyncEx.AwaitTask task

        member inline _.Source(task: Task) = AsyncEx.AwaitTask task

        member inline _.Source(vtask: ValueTask<_>) = AsyncEx.AwaitValueTask vtask

        member inline _.Source(vtask: ValueTask) = AsyncEx.AwaitValueTask vtask

namespace IcedTasks.Polyfill.Async

/// <namespacedoc>
///   <summary>
///     Namespace contains polyfills for <see cref='T:IcedTasks.AsyncExBuilder'/>. This will  <a href="https://en.wikipedia.org/wiki/Variable_shadowing">shadow</a> the <c>async {...}</c> builder with the version in IcedTasks.
///     </summary>
/// </namespacedoc>
[<AutoOpen>]
module PolyfillBuilders =
    open IcedTasks

    /// <summary>
    /// Builds an asynchronous workflow using computation expression syntax.
    ///
    /// <b>NOTE:</b> This is the AsyncExBuilder defined in IcedTasks.</summary>
    /// <remarks>
    /// The difference between the AsyncBuilder and AsyncExBuilder is follows:
    /// <list type="bullet">
    /// <item><description>Allows <c>use</c> on <see cref="T:System.IAsyncDisposable">System.IAsyncDisposable</see></description></item>
    /// <item><description>Allows <c>let!</c> for Tasks, ValueTasks, and any Awaitable Type</description></item>
    /// <item><description>When Tasks throw exceptions they will use the behavior described in <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see></description></item>
    /// <item><description>Allow <c>for</c> on <see cref="T:System.Collections.Generic.IAsyncEnumerable`1">System.Collections.Generic.IAsyncDisposable</see></description></item>
    /// </list>
    ///
    /// </remarks>
    let async = asyncEx
