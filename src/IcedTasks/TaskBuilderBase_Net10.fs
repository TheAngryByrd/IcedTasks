namespace IcedTasks.TaskBase_Net10

open IcedTasks.Nullness
open IcedTasks.TaskLike
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Microsoft.FSharp.Core.CompilerServices
open System.Collections.Generic
open System.Threading

module internal AsyncHelpers =

    let inline awaitAwaiter awaiter =
        if not (Awaiter.IsCompleted awaiter) then
            AsyncHelpers.UnsafeAwaitAwaiter awaiter

        Awaiter.GetResult awaiter

    let inline awaitAwaitable awaiter =
        awaitAwaiter (Awaitable.GetAwaiter awaiter)

type TaskBuilderBaseRuntime() =

    member inline _.Return(value: 'T) =
        ValueTask.FromResult value
        |> Awaitable.GetAwaiter

    member inline _.Bind(awaiter, [<InlineIfLambda>] continuation) =
        AsyncHelpers.awaitAwaiter awaiter
        |> continuation

    member inline this.ReturnFrom<'TResult1, 'Awaiter when Awaiter<'Awaiter, 'TResult1>>
        (awaiter: 'Awaiter)
        : 'Awaiter =
        awaiter

    member inline _.Zero() =
        ValueTask<_>()
        |> Awaitable.GetAwaiter

    member inline _.Delay([<InlineIfLambda>] f: unit -> 'a) = f

    member inline this.BindReturn(awaiter, [<InlineIfLambda>] mapper) =
        this.Bind(awaiter, (fun v -> this.Return(mapper v)))

    member inline this.MergeSources(awaiter1, awaiter2) =
        this.Bind(awaiter1, fun v1 -> this.Bind(awaiter2, fun v2 -> this.Return(struct (v1, v2))))


    member inline this.Combine(awaiter1, [<InlineIfLambda>] continuation) =
        this.Bind(awaiter1, fun _ -> continuation ())

    member inline this.While
        ([<InlineIfLambda>] guard: unit -> bool, [<InlineIfLambda>] body: unit -> 'a)
        =
        while guard () do
            body ()
            |> AsyncHelpers.awaitAwaiter

        this.Zero()


    member inline this.TryFinallyAsync
        ([<InlineIfLambda>] awaiter, [<InlineIfLambda>] compensation: unit -> ValueTask)
        =
        try
            awaiter ()
        finally
            compensation ()
            |> AsyncHelpers.awaitAwaitable

    member inline this.TryFinally
        ([<InlineIfLambda>] awaiter, [<InlineIfLambda>] compensation: unit -> unit)
        =
        try
            awaiter ()
        finally
            compensation ()

    member inline this.TryWith<'Awaiter>
        ([<InlineIfLambda>] awaiter, [<InlineIfLambda>] catchHandler: exn -> 'Awaiter)
        : 'Awaiter =
        try
            awaiter ()
        with ex ->
            catchHandler ex


    member inline this.Using
        (resource: #IAsyncDisposableNull, [<InlineIfLambda>] binder: #IAsyncDisposableNull -> 'a)
        =
        this.TryFinallyAsync(
            (fun () -> binder resource),
            (fun () ->
                if not (isNull (box resource)) then
                    resource.DisposeAsync()
                else
                    ValueTask.CompletedTask
            )
        )

    member inline this.For(sequence: #IAsyncEnumerable<'T>, [<InlineIfLambda>] body: 'T -> 'a) =
        this.Using(
            sequence.GetAsyncEnumerator CancellationToken.None,
            fun enumerator ->
                while enumerator.MoveNextAsync()
                      |> AsyncHelpers.awaitAwaitable do
                    body enumerator.Current
                    |> AsyncHelpers.awaitAwaiter

                this.Zero()
        )


[<AutoOpen>]
module LowPriority =

    type TaskBuilderBaseRuntime with


        /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
        ///
        /// <remarks>This is the identify function.</remarks>
        ///
        /// <returns>'Awaiter</returns>
        // [<NoEagerConstraintApplication>]
        member inline _.Source<'TResult1, 'Awaiter when Awaiter<'Awaiter, 'TResult1>>
            (awaiter: 'Awaiter)
            : 'Awaiter =
            awaiter

        /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
        ///
        /// <remarks>This turns a ^Awaitable into a 'Awaiter.</remarks>
        ///
        /// <returns>'Awaiter</returns>
        // [<NoEagerConstraintApplication>]
        member inline _.Source<'Awaitable, 'TResult1, 'Awaiter
            when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
            (awaitable: 'Awaitable)
            : 'Awaiter =
            Awaitable.GetAwaiter awaitable

        member inline _.Source(seq: #IAsyncEnumerable<_>) = seq

[<AutoOpen>]
module HighPriority =

    type TaskBuilderBaseRuntime with

        member inline this.Using
            (resource: #IDisposableNull, [<InlineIfLambda>] binder: #IDisposableNull -> 'a)
            =
            this.TryFinally(
                (fun () -> binder resource),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.Dispose()
                )
            )

        member inline this.For(sequence: #seq<'T>, [<InlineIfLambda>] body: 'T -> 'a) =
            for x in sequence do
                body x
                |> AsyncHelpers.awaitAwaiter

            this.Zero()

        /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
        ///
        /// <remarks>This turns a Task&lt;'T&gt; into a 'Awaiter.</remarks>
        ///
        /// <returns>'Awaiter</returns>
        member inline _.Source(task: Task<'T>) = Awaitable.GetTaskAwaiter task


        /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
        ///
        /// <remarks>This turns a Task&lt;'T&gt; into a 'Awaiter.</remarks>
        ///
        /// <returns>'Awaiter</returns>
        member inline _.Source(task: System.Func<Task<'T>>) =
            Awaitable.GetTaskAwaiter(task.Invoke())

        member inline x.Source(async: Async<'T>) =
            async
            |> Async.StartImmediateAsTask
            |> x.Source

        member inline _.Source(seq: #seq<_>) = seq


type TaskBuilderRuntime() =
    inherit TaskBuilderBaseRuntime()

    member inline this.Run([<InlineIfLambda>] f: unit -> 'a) : Task<'T> =
        AsyncHelpers.awaitAwaiter (f ())
        |> Task.FromResult

    /// Used to force type inference to prefer Task<_> for parameters of functions using the build
    member inline _.Source(task: Task<'T>) = Awaitable.GetTaskAwaiter task


type BackgroundTaskBuilderRuntime() =
    inherit TaskBuilderBaseRuntime()

    member inline this.Run([<InlineIfLambda>] f: unit -> 'a) : Task<'T> =
        // backgroundTask { .. } escapes to a background thread where necessary
        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
        if
            isNull SynchronizationContext.Current
            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
        then
            AsyncHelpers.awaitAwaiter (f ())
            |> Task.FromResult
        else
            Task.Run<'T>(fun () -> AsyncHelpers.awaitAwaiter (f ()))


    /// Used to force type inference to prefer Task<_> for parameters of functions using the build
    member inline _.Source(task: Task<'T>) = Awaitable.GetTaskAwaiter task


type ValueTaskBuilderRuntime() =
    inherit TaskBuilderBaseRuntime()

    member inline this.Run([<InlineIfLambda>] f) : ValueTask<'T> =
        AsyncHelpers.awaitAwaiter (f ())
        |> ValueTask.FromResult


    /// Used to force type inference to prefer ValueTask<_> for parameters of functions using the build
    member inline _.Source(task: ValueTask<'T>) = Awaitable.GetAwaiter task


namespace IcedTasks.Polyfill.TasksRuntime

[<AutoOpen>]
module TaskBuilder =
    open IcedTasks.TaskBase_Net10
    let task = TaskBuilderRuntime()

    let backgroundTask = BackgroundTaskBuilderRuntime()

    let valueTask = ValueTaskBuilderRuntime()
