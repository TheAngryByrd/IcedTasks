// Task builder for F# that compiles to allocation-free paths for synchronous code.
//
// Originally written in 2016 by Robert Peele (humbobst@gmail.com)
// New operator-based overload resolution for F# 4.0 compatibility by Gustavo Leon in 2018.
// Revised for insertion into FSharp.Core by Microsoft, 2019.
// Revised to implement CancellationToken semantics
//
// Original notice:
// To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights
// to this software to the public domain worldwide. This software is distributed without any warranty.

namespace IcedTasks

#if NETSTANDARD2_1

/// Contains methods to build CancellableTasks using the F# computation expression syntax
[<AutoOpen>]
module CancellableValueTasks =

    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// CancellationToken -> Task<'T>
    type CancellableValueTask<'T> = CancellationToken -> ValueTask<'T>
    /// CancellationToken -> Task
    type CancellableValueTask = CancellationToken -> ValueTask

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableValueTaskStateMachineData<'T> =
        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncValueTaskMethodBuilder<'T>

        member inline this.ThrowIfCancellationRequested() =
            this.CancellationToken.ThrowIfCancellationRequested()

    /// The extra data stored in ResumableStateMachine for tasks
    and CancellableValueTaskStateMachine<'TOverall> =
        ResumableStateMachine<CancellableValueTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a CancellableValueTask state machine created dynamically
    and CancellableValueTaskResumptionFunc<'TOverall> =
        ResumptionFunc<CancellableValueTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a CancellableValueTask state machine created dynamically
    and CancellableValueTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<CancellableValueTaskStateMachineData<'TOverall>>

    /// A special compiler-recognised delegate type for specifying blocks of CancellableValueTask code with access to the state machine
    and CancellableValueTaskCode<'TOverall, 'T> =
        ResumableCode<CancellableValueTaskStateMachineData<'TOverall>, 'T>

    /// Contains methods to build CancellableValueTasks using the F# computation expression syntax
    type CancellableValueTaskBuilderBase() =


        /// <summary>Creates a CancellableValueTask that runs erator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A cancellableValueTask that runs erator</returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> CancellableValueTaskCode<'TOverall, 'T>)
            : CancellableValueTaskCode<'TOverall, 'T> =
            ResumableCode.Delay(fun () ->
                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    (generator ()).Invoke(&sm)
                )
            )


        /// <summary>Creates an CancellableValueTask that just returns </summary>
        /// <remarks>
        /// The existence of this method permits the use of empty e branches in the
        /// cellableValueTask { ... } computation expression syntax.
        /// </remarks>
        /// <returns>An CancellableValueTask that returns </returns>
        [<DefaultValue>]
        member inline _.Zero() : CancellableValueTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result </summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of urn in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An CancellableValueTask that returns ue when executed.</returns>
        member inline _.Return(value: 'T) : CancellableValueTaskCode<'T, 'T> =
            CancellableValueTaskCode<'T, _>(fun sm ->
                sm.Data.ThrowIfCancellationRequested()
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an CancellableValueTask that first runs k1
        /// and then runs putation2, returning the result of computan2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An CancellableValueTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: CancellableValueTaskCode<'TOverall, unit>,
                task2: CancellableValueTaskCode<'TOverall, 'T>
            ) : CancellableValueTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(
                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task1.Invoke(&sm)
                ),

                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task2.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableValueTask that runs putation repeatedly
        /// until rd() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of le in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing putation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a le expression.</param>
        ///
        /// <returns>An CancellableValueTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: CancellableValueTaskCode<'TOverall, unit>
            ) : CancellableValueTaskCode<'TOverall, unit> =
            ResumableCode.While(
                guard,
                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableValueTask that runs putation and returns its result.
        /// If an exception happens then chHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of /with in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when putation throws an exception.</param>
        ///
        /// <returns>An CancellableValueTask that executes putation and calls catchHaer if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: CancellableValueTaskCode<'TOverall, 'T>,
                catchHandler: exn -> CancellableValueTaskCode<'TOverall, 'T>
            ) : CancellableValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(
                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                catchHandler
            )

        /// <summary>Creates an CancellableValueTask that runs putation. The action compenson is executed
        /// after putation completes, whether computan exits normally or by an exception. If compensation res an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of /finally in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after putation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An CancellableValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: CancellableValueTaskCode<'TOverall, 'T>,
                compensation: unit -> unit
            ) : CancellableValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(

                CancellableValueTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an CancellableValueTask that enumerates the sequence
        /// on demand and runs y for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of  in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an CancellableValueTask.  Can be seen as the body of the  expression.</param>
        ///
        /// <returns>An CancellableValueTask that will enumerate the sequence and run y
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> CancellableValueTaskCode<'TOverall, unit>
            ) : CancellableValueTaskCode<'TOverall, unit> =
            ResumableCode.For(
                sequence,
                fun item ->
                    CancellableValueTaskCode(fun sm ->
                        sm.Data.ThrowIfCancellationRequested()
                        (body item).Invoke(&sm)
                    )
            )

