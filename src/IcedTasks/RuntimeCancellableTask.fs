namespace IcedTasks.Polyfill.RuntimeTask

#if NET11_0_OR_GREATER

/// <summary>Contains the members that the runtime-async cancellable task builders share.</summary>
/// <remarks>
/// The body of a cancellable computation expression is a function of the ambient <c>CancellationToken</c>.
/// Each member is inline and has <c>NoDynamicInvocation</c>, so that the compiler can make one runtime-async method.
/// </remarks>
[<AutoOpen>]
module RuntimeCancellableTaskBase =
    open System
    open System.Collections.Generic
    open System.Runtime.CompilerServices
    open System.Runtime.ExceptionServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open IcedTasks.Nullness
    open IcedTasks.TaskLike

    /// <summary>A bound value that needs the ambient <c>CancellationToken</c>.</summary>
    /// <remarks>
    /// Invoke starts the operation with the token, suspends the runtime-async method until the operation completes, and then returns the result.
    /// </remarks>
    type WithToken<'T> = delegate of CancellationToken -> 'T

    /// <exclude/>
    module RuntimeCancellableAwait =

        /// <summary>Starts the token-dependent awaitable with the token, and awaits it.</summary>
        [<NoDynamicInvocation>]
        let inline withToken<'Awaitable, 'Awaiter, 'TResult
            when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
            ([<InlineIfLambda>] start: CancellationToken -> 'Awaitable)
            : WithToken<'TResult> =
            WithToken(fun ct -> RuntimeAwait.awaitAwaiter (Awaitable.GetAwaiter(start ct)))

    /// <summary>The base class of the runtime-async cancellable task builders.</summary>
    /// <remarks>
    /// The builder checks the ambient token before each <c>let!</c>, <c>do!</c>, <c>return!</c>, and <c>and!</c>.
    /// It gives the token to each bound <c>CancellationToken -> 'Awaitable</c>, <c>Async</c>, and <c>IAsyncEnumerable</c>.
    /// </remarks>
    type RuntimeCancellableTaskBuilderBase() =

        /// <summary>Delays the computation until the builder runs it.</summary>
        [<NoDynamicInvocation>]
        member inline _.Delay
            ([<InlineIfLambda>] generator: unit -> (CancellationToken -> 'T))
            : CancellationToken -> 'T =
            fun ct -> generator () ct

        /// <summary>Creates a computation that does nothing.</summary>
        [<NoDynamicInvocation>]
        member inline _.Zero() : CancellationToken -> unit = fun _ -> ()

        /// <summary>Returns the value as the result of the computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Return(value: 'T) : CancellationToken -> 'T = fun _ -> value

        /// <summary>Runs the second computation after the first computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Combine
            (
                [<InlineIfLambda>] first: CancellationToken -> unit,
                [<InlineIfLambda>] second: CancellationToken -> 'T
            ) : CancellationToken -> 'T =
            fun ct ->
                first ct
                second ct

        /// <summary>Runs the body while the guard returns true.</summary>
        [<NoDynamicInvocation>]
        member inline _.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                [<InlineIfLambda>] body: CancellationToken -> unit
            ) : CancellationToken -> unit =
            fun ct ->
                while guard () do
                    body ct

        /// <summary>Runs the body, and runs the handler if the body throws an exception.</summary>
        [<NoDynamicInvocation>]
        member inline _.TryWith
            (
                [<InlineIfLambda>] body: CancellationToken -> 'T,
                [<InlineIfLambda>] handler: exn -> CancellationToken -> 'T
            ) : CancellationToken -> 'T =
            fun ct ->
                try
                    body ct
                with ex ->
                    handler ex ct

        /// <summary>Runs the body, and then runs the compensation, also if the body throws an exception.</summary>
        [<NoDynamicInvocation>]
        member inline _.TryFinally
            (
                [<InlineIfLambda>] body: CancellationToken -> 'T,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : CancellationToken -> 'T =
            fun ct ->
                try
                    body ct
                finally
                    compensation ()

        /// <summary>Runs the body with the resource, and then awaits <c>DisposeAsync</c> on the resource.</summary>
        /// <remarks>
        /// The builder does not dispose a null resource, and it does not check the token before <c>DisposeAsync</c>.
        /// If the body throws, the builder awaits <c>DisposeAsync</c> and then throws the body exception with its original stack trace.
        /// If <c>DisposeAsync</c> throws, its exception replaces the body exception.
        /// </remarks>
        [<NoDynamicInvocation>]
        member inline _.Using
            (
                resource: #IAsyncDisposableNull,
                [<InlineIfLambda>] body: #IAsyncDisposableNull -> CancellationToken -> 'T
            ) : CancellationToken -> 'T =
            fun ct ->
                let mutable failure: ExceptionDispatchInfo | null = null

                let result =
                    try
                        body resource ct
                    with ex ->
                        failure <- ExceptionDispatchInfo.Capture ex
                        Unchecked.defaultof<'T>

                // A rethrow after a suspending finally loses the stack trace: https://github.com/dotnet/fsharp/issues/20869
                if not (isNull (box resource)) then
                    AsyncHelpers.Await(resource.DisposeAsync())

                match failure with
                | null -> result
                | failure ->
                    failure.Throw()
                    result

        /// <summary>Runs the body for each element of the sequence.</summary>
        [<NoDynamicInvocation>]
        member inline _.For
            (sequence: seq<'T>, [<InlineIfLambda>] body: 'T -> CancellationToken -> unit)
            : CancellationToken -> unit =
            fun ct ->
                for item in sequence do
                    body item ct

        /// <summary>Runs the body for each element of the asynchronous sequence.</summary>
        /// <remarks>The builder gives the ambient token to <c>GetAsyncEnumerator</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline this.For
            (
                sequence: IAsyncEnumerable<'T>,
                [<InlineIfLambda>] body: 'T -> CancellationToken -> unit
            ) : CancellationToken -> unit =
            fun ct ->
                this.Using
                    (sequence.GetAsyncEnumerator ct,
                     fun enumerator ct ->
                         while AsyncHelpers.Await(enumerator.MoveNextAsync()) do
                             body enumerator.Current ct)
                    ct

        /// <summary>Checks the token, awaits the operation, and then gives its result to the continuation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Bind
            (
                [<InlineIfLambda>] started: Started<'T>,
                [<InlineIfLambda>] continuation: 'T -> CancellationToken -> 'U
            ) : CancellationToken -> 'U =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                continuation (started.Invoke()) ct

        /// <summary>Checks the token, starts the operation with the token, awaits it, and then gives its result to the continuation.</summary>
        [<NoDynamicInvocation>]
        member inline _.Bind
            (
                [<InlineIfLambda>] withToken: WithToken<'T>,
                [<InlineIfLambda>] continuation: 'T -> CancellationToken -> 'U
            ) : CancellationToken -> 'U =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                continuation (withToken.Invoke ct) ct

        /// <summary>Checks the token, awaits the operation, and then gives its result to the mapper.</summary>
        [<NoDynamicInvocation>]
        member inline _.BindReturn
            ([<InlineIfLambda>] started: Started<'T>, [<InlineIfLambda>] mapper: 'T -> 'U)
            : CancellationToken -> 'U =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                mapper (started.Invoke())

        /// <summary>Checks the token, starts the operation with the token, awaits it, and then gives its result to the mapper.</summary>
        [<NoDynamicInvocation>]
        member inline _.BindReturn
            ([<InlineIfLambda>] withToken: WithToken<'T>, [<InlineIfLambda>] mapper: 'T -> 'U)
            : CancellationToken -> 'U =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                mapper (withToken.Invoke ct)

        /// <summary>Checks the token, awaits the operation, and returns its result as the result of the computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.ReturnFrom
            ([<InlineIfLambda>] started: Started<'T>)
            : CancellationToken -> 'T =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                started.Invoke()

        /// <summary>Checks the token, starts the operation with the token, awaits it, and returns its result as the result of the computation.</summary>
        [<NoDynamicInvocation>]
        member inline _.ReturnFrom
            ([<InlineIfLambda>] withToken: WithToken<'T>)
            : CancellationToken -> 'T =
            fun ct ->
                ct.ThrowIfCancellationRequested()
                withToken.Invoke ct

        /// <summary>Combines two operations for <c>and!</c>.</summary>
        [<NoDynamicInvocation>]
        member inline _.MergeSources
            ([<InlineIfLambda>] left: Started<'T1>, [<InlineIfLambda>] right: Started<'T2>)
            =
            Started(fun () ->
                let leftResult = left.Invoke()
                let rightResult = right.Invoke()
                struct (leftResult, rightResult)
            )

        /// <summary>Combines two operations for <c>and!</c>.</summary>
        /// <remarks>The builder starts the left operation, then the right operation, and then awaits them from left to right.</remarks>
        [<NoDynamicInvocation>]
        member inline _.MergeSources
            ([<InlineIfLambda>] left: WithToken<'T1>, [<InlineIfLambda>] right: WithToken<'T2>)
            =
            WithToken(fun ct ->
                let leftTask = __runtimeAsyncReturnValueTask (left.Invoke ct)
                let rightTask = __runtimeAsyncReturnValueTask (right.Invoke ct)
                let leftResult = AsyncHelpers.Await leftTask
                let rightResult = AsyncHelpers.Await rightTask
                struct (leftResult, rightResult)
            )

        /// <summary>Combines two operations for <c>and!</c>.</summary>
        /// <remarks>The builder starts the right operation, and then awaits the operations from left to right.</remarks>
        [<NoDynamicInvocation>]
        member inline _.MergeSources
            ([<InlineIfLambda>] left: Started<'T1>, [<InlineIfLambda>] right: WithToken<'T2>)
            =
            WithToken(fun ct ->
                let rightTask = __runtimeAsyncReturnValueTask (right.Invoke ct)
                let leftResult = left.Invoke()
                let rightResult = AsyncHelpers.Await rightTask
                struct (leftResult, rightResult)
            )

        /// <summary>Combines two operations for <c>and!</c>.</summary>
        /// <remarks>The builder starts the left operation, and then awaits the operations from left to right.</remarks>
        [<NoDynamicInvocation>]
        member inline _.MergeSources
            ([<InlineIfLambda>] left: WithToken<'T1>, [<InlineIfLambda>] right: Started<'T2>)
            =
            WithToken(fun ct ->
                let leftTask = __runtimeAsyncReturnValueTask (left.Invoke ct)
                let leftResult = AsyncHelpers.Await leftTask
                let rightResult = right.Invoke()
                struct (leftResult, rightResult)
            )

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =

        type RuntimeCancellableTaskBuilderBase with

            /// <summary>Wraps the awaiter for the builder to await.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
                (awaiter: 'Awaiter)
                : Started<'TResult> =
                RuntimeAwait.started awaiter

            /// <summary>Gets the awaiter of the awaitable, and wraps it for the builder to await.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'Awaiter, 'TResult
                when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
                (awaitable: 'Awaitable)
                : Started<'TResult> =
                RuntimeAwait.started (Awaitable.GetAwaiter awaitable)

            /// <summary>Gives the ambient token to the function when the builder binds it.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'Awaiter, 'TResult
                when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
                ([<InlineIfLambda>] cancellableAwaitable: CancellationToken -> 'Awaitable)
                : WithToken<'TResult> =
                RuntimeCancellableAwait.withToken cancellableAwaitable

            /// <summary>Calls the function when the builder binds it.</summary>
            [<NoDynamicInvocation; NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'Awaiter, 'TResult
                when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
                ([<InlineIfLambda>] coldAwaitable: unit -> 'Awaitable)
                : WithToken<'TResult> =
                RuntimeCancellableAwait.withToken (fun _ -> coldAwaitable ())

            /// <summary>Runs the body with the resource, and then calls <c>Dispose</c> on the resource.</summary>
            /// <remarks>The builder does not dispose a null resource.</remarks>
            [<NoDynamicInvocation>]
            member inline _.Using
                (
                    resource: #IDisposableNull,
                    [<InlineIfLambda>] body: #IDisposableNull -> CancellationToken -> 'T
                ) : CancellationToken -> 'T =
                fun ct ->
                    try
                        body resource ct
                    finally
                        if not (isNull (box resource)) then
                            resource.Dispose()

            /// <summary>Gives the sequence to <c>For</c>.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(sequence: #seq<_>) : #seq<_> = sequence

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        type RuntimeCancellableTaskBuilderBase with

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

            /// <summary>Starts the computation with the ambient token when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source(computation: Async<'T>) =
                WithToken(fun ct ->
                    AsyncHelpers.Await(
                        Async.StartImmediateAsTask(computation, cancellationToken = ct)
                    )
                )

            /// <summary>Calls the cancellable task with the ambient token when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source
                ([<InlineIfLambda>] cancellableTask: CancellationToken -> Task<'T>)
                =
                WithToken(fun ct -> AsyncHelpers.Await(cancellableTask ct))

            /// <summary>Calls the cancellable task with the ambient token when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source([<InlineIfLambda>] cancellableTask: CancellationToken -> Task) =
                WithToken(fun ct -> AsyncHelpers.Await(cancellableTask ct))

            /// <summary>Calls the cancellable value task with the ambient token when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source
                ([<InlineIfLambda>] cancellableValueTask: CancellationToken -> ValueTask<'T>)
                =
                WithToken(fun ct -> AsyncHelpers.Await(cancellableValueTask ct))

            /// <summary>Calls the cancellable value task with the ambient token when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source
                ([<InlineIfLambda>] cancellableValueTask: CancellationToken -> ValueTask)
                =
                WithToken(fun ct -> AsyncHelpers.Await(cancellableValueTask ct))

            /// <summary>Calls the cold task when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source([<InlineIfLambda>] coldTask: unit -> Task<'T>) =
                WithToken(fun _ -> AsyncHelpers.Await(coldTask ()))

            /// <summary>Calls the cold task when the builder binds it.</summary>
            [<NoDynamicInvocation>]
            member inline _.Source([<InlineIfLambda>] coldTask: unit -> Task) =
                WithToken(fun _ -> AsyncHelpers.Await(coldTask ()))

/// <summary>Contains the runtime-async <c>cancellableTask</c>, <c>backgroundCancellableTask</c>, and <c>cancellableValueTask</c> builders.</summary>
/// <remarks>
/// Open <c>IcedTasks.Polyfill.RuntimeTask</c> to use these builders in place of the IcedTasks state-machine builders.
/// The project that opens the namespace must target <c>net11.0</c> or later and set <c>LangVersion</c> to <c>preview</c>.
/// </remarks>
[<AutoOpen>]
module RuntimeCancellableTasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers

    /// <summary>Builds a cancellable task, <c>CancellationToken -> Task&lt;'T&gt;</c>, as a .NET runtime-async method.</summary>
    /// <remarks>
    /// Each call runs the computation again with the token that the caller gives.
    /// If the token is cancelled at the call, the task is cancelled and the computation does not run.
    /// </remarks>
    type RuntimeCancellableTaskBuilder() =
        inherit RuntimeCancellableTaskBuilderBase()

        /// <summary>Returns a function that runs the computation as a runtime-async method with the token that it gets.</summary>
        [<NoDynamicInvocation>]
        member inline _.Run
            ([<InlineIfLambda>] code: CancellationToken -> 'T)
            : CancellationToken -> Task<'T> =
            fun ct ->
                __runtimeAsyncReturn (
                    ct.ThrowIfCancellationRequested()
                    code ct
                )

        /// <summary>Calls the cancellable task with the ambient token when the builder binds it.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>CancellationToken -> Task&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source([<InlineIfLambda>] cancellableTask: CancellationToken -> Task<'T>) =
            WithToken(fun ct -> AsyncHelpers.Await(cancellableTask ct))

    /// <summary>Builds a cancellable task, <c>CancellationToken -> Task&lt;'T&gt;</c>, as a .NET runtime-async method that does not run on the caller context.</summary>
    /// <remarks>
    /// Each call runs the computation again with the token that the caller gives.
    /// If the caller has a <c>SynchronizationContext</c> or a task scheduler that is not the default,
    /// the call starts the computation on the thread pool with <c>Task.Run</c>.
    /// </remarks>
    type RuntimeBackgroundCancellableTaskBuilder() =
        inherit RuntimeCancellableTaskBuilderBase()

        /// <summary>Returns a function that runs the computation as a runtime-async method with the token that it gets.</summary>
        /// <remarks>
        /// If the caller has a <c>SynchronizationContext</c> or a task scheduler that is not the default, the computation starts on the thread pool.
        /// Otherwise, the computation starts on the caller thread.
        /// </remarks>
        [<NoDynamicInvocation>]
        member inline _.Run
            ([<InlineIfLambda>] code: CancellationToken -> 'T)
            : CancellationToken -> Task<'T> =
            fun ct ->
                if
                    isNull SynchronizationContext.Current
                    && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                then
                    __runtimeAsyncReturn (
                        ct.ThrowIfCancellationRequested()
                        code ct
                    )
                else
                    Task.Run<'T>(
                        (fun () ->
                            __runtimeAsyncReturn (
                                ct.ThrowIfCancellationRequested()
                                code ct
                            )
                        ),
                        ct
                    )

        /// <summary>Calls the cancellable task with the ambient token when the builder binds it.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>CancellationToken -> Task&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source([<InlineIfLambda>] cancellableTask: CancellationToken -> Task<'T>) =
            WithToken(fun ct -> AsyncHelpers.Await(cancellableTask ct))

    /// <summary>Builds a cancellable value task, <c>CancellationToken -> ValueTask&lt;'T&gt;</c>, as a .NET runtime-async method.</summary>
    /// <remarks>
    /// Each call runs the computation again with the token that the caller gives.
    /// If the token is cancelled at the call, the value task is cancelled and the computation does not run.
    /// </remarks>
    type RuntimeCancellableValueTaskBuilder() =
        inherit RuntimeCancellableTaskBuilderBase()

        /// <summary>Returns a function that runs the computation as a runtime-async method with the token that it gets.</summary>
        [<NoDynamicInvocation>]
        member inline _.Run
            ([<InlineIfLambda>] code: CancellationToken -> 'T)
            : CancellationToken -> ValueTask<'T> =
            fun ct ->
                __runtimeAsyncReturnValueTask (
                    ct.ThrowIfCancellationRequested()
                    code ct
                )

        /// <summary>Calls the cancellable value task with the ambient token when the builder binds it.</summary>
        /// <remarks>This member is on the builder type so that type inference prefers <c>CancellationToken -> ValueTask&lt;'T&gt;</c>.</remarks>
        [<NoDynamicInvocation>]
        member inline _.Source
            ([<InlineIfLambda>] cancellableValueTask: CancellationToken -> ValueTask<'T>)
            =
            WithToken(fun ct -> AsyncHelpers.Await(cancellableValueTask ct))

    /// <summary>Builds a cancellable task, <c>CancellationToken -> Task&lt;'T&gt;</c>, as a .NET runtime-async method.</summary>
    let cancellableTask = RuntimeCancellableTaskBuilder()

    /// <summary>Builds a cancellable task, <c>CancellationToken -> Task&lt;'T&gt;</c>, as a .NET runtime-async method that does not run on the caller context.</summary>
    let backgroundCancellableTask = RuntimeBackgroundCancellableTaskBuilder()

    /// <summary>Builds a cancellable value task, <c>CancellationToken -> ValueTask&lt;'T&gt;</c>, as a .NET runtime-async method.</summary>
    let cancellableValueTask = RuntimeCancellableValueTaskBuilder()

#endif
