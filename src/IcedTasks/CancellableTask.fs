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

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableTaskStateMachineData<'T> =
        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncTaskMethodBuilder<'T>

        with
            member inline this.ThrowIfCancellationRequested() =
                this.CancellationToken.ThrowIfCancellationRequested()

    and CancellableTaskStateMachine<'TOverall> =
        ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>

    and CancellableTaskResumptionFunc<'TOverall> =
        ResumptionFunc<CancellableTaskStateMachineData<'TOverall>>

    and CancellableTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<CancellableTaskStateMachineData<'TOverall>>

    and CancellableTaskCode<'TOverall, 'T> =
        ResumableCode<CancellableTaskStateMachineData<'TOverall>, 'T>

    type CancellableTaskBuilderBase() =


        /// <summary>Creates a CancellableTask that runs <c>generator</c></summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A cancellableTask that runs <c>generator</c></returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> CancellableTaskCode<'TOverall, 'T>)
            : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.Delay(fun () ->
                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    (generator ()).Invoke(&sm)
                )
            )


        /// <summary>Creates an CancellableTask that just returns <c>()</c>.</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty <c>else</c> branches in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.
        /// </remarks>
        /// <returns>An CancellableTask that returns <c>()</c>.</returns>
        [<DefaultValue>]
        member inline _.Zero() : CancellableTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result <c>v</c>.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of <c>return</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An CancellableTask that returns <c>value</c> when executed.</returns>
        member inline _.Return(value: 'T) : CancellableTaskCode<'T, 'T> =
            CancellableTaskCode<'T, _>(fun sm ->
                sm.Data.ThrowIfCancellationRequested()
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an CancellableTask that first runs <c>task1</c>
        /// and then runs <c>computation2</c>, returning the result of <c>computation2</c>.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
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
            ResumableCode.Combine(
                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task1.Invoke(&sm)
                ),

                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task2.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableTask that runs <c>computation</c> repeatedly
        /// until <c>guard()</c> becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>while</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing <c>computation</c>.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a <c>while</c> expression.</param>
        ///
        /// <returns>An CancellableTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.While(
                guard,
                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableTask that runs <c>computation</c> and returns its result.
        /// If an exception happens then <c>catchHandler(exn)</c> is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/with</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when <c>computation</c> throws an exception.</param>
        ///
        /// <returns>An CancellableTask that executes <c>computation</c> and calls <c>catchHandler</c> if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: CancellableTaskCode<'TOverall, 'T>,
                catchHandler: exn -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(
                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                catchHandler
            )

        /// <summary>Creates an CancellableTask that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An CancellableTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: CancellableTaskCode<'TOverall, 'T>,
                compensation: unit -> unit
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(

                CancellableTaskCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an CancellableTask that enumerates the sequence <c>seq</c>
        /// on demand and runs <c>body</c> for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of <c>for</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an CancellableTask.  Can be seen as the body of the <c>for</c> expression.</param>
        ///
        /// <returns>An CancellableTask that will enumerate the sequence and run <c>body</c>
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.For(
                sequence,
                fun item ->
                    CancellableTaskCode(fun sm ->
                        sm.Data.ThrowIfCancellationRequested()
                        (body item).Invoke(&sm)
                    )
            )

#if NETSTANDARD2_1
        /// <summary>Creates an CancellableTask that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
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
                            CancellableTaskResumptionFunc<'TOverall>(fun sm ->
                                awaiter.GetResult()
                                |> ignore

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

        /// <summary>Creates an CancellableTask that runs <c>binder(resource)</c>.
        /// The action <c>resource.DisposeAsync()</c> is executed as this computation yields its result
        /// or if the CancellableTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
        /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An CancellableTask that binds and eventually disposes <c>resource</c>.</returns>
        ///
        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
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
                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
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
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                CancellableTaskBuilder.RunDynamic(code)

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
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
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
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
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
                                                AsyncTaskMethodBuilder<'T>.Create ()

                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task
                                        ),
                                        ct
                                    )
                    ))

            else
                BackgroundCancellableTaskBuilder.RunDynamic(code)


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

    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    getAwaiter: CancellationToken -> ^Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter sm.Data.CancellationToken

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall>(fun sm ->
                        let result = (^Awaiter: (member GetResult: unit -> 'TResult1) (awaiter))
                        (continuation result).Invoke(&sm)
                    ))

                // shortcut to continue immediately
                if (^Awaiter: (member get_IsCompleted: unit -> bool) (awaiter)) then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <-
                        (awaiter :> ICriticalNotifyCompletion)

                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false

            /// <summary>Creates an CancellableTask that runs <c>computation</c>, and when
            /// <c>computation</c> generates a result <c>T</c>, runs <c>binder res</c>.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of <c>let!</c> in the
            /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of <c>computation</c>.</param>
            ///
            /// <returns>An CancellableTask that performs a monadic bind on the result
            /// of <c>computation</c>.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    getAwaiter: CancellationToken -> ^Awaiter,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.ThrowIfCancellationRequested()
                        // Get an awaiter from the awaitable
                        let mutable awaiter = getAwaiter sm.Data.CancellationToken

                        let mutable __stack_fin = true

                        if not (^Awaiter: (member get_IsCompleted: unit -> bool) (awaiter)) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = (^Awaiter: (member GetResult: unit -> 'TResult1) (awaiter))
                            (continuation result).Invoke(&sm)
                        else
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    else
                        CancellableTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of <c>return!</c> in the
            /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: CancellationToken -> ^Awaiter)
                : CancellableTaskCode<_, _> =
                this.Bind((fun ct -> getAwaiter ct), (fun v -> this.Return v))


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: CancellationToken -> ^Awaiter)
                : CancellationToken -> ^Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (task: ^TaskLike)
                : CancellationToken -> ^Awaiter =
                (fun (ct: CancellationToken) ->
                    (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))
                )


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellationToken -> ^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambda>] task: CancellationToken -> ^TaskLike)
                : CancellationToken -> ^Awaiter =
                (fun ct -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task ct)))


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>unit -> ^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambda>] task: unit -> ^TaskLike)
                : CancellationToken -> ^Awaiter =
                (fun ct -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task ())))


            /// <summary>Creates an CancellableTask that runs <c>binder(resource)</c>.
            /// The action <c>resource.Dispose()</c> is executed as this computation yields its result
            /// or if the CancellableTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
            /// <c>cancellableTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An CancellableTask that binds and eventually disposes <c>resource</c>.</returns>
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

    [<AutoOpen>]
    module HighPriority =
        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask(t: CancellableTask<'T>) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitTask
            }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableTask(t: CancellableTask) = async {
                let! ct = Async.CancellationToken

                return!
                    t ct
                    |> Async.AwaitTask
            }

            /// <summary>Executes a computation in the thread pool.</summary>
            static member inline AsCancellableTask(computation: Async<'T>) : CancellableTask<_> =
                fun ct -> Async.StartAsTask(computation, cancellationToken = ct)

        // High priority extensions
        type CancellableTaskBuilderBase with


            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns><c>IEnumerable</c></returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>Task&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline _.Source(task: Task<'T>) =
                (fun (ct: CancellationToken) -> task.GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline _.Source([<InlineIfLambda>] task: ColdTask<'TResult1>) =
                (fun (ct: CancellationToken) -> (task ()).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellableTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline _.Source([<InlineIfLambda>] task: CancellableTask<'TResult1>) =
                (fun ct -> (task ct).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>Async&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(Async.AsCancellableTask(computation))


    [<AutoOpen>]
    module AsyncExtenions =
        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind(t: CancellableTask<'T>, binder: ('T -> Async<'U>)) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom(t: CancellableTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

            member inline this.Bind(t: CancellableTask, binder: (unit -> Async<'U>)) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom(t: CancellableTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

    // There is explicitly no Binds for `CancellableTasks` in `Microsoft.FSharp.Control.TaskBuilderBase`.
    // You need to explicitly pass in a `CancellationToken`to start it, you can use `CancellationToken.None`.
    // Reason is I don't want people to assume cancellation is happening without the caller being explicit about where the CancellationToken came from.
    // Similar reasoning for `IcedTasks.ColdTasks.ColdTaskBuilderBase`.

    [<RequireQualifiedAccess>]
    module CancellableTask =

        /// <summary>Gets the default cancellation token for executing computations.</summary>
        ///
        /// <returns>The default CancellationToken.</returns>
        ///
        /// <category index="3">Cancellation and Exceptions</category>
        ///
        /// <example id="default-cancellation-token-1">
        /// <code lang="fsharp">
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
        let getCancellationToken () =
            CancellableTaskBuilder.cancellableTask.Run(
                CancellableTaskCode<_, _>(fun sm ->
                    sm.Data.Result <- sm.Data.CancellationToken
                    true
                )
            )

        /// <summary>Lifts an item to a CancellableTask.</summary>
        /// <param name="item">The item to be the result of the CancellableTask.</param>
        /// <returns>A CancellableTask with the item as the result.</returns>
        let inline singleton (item: 'item) : CancellableTask<'item> = cancellableTask {
            return item
        }


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


        /// <summary>Coverts a CancellableTask to a CancellableTask\&lt;unit\&gt;.</summary>
        /// <param name="unitCancellabletTask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitCancellabletTask: CancellableTask) = cancellableTask {
            return! unitCancellabletTask
        }

        /// <summary>Coverts a CancellableTask\&lt;_\&gt; to a CancellableTask.</summary>
        /// <param name="cancellabletTask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask.</returns>
        let inline toUnit
            ([<InlineIfLambda>] cancellabletTask: CancellableTask<_>)
            : CancellableTask =
            fun ct -> cancellabletTask ct :> Task