#if NETSTANDARD2_1
        /// <summary>Creates an CancellableValueTask that runs putation. The action compenson is executed
        /// after putation completes, whether computan exits normally or by an exception. If compensation res an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of /finally in the
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after putation completes or raises an
        /// exception.</param>
        ///
        /// <returns>An CancellableValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: CancellableValueTaskCode<'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : CancellableValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinallyAsync(
                computation,
                ResumableCode<_, _>(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()

                    if __useResumableCode then
                        let mutable __stack_condition_fin = true
                        let __stack_vtask = compensation ()

                        if not __stack_vtask.IsCompleted then
                            let mutable awaiter = __stack_vtask.GetAwaiter()
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_condition_fin <- __stack_yield_fin

                            if not __stack_condition_fin then
                                sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)

                        __stack_condition_fin
                    else
                        let vtask = compensation ()
                        let mutable awaiter = vtask.GetAwaiter()

                        let cont =
                            CancellableValueTaskResumptionFunc<'TOverall>(fun sm ->
                                awaiter
                                |> Awaiter.GetResult

                                true
                            )

                        // shortcut to continue immediately
                        if awaiter.IsCompleted then
                            true
                        else
                            sm.ResumptionDynamicInfo.ResumptionData <-
                                (awaiter :> ICriticalNotifyCompletion)

                            sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                            false
                )
            )

        /// <summary>Creates an CancellableValueTask that runs der(resource).
        /// The action ource.DisposeAsync() is executed as this computation yields its result
        /// or if the CancellableValueTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of  and use! ine
        /// cellableValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An CancellableValueTask that binds and eventually disposes ource.</returns>
        ///
        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> CancellableValueTaskCode<'TOverall, 'T>
            ) : CancellableValueTaskCode<'TOverall, 'T> =
            this.TryFinallyAsync(
                (fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    (binder resource).Invoke(&sm)
                ),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                    else
                        ValueTask()
                )
            )
