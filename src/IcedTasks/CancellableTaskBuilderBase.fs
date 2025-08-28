namespace IcedTasks

/// Contains methods to build Tasks using the F# computation expression syntax
[<AutoOpen>]
module CancellableTaskBase =
    open System
    open System.Runtime.CompilerServices
    open System.Runtime.ExceptionServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections
    open System.Collections.Generic

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableTaskBaseStateMachineData<'T, 'Builder> =
        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: 'Builder

        /// <summary>Throws a <see cref="T:System.OperationCanceledException" /> if this token has had cancellation requested.</summary>
        /// <exception cref="T:System.OperationCanceledException">The token has had cancellation requested.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The associated <see cref="T:System.Threading.CancellationTokenSource" /> has been disposed.</exception>
        member inline this.ThrowIfCancellationRequested() =
            this.CancellationToken.ThrowIfCancellationRequested()

    /// This is used by the compiler as a template for creating state machine structs
    and CancellableTaskBaseStateMachine<'TOverall, 'Builder> =
        ResumableStateMachine<CancellableTaskBaseStateMachineData<'TOverall, 'Builder>>

    /// Represents the runtime continuation of a cancellableTasks state machine created dynamically
    and CancellableTaskBaseResumptionFunc<'TOverall, 'Builder> =
        ResumptionFunc<CancellableTaskBaseStateMachineData<'TOverall, 'Builder>>

    /// Represents the runtime continuation of a cancellableTasks state machine created dynamically
    and CancellableTaskBaseResumptionDynamicInfo<'TOverall, 'Builder> =
        ResumptionDynamicInfo<CancellableTaskBaseStateMachineData<'TOverall, 'Builder>>

    /// A special compiler-recognised delegate type for specifying blocks of cancellableTasks code with access to the state machine
    and CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
        ResumableCode<CancellableTaskBaseStateMachineData<'TOverall, 'Builder>, 'T>

    let inline yieldOnBindLimit bounce =
        CancellableTaskBaseCode(fun sm ->
            if bounce then
                let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)

                if not __stack_yield_fin then
                    MethodBuilder.AwaitUnsafeOnCompleted(
                        &sm.Data.MethodBuilder,
                        Trampoline.Current.Ref,
                        &sm
                    )

                __stack_yield_fin
            else
                true
        )

    /// <summary>
    /// Contains methods to build TaskLikes using the F# computation expression syntax
    /// </summary>
    type CancellableTaskBuilderBase() =
        /// <summary>Creates a CancellableTasks that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A CancellableTasks that runs generator</returns>
        member inline _.Delay
            (generator: unit -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>)
            : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Delay(generator)

        /// <summary>Creates A CancellableTasks that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// cancellableTasks { ... } computation expression syntax.
        /// </remarks>
        /// <returns>A CancellableTasks that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.Zero()

        /// <summary>Creates A Computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// cancellableTasks { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>A cancellableTasks that returns value when executed.</returns>
        member inline _.Return(value: 'T) : CancellableTaskBaseCode<'T, 'T, 'Builder> =
            CancellableTaskBaseCode<'T, 'T, 'Builder>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates a CancellableTasks that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// cancellableTasks { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>A CancellableTasks that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: CancellableTaskBaseCode<'TOverall, unit, 'Builder>,
                task2: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates A CancellableTasks that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// cancellableTasks { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>A CancellableTasks that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                computation: CancellableTaskBaseCode<'TOverall, unit, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates A CancellableTasks that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// cancellableTasks { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>A CancellableTasks that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>,
                catchHandler: exn -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates A CancellableTasks that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// cancellableTasks { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>A CancellableTasks that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinally(
                computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates a CancellableTask that enumerates the sequence seq
        /// on demand and runs body for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of for in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// A CancellableTask.  Can be seen as the body of the for expression.</param>
        ///
        /// <returns>A CancellableTask that will enumerate the sequence and run body
        /// for each element.</returns>
        member inline _.For
            (sequence: seq<'T>, body: 'T -> CancellableTaskBaseCode<'TOverall, unit, 'Builder>)
            : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.For(sequence, body)


    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic
                (
                    sm:
                        byref<
                            ResumableStateMachine<
                                CancellableTaskBaseStateMachineData<'TOverall, 'Builder>
                             >
                         >,
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter sm.Data.CancellationToken

                let cont =
                    (CancellableTaskBaseResumptionFunc<'TOverall, _>(fun sm ->
                        let result = Awaiter.GetResult awaiter
                        (continuation result).Invoke(&sm)
                    ))

                // shortcut to continue immediately
                if Awaiter.IsCompleted awaiter then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <-
                        (awaiter :> ICriticalNotifyCompletion)

                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false

            /// <summary>Creates A CancellableTask that runs computation, and when
            /// computation generates a result T, runs binder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of let! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of computation.</param>
            ///
            /// <returns>A CancellableTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind
                (
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder> =

                CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.ThrowIfCancellationRequested()
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = getAwaiter sm.Data.CancellationToken

                        let mutable __stack_fin = true

                        if not (Awaiter.IsCompleted awaiter) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = ExceptionCache.GetResultOrThrow awaiter
                            (continuation result).Invoke(&sm)
                        else
                            let mutable awaiter = awaiter :> ICriticalNotifyCompletion

                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                            false
                    else
                        CancellableTaskBuilderBase.BindDynamic(&sm, getAwaiter, continuation)
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of return! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom
                ([<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter)
                =
                this.Bind(
                    getAwaiter = (fun ct -> getAwaiter ct),
                    continuation = (fun v -> this.Return v)
                )


            [<NoEagerConstraintApplication>]
            member inline this.BindReturn
                (
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    [<InlineIfLambda>] mapper: 'TResult1 -> 'TResult2
                ) : CancellableTaskBaseCode<_, _, _> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return(mapper v)))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellationToken -> 'Awaitable into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] cancellableAwaitable: CancellationToken -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct -> Awaitable.GetAwaiter(cancellableAwaitable ct))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a unit -> 'Awaitable into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] coldAwaitable: unit -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct -> Awaitable.GetAwaiter(coldAwaitable ()))

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic
                (
                    sm:
                        byref<
                            ResumableStateMachine<
                                CancellableTaskBaseStateMachineData<'TOverall, 'Builder>
                             >
                         >,
                    awaiter: 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()
                let mutable awaiter = awaiter

                let cont =
                    (CancellableTaskBaseResumptionFunc<'TOverall, 'Builder>(fun sm ->
                        let result = Awaiter.GetResult awaiter
                        (continuation result).Invoke(&sm)
                    ))

                // shortcut to continue immediately
                if Awaiter.IsCompleted awaiter then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <-
                        (awaiter :> ICriticalNotifyCompletion)

                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false


            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline internal BindDynamicNoCancellation
                (
                    sm:
                        byref<
                            ResumableStateMachine<
                                CancellableTaskBaseStateMachineData<'TOverall, 'Builder>
                             >
                         >,
                    awaiter: 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : bool =
                let mutable awaiter = awaiter

                let cont =
                    (CancellableTaskBaseResumptionFunc<'TOverall, 'Builder>(fun sm ->
                        let result = Awaiter.GetResult awaiter
                        (continuation result).Invoke(&sm)
                    ))

                // shortcut to continue immediately
                if Awaiter.IsCompleted awaiter then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <-
                        (awaiter :> ICriticalNotifyCompletion)

                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false


            /// <summary>Creates A CancellableTask that runs computation, and when
            /// computation generates a result T, runs binder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of let! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of computation.</param>
            ///
            /// <returns>A CancellableTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline internal _.BindNoCancellation
                (
                    awaiter: 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder> =

                CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = awaiter

                        let mutable __stack_fin = true

                        if not (Awaiter.IsCompleted awaiter) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = Awaiter.GetResult awaiter
                            (continuation result).Invoke(&sm)
                        else
                            let mutable awaiter = awaiter :> ICriticalNotifyCompletion

                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                            false
                    else
                        CancellableTaskBuilderBase.BindDynamicNoCancellation(
                            &sm,
                            awaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )

            /// <summary>Creates A CancellableTask that runs computation, and when
            /// computation generates a result T, runs binder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of let! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of computation.</param>
            ///
            /// <returns>A CancellableTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind
                (
                    awaiter: 'Awaiter,
                    continuation:
                        ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder> =

                CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.ThrowIfCancellationRequested()
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = awaiter

                        let mutable __stack_fin = true

                        if not (Awaiter.IsCompleted awaiter) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = ExceptionCache.GetResultOrThrow awaiter
                            (continuation result).Invoke(&sm)
                        else
                            let mutable awaiter = awaiter :> ICriticalNotifyCompletion

                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                            false
                    else
                        CancellableTaskBuilderBase.BindDynamic(&sm, awaiter, continuation)
                //-- RESUMABLE CODE END
                )

            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of return! in the
            /// task { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom
                (awaiter: 'Awaiter)
                : CancellableTaskBaseCode<_, _, 'Builder> =
                this.Bind(awaiter = awaiter, continuation = (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.BindReturn
                (awaiter: 'Awaiter, [<InlineIfLambda>] mapper: 'a -> 'TResult2)
                : CancellableTaskBaseCode<'TResult2, 'TResult2, 'Builder> =
                this.Bind(awaiter = awaiter, continuation = (fun v -> this.Return(mapper v)))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (awaiter: 'Awaiter)
                : 'Awaiter =
                awaiter


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a ^Awaitable into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (awaitable: 'Awaitable)
                : 'Awaiter =
                Awaitable.GetAwaiter awaitable


            /// <summary>Creates A CancellableTask that runs binder(resource).
            /// The action resource.Dispose() is executed as this computation yields its result
            /// or if the CancellableTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of use and use! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>A CancellableTask that binds and eventually disposes resource.</returns>
            ///
            member inline _.Using
                (
                    resource: #IDisposableNull,
                    binder: #IDisposableNull -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
                ) =
                ResumableCode.Using(resource, binder)


            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(items: #seq<_>) : #seq<_> = items


    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =


        type AsyncEx with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            ///
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableTask
                ([<InlineIfLambda>] t: CancellationToken -> Task<'T>)
                =
                asyncEx {
                    let! ct = Async.CancellationToken
                    return! t ct
                }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            ///
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableTask
                ([<InlineIfLambda>] t: CancellationToken -> Task)
                =
                asyncEx {
                    let! ct = Async.CancellationToken
                    return! t ct
                }

        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask
                ([<InlineIfLambda>] t: CancellationToken -> Task<'T>)
                =
                async {
                    let! ct = Async.CancellationToken

                    return!
                        t ct
                        |> Async.AwaitTask
                }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask
                ([<InlineIfLambda>] t: CancellationToken -> Task)
                =
                async {
                    let! ct = Async.CancellationToken

                    return!
                        t ct
                        |> Async.AwaitTask
                }

            /// <summary>Runs an asynchronous computation, starting on the current operating system thread.</summary>
            static member inline AsCancellableTask
                (computation: Async<'T>)
                : CancellationToken -> Task<_> =
                fun ct -> Async.StartImmediateAsTask(computation, cancellationToken = ct)

        // High priority extensions
        type CancellableTaskBuilderBase with

            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(asyncItems: #IAsyncEnumerable<_>) = asyncItems

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline _.Source(taskAwaiter: TaskAwaiter<'T>) : Awaiter<TaskAwaiter<'T>, 'T> =
                taskAwaiter

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline _.Source(taskT: Task<'T>) : Awaiter<TaskAwaiter<'T>, 'T> =
                Awaitable.GetTaskAwaiter taskT

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a ColdTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source
                ([<InlineIfLambda>] coldTaskAwaiter: unit -> TaskAwaiter<'T>)
                : CancellationToken -> Awaiter<TaskAwaiter<'T>, 'T> =
                (fun (ct: CancellationToken) -> (coldTaskAwaiter ()))

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a ColdTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source
                ([<InlineIfLambda>] coldTask: unit -> Task<'T>)
                : CancellationToken -> Awaiter<TaskAwaiter<'T>, 'T> =
                (fun (ct: CancellationToken) ->
                    Awaitable.GetTaskAwaiter(BindContext.SetIsBind coldTask ())
                )

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellableTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source
                ([<InlineIfLambda>] cancellableTaskAwaiter: CancellationToken -> TaskAwaiter<'T>)
                : CancellationToken -> Awaiter<TaskAwaiter<'T>, 'T> =
                (fun ct -> (cancellableTaskAwaiter ct))

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellableTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source
                ([<InlineIfLambda>] cancellableTask: CancellationToken -> Task<'T>)
                : CancellationToken -> Awaiter<TaskAwaiter<'T>, 'T> =
                (fun ct -> Awaitable.GetTaskAwaiter(cancellableTask ct))

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline this.Source
                (asyncComputation: Async<'T>)
                : CancellationToken -> Awaiter<TaskAwaiter<'T>, 'T> =
                this.Source(Async.AsCancellableTask(asyncComputation))


            /// <summary>Creates A CancellableTask that runs computation. The action compensation is executed
            /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
            /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of try/finally in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="computation">The input computation.</param>
            /// <param name="compensation">The action to be run after computation completes or raises an
            /// exception.</param>
            ///
            /// <returns>A CancellableTask that executes computation and compensation afterwards or
            /// when an exception is raised.</returns>
            member inline internal x.TryFinallyAsync
                (
                    computation: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>,
                    [<InlineIfLambda>] compensation: unit -> 'Awaitable
                ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
                ResumableCode.TryFinallyAsync(
                    computation,
                    ResumableCode<_, _>(fun sm ->
                        x.BindNoCancellation((compensation ()), (x.Zero)).Invoke(&sm)
                    )
                )


            /// <summary>Creates A CancellableTask that runs binder(resource).
            /// The action resource.DisposeAsync() is executed as this computation yields its result
            /// or if the CancellableTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of use and use! in the
            /// cancellableTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>A CancellableTask that binds and eventually disposes resource.</returns>
            ///
            member inline this.Using
                (
                    resource: #IAsyncDisposableNull,
                    binder:
                        #IAsyncDisposableNull -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
                ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
                this.TryFinallyAsync(
                    (fun sm -> (binder resource).Invoke(&sm)),
                    (fun () ->
                        if not (isNull (box resource)) then
                            resource.DisposeAsync()
                            |> Awaitable.GetAwaiter
                        else
                            ValueTask()
                            |> Awaitable.GetAwaiter
                    )
                )


            member inline internal x.WhileAsync
                (
                    [<InlineIfLambda>] condition: unit -> 'Awaitable,
                    body: CancellableTaskBaseCode<_, unit, 'Builder>
                ) : CancellableTaskBaseCode<_, unit, 'Builder> =
                let mutable condition_res = true

                x.While(
                    (fun () -> condition_res),
                    ResumableCode<_, _>(fun sm ->
                        x
                            .Bind(
                                condition (),
                                (fun result ->
                                    ResumableCode<_, _>(fun sm ->
                                        condition_res <- result
                                        if condition_res then body.Invoke(&sm) else true
                                    )
                                )
                            )
                            .Invoke(&sm)
                    )
                )

            member inline this.For
                (
                    source: #IAsyncEnumerable<'T>,
                    body: 'T -> CancellableTaskBaseCode<_, unit, 'Builder>
                ) : CancellableTaskBaseCode<_, _, 'Builder> =
                CancellableTaskBaseCode<_, _, _>(fun sm ->
                    this
                        .Using(
                            source.GetAsyncEnumerator sm.Data.CancellationToken,
                            (fun (e: IAsyncEnumerator<'T>) ->
                                this.WhileAsync(
                                    (fun () ->
                                        __debugPoint "ForLoop.InOrToKeyword"
                                        Awaitable.GetAwaiter(e.MoveNextAsync())
                                    ),
                                    (fun sm -> (body e.Current).Invoke(&sm))
                                )
                            )
                        )
                        .Invoke(&sm)
                )
