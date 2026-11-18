namespace IcedTasks.Polyfill.RuntimeTask

#if NET11_0_OR_GREATER

/// <summary>Contains the members that the runtime-async task builders share.</summary>
/// <remarks>
/// The .NET runtime, not a compiler-generated state machine, suspends and resumes these builders.
/// The F# compiler can lower a builder to runtime async only when it inlines all of the builder members.
/// Thus, each member is inline and has <c>NoDynamicInvocation</c>.
/// </remarks>
[<AutoOpen>]
module RuntimeTaskBase =
    open System
    open System.Collections.Generic
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core.CompilerServices
    open IcedTasks.Nullness
    open IcedTasks.TaskLike

    /// <summary>An operation that started before the builder binds it.</summary>
    /// <remarks>Invoke suspends the runtime-async method until the operation completes, and then returns the result.</remarks>
    type Started<'T> = delegate of unit -> 'T

    /// <exclude/>
    module RuntimeAwait =

        /// <summary>Suspends the runtime-async method until the awaiter completes, and then returns its result.</summary>
        [<NoDynamicInvocation>]
        let inline awaitAwaiter<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
            (awaiter: 'Awaiter)
            : 'TResult =
            if not (Awaiter.IsCompleted awaiter) then
                AsyncHelpers.UnsafeAwaitAwaiter awaiter

            Awaiter.GetResult awaiter

        /// <summary>Starts an awaiter-based operation for the builder to bind.</summary>
        [<NoDynamicInvocation>]
        let inline started<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
            (awaiter: 'Awaiter)
            : Started<'TResult> =
            Started(fun () -> awaitAwaiter awaiter)

    /// <summary>The base class of the runtime-async task builders.</summary>
    /// <remarks>
    /// The body of the computation expression returns its result directly.
    /// A <c>let!</c> suspends the runtime-async method with <c>System.Runtime.CompilerServices.AsyncHelpers</c>.
    /// </remarks>
    type RuntimeTaskBuilderBase() =

        /// <summary>Delays the computation until the builder runs it.</summary>
        [<NoDynamicInvocation>]
        member inline _.Delay([<InlineIfLambda>] generator: unit -> 'T) = generator

        /// <summary>Creates a computation that does nothing.</summary>
        [<NoDynamicInvocation>]
        member inline _.Zero() = ()

        /// <summary>Returns the value as the result of the computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Return(value: 'T) = value

        /// <summary>Runs the second computation after the first computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Combine(first: unit, [<InlineIfLambda>] second: unit -> 'T) =
            ignore first
            second ()

        /// <summary>Runs the body while the guard returns true.</summary>
        [<NoDynamicInvocation>]
        member inline _.While
            ([<InlineIfLambda>] guard: unit -> bool, [<InlineIfLambda>] body: unit -> unit)
            =
            while guard () do
                body ()

        /// <summary>Runs the body, and runs the handler if the body throws an exception.</summary>
        [<NoDynamicInvocation>]
        member inline _.TryWith
            ([<InlineIfLambda>] body: unit -> 'T, [<InlineIfLambda>] handler: exn -> 'T)
            =
            try
                body ()
            with ex ->
                handler ex

        /// <summary>Runs the body, and then runs the compensation, also if the body throws an exception.</summary>
        [<NoDynamicInvocation>]
        member inline _.TryFinally
            ([<InlineIfLambda>] body: unit -> 'T, [<InlineIfLambda>] compensation: unit -> unit)
            =
            try
                body ()
            finally
                compensation ()

        /// <summary>Runs the body with the resource, and then awaits <c>DisposeAsync</c> on the resource.</summary>
        /// <remarks>The builder does not dispose a null resource.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Using
            (resource: #IAsyncDisposableNull, [<InlineIfLambda>] body: #IAsyncDisposableNull -> 'T)
            =
            try
                body resource
            finally
                if not (isNull (box resource)) then
                    AsyncHelpers.Await(resource.DisposeAsync())

        /// <summary>Runs the body for each element of the sequence.</summary>
        [<NoDynamicInvocation>]
        member inline _.For(sequence: seq<'T>, [<InlineIfLambda>] body: 'T -> unit) =
            for item in sequence do
                body item

        /// <summary>Runs the body for each element of the asynchronous sequence.</summary>
        /// <remarks>The builder awaits <c>MoveNextAsync</c> for each element, and awaits <c>DisposeAsync</c> on the enumerator at the end.</remarks>
        [<NoDynamicInvocation>]
        member inline this.For
            (sequence: IAsyncEnumerable<'T>, [<InlineIfLambda>] body: 'T -> unit)
            =
            this.Using(
                sequence.GetAsyncEnumerator CancellationToken.None,
                fun enumerator ->
                    while AsyncHelpers.Await(enumerator.MoveNextAsync()) do
                        body enumerator.Current
            )

        /// <summary>Awaits the started operation, and then gives its result to the continuation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Bind
            ([<InlineIfLambda>] started: Started<'T>, [<InlineIfLambda>] continuation: 'T -> 'U)
            =
            continuation (started.Invoke())

        /// <summary>Awaits the started operation, and then gives its result to the mapper.</summary>
        [<NoDynamicInvocation>]
        member inline _.BindReturn
            ([<InlineIfLambda>] started: Started<'T>, [<InlineIfLambda>] mapper: 'T -> 'U)
            =
            mapper (started.Invoke())

        /// <summary>Awaits the started operation, and returns its result as the result of the computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.ReturnFrom([<InlineIfLambda>] started: Started<'T>) = started.Invoke()

        /// <summary>Combines two started operations for <c>and!</c>.</summary>
        /// <remarks>Both operations start before the builder awaits the left operation.</remarks>
        [<NoDynamicInvocation>]
        member inline _.MergeSources
            ([<InlineIfLambda>] left: Started<'T1>, [<InlineIfLambda>] right: Started<'T2>)
            =
            Started(fun () ->
                let leftResult = left.Invoke()
                let rightResult = right.Invoke()
                struct (leftResult, rightResult)
            )

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =

        type RuntimeTaskBuilderBase with

            /// <summary>Starts an awaiter-based operation for the builder to bind.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
                (awaiter: 'Awaiter)
                : Started<'TResult> =
                RuntimeAwait.started awaiter

            /// <summary>Gets the awaiter of the awaitable, and starts it for the builder to bind.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'Awaiter, 'TResult
                when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
                (awaitable: 'Awaitable)
                : Started<'TResult> =
                RuntimeAwait.started (Awaitable.GetAwaiter awaitable)

            /// <summary>Runs the body with the resource, and then calls <c>Dispose</c> on the resource.</summary>
            /// <remarks>The builder does not dispose a null resource.</remarks>
            [<NoDynamicInvocation>]
            member inline _.Using
                (resource: #IDisposableNull, [<InlineIfLambda>] body: #IDisposableNull -> 'T)
                =
                try
                    body resource
                finally
                    if not (isNull (box resource)) then
                        resource.Dispose()

            /// <summary>Gives the sequence to <c>For</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(sequence: #seq<_>) : #seq<_> = sequence

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        type RuntimeTaskBuilderBase with

            /// <summary>Gives the asynchronous sequence to <c>For</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(sequence: #IAsyncEnumerable<_>) = sequence

            /// <summary>Gives the task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(task: Task<'T>) =
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Gives the task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(task: Task) =
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Gives the value task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(task: ValueTask<'T>) =
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Gives the value task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(task: ValueTask) =
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Starts the computation with <c>Async.StartImmediateAsTask</c>, and gives the task to the builder.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(computation: Async<'T>) =
                let task = Async.StartImmediateAsTask computation
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Starts the cold task, and gives its task to the builder.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(coldTask: unit -> Task<'T>) =
                let task = coldTask ()
                Started(fun () -> AsyncHelpers.Await task)

            /// <summary>Starts the cold task, and gives its task to the builder.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(coldTask: unit -> Task) =
                let task = coldTask ()
                Started(fun () -> AsyncHelpers.Await task)

/// <summary>Contains the runtime-async <c>task</c>, <c>backgroundTask</c>, and <c>valueTask</c> builders.</summary>
/// <remarks>
/// Open <c>IcedTasks.Polyfill.RuntimeTask</c> to use these builders in place of the FSharp.Core and IcedTasks builders.
/// The project that opens the namespace must target <c>net11.0</c> or later and set <c>LangVersion</c> to <c>preview</c>.
/// </remarks>
[<AutoOpen>]
module RuntimeTasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers

    /// <summary>Builds a <c>Task&lt;'T&gt;</c> as a .NET runtime-async method.</summary>
    type RuntimeTaskBuilder() =
        inherit RuntimeTaskBuilderBase()

        /// <summary>Runs the computation as a runtime-async method, and returns its task.</summary>
        [<NoDynamicInvocation>]
        member inline _.Run([<InlineIfLambda>] code: unit -> 'T) : Task<'T> =
            __runtimeAsyncReturn (code ())

        /// <summary>Gives the task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>Task&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source(task: Task<'T>) =
            Started(fun () -> AsyncHelpers.Await task)

    /// <summary>Builds a <c>Task&lt;'T&gt;</c> as a .NET runtime-async method that does not run on the caller context.</summary>
    /// <remarks>
    /// If the caller has a <c>SynchronizationContext</c> or a task scheduler that is not the default,
    /// the builder starts the computation on the thread pool with <c>Task.Run</c>.
    /// </remarks>
    type RuntimeBackgroundTaskBuilder() =
        inherit RuntimeTaskBuilderBase()

        /// <summary>Runs the computation as a runtime-async method on the thread pool, and returns its task.</summary>
        [<NoDynamicInvocation>]
        member inline _.Run([<InlineIfLambda>] code: unit -> 'T) : Task<'T> =
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                __runtimeAsyncReturn (code ())
            else
                Task.Run<'T>(fun () -> __runtimeAsyncReturn (code ()))

        /// <summary>Gives the task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>Task&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source(task: Task<'T>) =
            Started(fun () -> AsyncHelpers.Await task)

    /// <summary>Builds a <c>ValueTask&lt;'T&gt;</c> as a .NET runtime-async method.</summary>
    type RuntimeValueTaskBuilder() =
        inherit RuntimeTaskBuilderBase()

        /// <summary>Runs the computation as a runtime-async method, and returns its value task.</summary>
        [<NoDynamicInvocation>]
        member inline _.Run([<InlineIfLambda>] code: unit -> 'T) : ValueTask<'T> =
            __runtimeAsyncReturnValueTask (code ())

        /// <summary>Gives the value task to the builder, which awaits it with <c>AsyncHelpers.Await</c>.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>ValueTask&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source(task: ValueTask<'T>) =
            Started(fun () -> AsyncHelpers.Await task)

    /// <summary>Builds a <c>Task&lt;'T&gt;</c> as a .NET runtime-async method.</summary>
    /// <example>
    /// <code>
    /// open IcedTasks.Polyfill.RuntimeTask
    ///
    /// let readLength (path: string) =
    ///     task {
    ///         let! text = System.IO.File.ReadAllTextAsync path
    ///         return text.Length
    ///     }
    /// </code>
    /// </example>
    let task = RuntimeTaskBuilder()

    /// <summary>Builds a <c>Task&lt;'T&gt;</c> as a .NET runtime-async method that does not run on the caller context.</summary>
    let backgroundTask = RuntimeBackgroundTaskBuilder()

    /// <summary>Builds a <c>ValueTask&lt;'T&gt;</c> as a .NET runtime-async method.</summary>
    let valueTask = RuntimeValueTaskBuilder()

#endif
