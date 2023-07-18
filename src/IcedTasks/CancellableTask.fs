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


/// Contains methods to build CancellableTasks using the F# computation expression syntax
[<AutoOpen>]
module CancellableTasks =

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
    type CancellableTask<'T> = CancellationToken -> Task<'T>
    /// CancellationToken -> Task
    type CancellableTask = CancellationToken -> Task

    // type CancellableTaskDelegate<'T> = Func<CancellationToken, Task<'T>>
    // type CancellableTaskDelegate<'T> = FSharpFunc<CancellationToken, Task<'T>>
    // type CancellableTaskDelegate<'T> = delegate of CancellationToken -> Task<'T>

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableTaskStateMachineData<'T> =
        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncTaskMethodBuilder<'T>

        member inline this.ThrowIfCancellationRequested() =
            this.CancellationToken.ThrowIfCancellationRequested()

    /// This is used by the compiler as a template for creating state machine structs
    and CancellableTaskStateMachine<'TOverall> =
        ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a CancellableTask state machine created dynamically
    and CancellableTaskResumptionFunc<'TOverall> =
        ResumptionFunc<CancellableTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a CancellableTask state machine created dynamically
    and CancellableTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<CancellableTaskStateMachineData<'TOverall>>

    /// A special compiler-recognized delegate type for specifying blocks of CancellableTask code with access to the state machine
    and CancellableTaskCode<'TOverall, 'T> =
        ResumableCode<CancellableTaskStateMachineData<'TOverall>, 'T>

    /// Contains methods to build CancellableTasks using the F# computation expression syntax
    type CancellableTaskBuilderBase() =


        /// <summary>Creates a CancellableTask that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A cancellableTask that runs generator</returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> CancellableTaskCode<'TOverall, 'T>)
            : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.Delay(generator)


        /// <summary>Creates an CancellableTask that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// cancellableTask { ... } computation expression syntax.
        /// </remarks>
        /// <returns>An CancellableTask that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : CancellableTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An CancellableTask that returns value when executed.</returns>
        member inline _.Return(value: 'T) : CancellableTaskCode<'T, 'T> =
            CancellableTaskCode<'T, _>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an CancellableTask that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An CancellableTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: CancellableTaskCode<'TOverall, unit>,
                task2: CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates an CancellableTask that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>An CancellableTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                computation: CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates an CancellableTask that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>An CancellableTask that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: CancellableTaskCode<'TOverall, 'T>,
                catchHandler: exn -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates an CancellableTask that runs computation. The action compensation is executed
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
        /// exception (including cancellation).</param>
        ///
        /// <returns>An CancellableTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: CancellableTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(
                computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an CancellableTask that enumerates the sequence seq
        /// on demand and runs body for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of for in the
        /// cancellableTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an CancellableTask.  Can be seen as the body of the for expression.</param>
        ///
        /// <returns>An CancellableTask that will enumerate the sequence and run body
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

#if NETSTANDARD2_1
        /// <summary>Creates an CancellableTask that runs computation. The action compensation is executed
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
        /// <returns>An CancellableTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: CancellableTaskCode<'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinallyAsync(
                computation,
                ResumableCode<_, _>(fun sm ->

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
                            CancellableTaskResumptionFunc<'TOverall>(fun sm ->
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

        /// <summary>Creates an CancellableTask that runs binder(resource).
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
        /// <returns>An CancellableTask that binds and eventually disposes resource.</returns>
        ///
        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            this.TryFinallyAsync(
                (fun sm -> (binder resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                    else
                        ValueTask()
                )
            )
#endif
    /// Contains methods to build CancellableTasks using the F# computation expression syntax
    type CancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =

            let mutable sm = CancellableTaskStateMachine<'T>()

            let initialResumptionFunc =
                CancellableTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new CancellableTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
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
                    Task.FromCanceled<_>(ct)
                else
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo
                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task


        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskStateMachineData<'T>, CancellableTask<'T>>
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
                                Task.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                failwith "sorry lol"
    // CancellableTaskBuilder.RunDynamic(code)

    /// Contains methods to build CancellableTasks using the F# computation expression syntax
    type BackgroundCancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                CancellableTaskBuilder.RunDynamic(code)
            else
                fun (ct) ->
                    Task.Run<'T>((fun () -> CancellableTaskBuilder.RunDynamic (code) (ct)), ct)

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the ThreadPool using Task.Run
        /// </summary>
        member inline _.Run(code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskStateMachineData<'T>, CancellableTask<'T>>
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
                    (AfterCode<_, CancellableTask<'T>>(fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    sm.Data.CancellationToken <- ct
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy contents of state machine so we can capture it

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    Task.Run<'T>(
                                        (fun () ->
                                            let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                            sm.Data.CancellationToken <- ct

                                            sm.Data.MethodBuilder <-
                                                AsyncTaskMethodBuilder<'T>.Create()

                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task
                                        ),
                                        ct
                                    )
                    ))

            else
                BackgroundCancellableTaskBuilder.RunDynamic(code)

    /// Contains the cancellableTask computation expression builder.
    [<AutoOpen>]
    module CancellableTaskBuilder =

        /// <summary>
        /// Builds a cancellableTask using computation expression syntax.
        /// </summary>
        let cancellableTask = CancellableTaskBuilder()

        /// <summary>
        /// Builds a cancellableTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundCancellableTask = BackgroundCancellableTaskBuilder()

    /// <exclude />
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter sm.Data.CancellationToken

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall>(fun sm ->
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

            /// <summary>Creates an CancellableTask that runs computation, and when
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
            /// <returns>An CancellableTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _>(fun sm ->
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
                        CancellableTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall>(fun sm ->
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

            /// <summary>Creates an CancellableTask that runs computation, and when
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
            /// <returns>An CancellableTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.ThrowIfCancellationRequested()
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = getAwaiter

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
                        CancellableTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
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
            member inline this.ReturnFrom<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter)
                : CancellableTaskCode<_, _> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return v))


            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : CancellableTaskCode<_, _> =
                this.Bind(getAwaiter, (fun v -> this.Return v))


            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    [<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter,
                    mapper: 'TResult1 -> 'TResult2
                ) : CancellableTaskCode<'TResult2, 'TResult2> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return(mapper v)))


            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: 'Awaiter,
                    mapper: 'TResult1 -> 'TResult2
                ) : CancellableTaskCode<'TResult2, 'TResult2> =
                this.Bind(getAwaiter, (fun v -> this.Return(mapper v)))

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] getAwaiter: CancellationToken -> 'Awaiter)
                : CancellationToken -> 'Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : 'Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a 'Awaitable into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (task: 'Awaitable)
                : 'Awaiter =
                Awaitable.GetAwaiter task


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellationToken -> 'Awaitable into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] task: CancellationToken -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct -> Awaitable.GetAwaiter(task ct))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a unit -> 'Awaitable into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                ([<InlineIfLambda>] task: unit -> 'Awaitable)
                : CancellationToken -> 'Awaiter =
                (fun ct -> Awaitable.GetAwaiter(task ()))


            /// <summary>Creates an CancellableTask that runs binder(resource).
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
            /// <returns>An CancellableTask that binds and eventually disposes resource.</returns>
            ///
            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    binder: 'Resource -> CancellableTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(
                    resource,
                    fun resource ->
                        CancellableTaskCode<'TOverall, 'T>(fun sm ->
                            sm.Data.ThrowIfCancellationRequested()
                            (binder resource).Invoke(&sm)
                        )
                )

    /// <exclude />
    [<AutoOpen>]
    module HighPriority =

        type AsyncEx with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            ///
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask<'T>) = asyncEx {
                let! ct = Async.CancellationToken
                return! t ct
            }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            ///
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask) = asyncEx {
                let! ct = Async.CancellationToken
                return! t ct
            }

        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask<'T>) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitTask
            }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitTask
            }

            /// <summary>Runs an asynchronous computation, starting on the current operating system thread.</summary>
            static member inline AsCancellableTask(computation: Async<'T>) : CancellableTask<_> =
                fun ct -> Async.StartImmediateAsTask(computation, cancellationToken = ct)

        // High priority extensions
        type CancellableTaskBuilderBase with


            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source(task: Task<'T>) = task.GetAwaiter()

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a ColdTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source([<InlineIfLambda>] task: unit -> Task<'TResult1>) =
                (fun (ct: CancellationToken) -> (task ()).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellableTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source([<InlineIfLambda>] task: CancellableTask<'TResult1>) =
                (fun ct -> (task ct).GetAwaiter())


            // member inline _.Source([<InlineIfLambda>] task: CancellableTaskDelegate<'TResult1>) =
            //     (fun ct -> (task.Invoke ct).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(fun ct -> Async.AsCancellableTask (computation) ct)

            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This turns a CancellableTask&lt;'T&gt; into a CancellationToken -> 'Awaiter.</remarks>
            ///
            /// <returns>CancellationToken -> 'Awaiter</returns>
            member inline _.Source(awaiter: TaskAwaiter<'TResult1>) = awaiter

    /// <summary>
    /// A set of extension methods making it possible to bind against <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in async computations.
    /// </summary>
    [<AutoOpen>]
    module AsyncExtensions =

        type AsyncExBuilder with

            member inline this.Source([<InlineIfLambda>] t: CancellableTask<'T>) : Async<'T> =
                AsyncEx.AwaitCancellableTask t

            member inline this.Source([<InlineIfLambda>] t: CancellableTask) : Async<unit> =
                AsyncEx.AwaitCancellableTask t

        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableTask<'T>,
                    [<InlineIfLambda>] binder: ('T -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] t: CancellableTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableTask,
                    [<InlineIfLambda>] binder: (unit -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] t: CancellableTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

    // There is explicitly no Binds for `CancellableTasks` in `Microsoft.FSharp.Control.TaskBuilderBase`.
    // You need to explicitly pass in a `CancellationToken`to start it, you can use `CancellationToken.None`.
    // Reason is I don't want people to assume cancellation is happening without the caller being explicit about where the CancellationToken came from.
    // Similar reasoning for `IcedTasks.ColdTasks.ColdTaskBuilderBase`.

    // Contains a set of standard functional helper function

    [<RequireQualifiedAccess>]
    module CancellableTask =

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
        ///         cancellableTask {
        ///             let! cancellationToken = CancellableTask.getCancellationToken()
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
        let inline getCancellationToken () =
            fun (ct: CancellationToken) ->
#if NETSTANDARD2_1
                ValueTask<CancellationToken> ct
#else
                Task.FromResult ct
#endif

        /// <summary>Lifts an item to a CancellableTask.</summary>
        /// <param name="item">The item to be the result of the CancellableTask.</param>
        /// <returns>A CancellableTask with the item as the result.</returns>
        let inline singleton (item: 'item) : CancellableTask<'item> = fun _ -> Task.FromResult(item)


        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> CancellableTask<'output>)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a CancellableTasks.</returns>
        let inline map
            ([<InlineIfLambda>] mapper: 'input -> 'output)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="applicable">A function wrapped in a CancellableTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply
            ([<InlineIfLambda>] applicable: CancellableTask<'input -> 'output>)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two CancellableTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip
            ([<InlineIfLambda>] left: CancellableTask<'left>)
            ([<InlineIfLambda>] right: CancellableTask<'right>)
            =
            cancellableTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        /// <summary>Takes two CancellableTask, starts them concurrently, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in.</returns>
        let inline parallelZip
            ([<InlineIfLambda>] left: CancellableTask<'left>)
            ([<InlineIfLambda>] right: CancellableTask<'right>)
            =
            cancellableTask {
                let! ct = getCancellationToken ()
                let r1 = left ct
                let r2 = right ct
                let! r1 = r1
                let! r2 = r2
                return r1, r2
            }


        /// <summary>Creates a task that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in an enumerable collection have completed.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks" /> argument was <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="tasks" /> collection contained a <see langword="null" /> task.</exception>
        let inline whenAll (tasks: CancellableTask<_> seq) = cancellableTask {
            let! ct = getCancellationToken ()

            let! results =
                tasks
                |> Seq.map (fun t -> t ct)
                |> Task.WhenAll

            return results
        }

        /// <summary>Creates a task that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in an enumerable collection have completed.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of tasks to run concurrently.</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks" /> argument was <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="tasks" /> collection contained a <see langword="null" /> task.</exception>
        let inline whenAllThrottled (maxDegreeOfParallelism: int) (tasks: CancellableTask<_> seq) = cancellableTask {
            let! ct = getCancellationToken ()

            use semaphore =
                new SemaphoreSlim(
                    initialCount = maxDegreeOfParallelism,
                    maxCount = maxDegreeOfParallelism
                )

            let! results =
                tasks
                |> Seq.map (fun t -> task {
                    do! semaphore.WaitAsync ct

                    try
                        return! t ct
                    finally
                        semaphore.Release()
                        |> ignore

                })
                |> Task.WhenAll

            return results
        }

        /// <summary>Creates a <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/>s in an enumerable collection have completed sequentially.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        let inline sequential (tasks: CancellableTask<'a> seq) = cancellableTask {
            let mutable results = ArrayCollector<'a>()

            for t in tasks do
                let! result = t
                results.Add result

            return results.Close()
        }


        /// <summary>Coverts a CancellableTask to a CancellableTask\&lt;unit\&gt;.</summary>
        /// <param name="unitCancellableTask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitCancellableTask: CancellableTask) = cancellableTask {
            return! unitCancellableTask
        }

        /// <summary>Coverts a CancellableTask\&lt;_\&gt; to a CancellableTask.</summary>
        /// <param name="ctask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask.</returns>
        let inline toUnit ([<InlineIfLambda>] ctask: CancellableTask<_>) : CancellableTask =
            fun ct -> ctask ct

        let inline internal getAwaiter ([<InlineIfLambda>] ctask: CancellableTask<_>) =
            fun ct -> (ctask ct).GetAwaiter()

        // let inline startAsTask ct (ctask: CancellableTaskDelegate<_>) = ctask.Invoke ct

        let inline startAsTask (ct: byref<CancellationToken>) (ctask: CancellableTask<_>) = ctask ct

    /// <exclude />
    [<AutoOpen>]
    module MergeSourcesExtensions =

        type CancellableTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    [<InlineIfLambda>] left: CancellationToken -> 'Awaiter1,
                    [<InlineIfLambda>] right: CancellationToken -> 'Awaiter2
                ) : CancellationToken -> TaskAwaiter<'TResult1 * 'TResult2> =

                cancellableTask {
                    let! ct = CancellableTask.getCancellationToken ()
                    let leftStarted = left ct
                    let rightStarted = right ct
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> CancellableTask.getAwaiter


            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    left: 'Awaiter1,
                    right: 'Awaiter2
                ) : CancellationToken -> TaskAwaiter<'TResult1 * 'TResult2> =

                cancellableTask {
                    let! leftResult = left
                    let! rightResult = right
                    return leftResult, rightResult
                }
                |> CancellableTask.getAwaiter

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    left: CancellationToken -> 'Awaiter1,
                    right: 'Awaiter2
                ) : CancellationToken -> TaskAwaiter<'TResult1 * 'TResult2> =

                cancellableTask {
                    let leftStarted = fun ct -> left ct
                    let rightStarted = right
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> CancellableTask.getAwaiter


            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    left: 'Awaiter1,
                    right: CancellationToken -> 'Awaiter2
                ) : CancellationToken -> TaskAwaiter<'TResult1 * 'TResult2> =

                cancellableTask {
                    let leftStarted = left
                    let rightStarted = fun ct -> right ct
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> CancellableTask.getAwaiter