#endif

    /// Contains methods to build CancellableValueTasks using the F# computation expression syntax
    type CancellableValueTaskBuilder() =

        inherit CancellableValueTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic
            (code: CancellableValueTaskCode<'T, 'T>)
            : CancellableValueTask<'T> =

            let mutable sm = CancellableValueTaskStateMachine<'T>()

            let initialResumptionFunc =
                CancellableValueTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new CancellableValueTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
                    member info.MoveNext(sm) =
                        let mutable savedExn = null

                        try
                            sm.ResumptionDynamicInfo.ResumptionData <- null
                            let step = info.ResumptionFunc.Invoke(&sm)

                            if step then
                                sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                            else
                                let mutable awaiter =
                                    sm.ResumptionDynamicInfo.ResumptionData
                                    :?> ICriticalNotifyCompletion

                                assert not (isNull awaiter)
                                sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)

                        with exn ->
                            savedExn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match savedExn with
                        | null -> ()
                        | exn -> sm.Data.MethodBuilder.SetException exn

                    member _.SetStateMachine(sm, state) =
                        sm.Data.MethodBuilder.SetStateMachine(state)
                }

            fun (ct) ->
                if ct.IsCancellationRequested then
                    ValueTask.FromCanceled<_>(ct)
                else
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo
                    sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: CancellableValueTaskCode<'T, 'T>) : CancellableValueTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableValueTaskStateMachineData<'T>, CancellableValueTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint
                        let mutable __stack_exn: Exception = null

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
                                sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                        with exn ->
                            __stack_exn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match __stack_exn with
                        | null -> ()
                        | exn -> sm.Data.MethodBuilder.SetException exn
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        sm.Data.MethodBuilder.SetStateMachine(state)
                    ))
                    (AfterCode<_, _>(fun sm ->
                        let sm = sm

                        fun (ct) ->
                            if ct.IsCancellationRequested then
                                ValueTask.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct
                                sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                CancellableValueTaskBuilder.RunDynamic(code)

    /// Contains methods to build CancellableValueTasks using the F# computation expression syntax
    type BackgroundCancellableValueTaskBuilder() =

        inherit CancellableValueTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic
            (code: CancellableValueTaskCode<'T, 'T>)
            : CancellableValueTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                CancellableValueTaskBuilder.RunDynamic(code)
            else
                fun (ct) ->
                    Task.Run<'T>(
                        (fun () -> (CancellableValueTaskBuilder.RunDynamic code ct).AsTask()),
                        ct
                    )
                    |> ValueTask<'T>

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run(code: CancellableValueTaskCode<'T, 'T>) : CancellableValueTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableValueTaskStateMachineData<'T>, CancellableValueTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
                                sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                        with exn ->
                            sm.Data.MethodBuilder.SetException exn
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        sm.Data.MethodBuilder.SetStateMachine(state)
                    ))
                    (AfterCode<_, CancellableValueTask<'T>>(fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    ValueTask.FromCanceled<_>(ct)
                                else
                                    sm.Data.CancellationToken <- ct

                                    sm.Data.MethodBuilder <-
                                        AsyncValueTaskMethodBuilder<'T>.Create()

                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy contents of state machine so we can capture it

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    ValueTask.FromCanceled<_>(ct)
                                else
                                    Task.Run<'T>(
                                        (fun () ->
                                            let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                            sm.Data.CancellationToken <- ct

                                            sm.Data.MethodBuilder <-
                                                AsyncValueTaskMethodBuilder<'T>.Create()

                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task.AsTask()
                                        ),
                                        ct
                                    )
                                    |> ValueTask<'T>
                    ))

            else
                BackgroundCancellableValueTaskBuilder.RunDynamic(code)

    /// Contains the cancellableTask computation expression builder.
    [<AutoOpen>]
    module CancellableValueTaskBuilder =

        /// <summary>
        /// Builds a cancellableValueTask using computation expression syntax.
        /// </summary>
        let cancellableValueTask = CancellableValueTaskBuilder()

        /// <summary>
        /// Builds a cancellableValueTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundCancellableValueTask = BackgroundCancellableValueTaskBuilder()

    /// <exclude />
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableValueTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    sm:
                        byref<ResumableStateMachine<CancellableValueTaskStateMachineData<'TOverall>>>,
                    getAwaiter: CancellationToken -> 'Awaiter,
                    continuation: ('TResult1 -> CancellableValueTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter sm.Data.CancellationToken

                let cont =
                    (CancellableValueTaskResumptionFunc<'TOverall>(fun sm ->
                        let result =
                            awaiter
                            |> Awaiter.GetResult

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

            /// <summary>Creates an CancellableValueTask that runs putation, and when
            /// putation generates a result T, runsnder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of ! in the
            /// cellableValueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of putation.</param>
            ///
            /// <returns>An CancellableValueTask that performs a monadic bind on the result
            /// of putation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: CancellationToken -> 'Awaiter,
                    continuation: ('TResult1 -> CancellableValueTaskCode<'TOverall, 'TResult2>)
                ) : CancellableValueTaskCode<'TOverall, 'TResult2> =

                CancellableValueTaskCode<'TOverall, _>(fun sm ->
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
                            let result =
                                awaiter
                                |> Awaiter.GetResult

                            (continuation result).Invoke(&sm)
                        else
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    else
                        CancellableValueTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of urn! in the
            /// cellableValueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: CancellationToken -> 'Awaiter)
                : CancellableValueTaskCode<_, _> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return v))


            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: CancellationToken -> 'Awaiter,
                    f
                ) : CancellableValueTaskCode<'TResult2, 'TResult2> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return(f v)))


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : CancellationToken -> 'Awaiter =
                (fun ct -> getAwaiter)


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: CancellationToken -> 'Awaiter)
                : CancellationToken -> 'Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a aitable into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (task: 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun (ct: CancellationToken) ->
                    task
                    |> Awaitable.GetAwaiter
                )


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a cellationToken -> 'Awaitable into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] task: CancellationToken -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct ->
                    task ct
                    |> Awaitable.GetAwaiter
                )


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a t -> 'Awaitable into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] task: unit -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct ->
                    task ()
                    |> Awaitable.GetAwaiter
                )


            /// <summary>Creates an CancellableValueTask that runs der(resource).
            /// The action ource.Dispose() is executed as this computation yields its result
            /// or if the CancellableValueTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of  and use! ine
            /// cellableValueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An CancellableValueTask that binds and eventually disposes ource.</returns>
            ///
            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    binder: 'Resource -> CancellableValueTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(
                    resource,
                    fun resource ->
                        CancellableValueTaskCode<'TOverall, 'T>(fun sm ->
                            sm.Data.ThrowIfCancellationRequested()
                            (binder resource).Invoke(&sm)
                        )
                )

    /// <exclude />
    [<AutoOpen>]
    module HighPriority =
        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableValueTask(t: CancellableValueTask<'T>) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitValueTask
            }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableValueTask(t: CancellableValueTask) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitValueTask
            }

            /// <summary>Executes a computation in the thread pool.</summary>
            static member inline AsCancellableValueTask
                (computation: Async<'T>)
                : CancellableValueTask<_> =
                fun ct ->
                    Async.StartAsTask(computation, cancellationToken = ct)
                    |> ValueTask<'T>

        // High priority extensions
        type CancellableValueTaskBuilderBase with


            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>umerable</returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s

            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a k&lt;'T&gt; into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            member inline _.Source(task: Task<'T>) =
                (fun (ct: CancellationToken) -> task.GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a dTask&lt;'T&gt; into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            member inline _.Source([<InlineIfLambda>] task: ColdTask<'TResult1>) =
                (fun (ct: CancellationToken) -> (task ()).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a cellableValueTask&lt;'T&gt; into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            member inline _.Source([<InlineIfLambda>] task: CancellationToken -> Task<'TResult1>) =
                (fun ct -> (task ct).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a nc&lt;'T&gt; into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(Async.AsCancellableValueTask(computation))


            /// <summary>Allows the computation expression to turn other types into cellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a cellableTask&lt;'T&gt; into a CancellonToken -> 'Awaiter.</remarks>
            ///
            /// <returns>cellationToken -> 'Awaiter</returns>
            member inline _.Source(awaiter: TaskAwaiter<'TResult1>) = (fun ct -> awaiter)

    /// <summary>
    /// A set of extension methods making it possible to bind against <see cref='T:IcedTasks.CancellableValueTasks.CancellableValueTask`1'/> in async computations.
    /// </summary>
    [<AutoOpen>]
    module AsyncExtenions =
        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind
                (
                    t: CancellableValueTask<'T>,
                    binder: ('T -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableValueTask t, binder)

            member inline this.ReturnFrom(t: CancellableValueTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitCancellableValueTask t)

            member inline this.Bind
                (
                    t: CancellableValueTask,
                    binder: (unit -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableValueTask t, binder)

            member inline this.ReturnFrom(t: CancellableValueTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitCancellableValueTask t)

    // There is explicitly no Binds for `CancellableValueTasks` in `Microsoft.FSharp.Control.TaskBuilderBase`.
    // You need to explicitly pass in a `CancellationToken`to start it, you can use `CancellationToken.None`.
    // Reason is I don't want people to assume cancellation is happening without the caller being explicit about where the CancellationToken came from.
    // Similar reasoning for `IcedTasks.ColdTasks.ColdTaskBuilderBase`.

    /// Contains a set of standard functional helper function
    [<RequireQualifiedAccess>]
    module CancellableValueTask =

        /// <summary>Gets the default cancellation token for executing computations.</summary>
        ///
        /// <returns>The default CancellationToken.</returns>
        ///
        /// <category index="3">Cancellation and Exceptions</category>
        ///
        /// <example id="default-cancellation-token-1">
        /// <code lang="F#">
        /// use tokenSource = new CancellationTokenSource()
        /// let primes = [ 2; 3; 5; 7; 11 ]
        /// for i in primes do
        ///     let computation =
        ///         cancellableValueTask {
        ///             let! cancellationToken = CancellableValueTask.getCancellationToken()
        ///             do! Task.Delay(i * 1000, cancellationToken)
        ///             printfn $"{i}"
        ///         }
        ///     computation tokenSource.Token |> ignore
        /// Thread.Sleep(6000)
        /// tokenSource.Cancel()
        /// printfn "Tasks Finished"
        /// </code>
        /// This will print "2" 2 seconds from start, "3" 3 seconds from start, "5" 5 seconds from start, cease computation and then
        /// followed by "Tasks Finished".
        /// </example>
        let getCancellationToken () =
            CancellableValueTaskBuilder.cancellableValueTask.Run(
                CancellableValueTaskCode<_, _>(fun sm ->
                    sm.Data.Result <- sm.Data.CancellationToken
                    true
                )
            )

        /// <summary>Lifts an item to a CancellableValueTask.</summary>
        /// <param name="item">The item to be the result of the CancellableValueTask.</param>
        /// <returns>A CancellableValueTask with the item as the result.</returns>
        let inline singleton (item: 'item) : CancellableValueTask<'item> =
            fun (ct: CancellationToken) -> ValueTask<'item> item


        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> CancellableValueTask<'output>)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellableValueTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a CancellableValueTasks.</returns>
        let inline map
            ([<InlineIfLambda>] mapper: 'input -> 'output)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellableValueTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="applicable">A function wrapped in a CancellableValueTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply
            ([<InlineIfLambda>] applicable: CancellableValueTask<'input -> 'output>)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellableValueTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two CancellableValueTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip
            ([<InlineIfLambda>] left: CancellableValueTask<'left>)
            ([<InlineIfLambda>] right: CancellableValueTask<'right>)
            =
            cancellableValueTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        /// <summary>Takes two CancellableValueTask, starts them concurrently, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in.</returns>
        let inline parallelZip
            ([<InlineIfLambda>] left: CancellableValueTask<'left>)
            ([<InlineIfLambda>] right: CancellableValueTask<'right>)
            =
            cancellableValueTask {
                let! ct = getCancellationToken ()
                let r1 = left ct
                let r2 = right ct
                let! r1 = r1
                let! r2 = r2
                return r1, r2
            }


        /// <summary>Coverts a CancellableValueTask to a CancellableValueTask\&lt;unit\&gt;.</summary>
        /// <param name="unitCancellabletTask">The CancellableValueTask to convert.</param>
        /// <returns>a CancellableValueTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitCancellabletTask: CancellableValueTask) = cancellableValueTask {
            return! unitCancellabletTask
        }

        /// <summary>Coverts a CancellableValueTask\&lt;_\&gt; to a CancellableValueTask.</summary>
        /// <param name="cancellabletTask">The CancellableValueTask to convert.</param>
        /// <returns>a CancellableValueTask.</returns>
        let inline toUnit
            ([<InlineIfLambda>] cancellabletTask: CancellableValueTask<_>)
            : CancellableValueTask =
            fun ct ->
                cancellabletTask ct
                |> ValueTask.toUnit

        let inline internal getAwaiter ([<InlineIfLambda>] ctask: CancellableValueTask<_>) =
            fun ct -> (ctask ct).GetAwaiter()


    /// <exclude />
    [<AutoOpen>]
    module MergeSourcesExtensions =

        type CancellableValueTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    [<InlineIfLambda>] left: CancellationToken -> 'Awaiter1,
                    [<InlineIfLambda>] right: CancellationToken -> 'Awaiter2
                ) : CancellationToken -> ValueTaskAwaiter<'TResult1 * 'TResult2> =

                cancellableValueTask {
                    let! ct = CancellableValueTask.getCancellationToken ()
                    let leftStarted = left ct
                    let rightStarted = right ct
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> CancellableValueTask.getAwaiter


#endif
