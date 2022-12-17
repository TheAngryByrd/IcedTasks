// Task builder for F# that compiles to allocation-free paths for synchronous code.
//
// Originally written in 2016 by Robert Peele (humbobst@gmail.com)
// New operator-based overload resolution for F# 4.0 compatibility by Gustavo Leon in 2018.
// Revised for insertion into FSharp.Core by Microsoft, 2019.
// Revised to implement Lazy/ColdTask semantics
//
// Original notice:
// To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights
// to this software to the public domain worldwide. This software is distributed without any warranty.

namespace IcedTasks

[<AutoOpen>]
module ColdTasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// unit -> Task<'T>
    type ColdTask<'T> = unit -> Task<'T>
    /// unit -> Task
    type ColdTask = unit -> Task

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type ColdTaskStateMachineData<'T> =
        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncTaskMethodBuilder<'T>

    and ColdTaskStateMachine<'TOverall> = ResumableStateMachine<ColdTaskStateMachineData<'TOverall>>
    and ColdTaskResumptionFunc<'TOverall> = ResumptionFunc<ColdTaskStateMachineData<'TOverall>>

    and ColdTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<ColdTaskStateMachineData<'TOverall>>

    and ColdTaskCode<'TOverall, 'T> = ResumableCode<ColdTaskStateMachineData<'TOverall>, 'T>

    type ColdTaskBuilderBase() =

        /// <summary>Creates a ColdTask that runs <c>generator</c></summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A coldTask that runs <c>generator</c></returns>
        member inline _.Delay
            (generator: unit -> ColdTaskCode<'TOverall, 'T>)
            : ColdTaskCode<'TOverall, 'T> =
            ColdTaskCode<'TOverall, 'T>(fun sm -> (generator ()).Invoke(&sm))

        /// <summary>Creates an ColdTask that just returns <c>()</c>.</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty <c>else</c> branches in the
        /// <c>coldTask { ... }</c> computation expression syntax.
        /// </remarks>
        /// <returns>An ColdTask that returns <c>()</c>.</returns>
        [<DefaultValue>]
        member inline _.Zero() : ColdTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result <c>v</c>.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>return</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An ColdTask that returns <c>value</c> when executed.</returns>
        member inline _.Return(value: 'T) : ColdTaskCode<'T, 'T> =
            ColdTaskCode<'T, _>(fun sm ->
                sm.Data.Result <- value
                true
            )


        /// <summary>Creates an ColdTask that first runs <c>task1</c>
        /// and then runs <c>computation2</c>, returning the result of <c>computation2</c>.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An ColdTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: ColdTaskCode<'TOverall, unit>,
                task2: ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates an ColdTask that runs <c>computation</c> repeatedly
        /// until <c>guard()</c> becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>while</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing <c>computation</c>.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a <c>while</c> expression.</param>
        ///
        /// <returns>An ColdTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                body: ColdTaskCode<'TOverall, unit>
            ) : ColdTaskCode<'TOverall, unit> =
            ResumableCode.While(guard, body)


        /// <summary>Creates an ColdTask that runs <c>computation</c> and returns its result.
        /// If an exception happens then <c>catchHandler(exn)</c> is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/with</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when <c>computation</c> throws an exception.</param>
        ///
        /// <returns>An ColdTask that executes <c>computation</c> and calls <c>catchHandler</c> if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                body: ColdTaskCode<'TOverall, 'T>,
                catch: exn -> ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(body, catch)

        /// <summary>Creates an ColdTask that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An ColdTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                body: ColdTaskCode<'TOverall, 'T>,
                compensation: unit -> unit
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(
                body,
                ResumableCode<_, _>(fun _sm ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an ColdTask that enumerates the sequence <c>seq</c>
        /// on demand and runs <c>body</c> for each element.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>for</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an ColdTask.  Can be seen as the body of the <c>for</c> expression.</param>
        ///
        /// <returns>An ColdTask that will enumerate the sequence and run <c>body</c>
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> ColdTaskCode<'TOverall, unit>
            ) : ColdTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

#if NETSTANDARD2_1
        /// <summary>Creates an ColdTask that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
        /// exception.</param>
        ///
        /// <returns>An ColdTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                body: ColdTaskCode<'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinallyAsync(
                body,
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
                            ColdTaskResumptionFunc<'TOverall>(fun sm ->
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

        /// <summary>Creates an ColdTask that runs <c>binder(resource)</c>.
        /// The action <c>resource.DisposeAsync()</c> is executed as this computation yields its result
        /// or if the ColdTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
        /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An ColdTask that binds and eventually disposes <c>resource</c>.</returns>
        ///
        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
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

    type ColdTaskBuilder() =

        inherit ColdTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =

            let mutable sm = ColdTaskStateMachine<'T>()
            let initialResumptionFunc = ColdTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new ColdTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
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

            fun () ->
                sm.ResumptionDynamicInfo <- resumptionInfo
                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                sm.Data.MethodBuilder.Start(&sm)
                sm.Data.MethodBuilder.Task

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            if __useResumableCode then
                __stateMachine<ColdTaskStateMachineData<'T>, ColdTask<'T>>
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

                        fun () ->
                            let mutable sm = sm
                            sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                            sm.Data.MethodBuilder.Start(&sm)
                            sm.Data.MethodBuilder.Task
                    ))
            else
                ColdTaskBuilder.RunDynamic(code)

    type BackgroundColdTaskBuilder() =

        inherit ColdTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                ColdTaskBuilder.RunDynamic(code)
            else

                fun () -> Task.Run<'T>(fun () -> ColdTaskBuilder.RunDynamic (code) ())

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run(code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            if __useResumableCode then
                __stateMachine<ColdTaskStateMachineData<'T>, ColdTask<'T>>
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
                    (AfterCode<_, ColdTask<'T>>(fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            fun () ->
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy

                            fun () ->
                                Task.Run<'T>(fun () ->
                                    let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                                )
                    ))
            else
                BackgroundColdTaskBuilder.RunDynamic(code)

    [<AutoOpen>]
    module ColdTaskBuilder =

        /// <summary>
        /// Builds a coldTask using computation expression syntax.
        /// </summary>
        let coldTask = ColdTaskBuilder()

        /// <summary>
        /// Builds a coldTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundColdTask = BackgroundColdTaskBuilder()


    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type ColdTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm: byref<_>,
                    getAwaiter: unit -> ^Awaiter,
                    continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : bool =

                let mutable awaiter = getAwaiter ()

                let cont =
                    (ColdTaskResumptionFunc<'TOverall>(fun sm ->
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


            /// <summary>Creates an ColdTask that runs <c>computation</c>, and when
            /// <c>computation</c> generates a result <c>T</c>, runs <c>binder res</c>.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of <c>let!</c> in the
            /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of <c>computation</c>.</param>
            ///
            /// <returns>An ColdTask that performs a monadic bind on the result
            /// of <c>computation</c>.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    getAwaiter: unit -> ^Awaiter,
                    continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =

                ColdTaskCode<'TOverall, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        let mutable awaiter = getAwaiter ()

                        let mutable __stack_fin = true

                        if not (^Awaiter: (member get_IsCompleted: unit -> bool) (awaiter)) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result =
                                (^Awaiter: (member GetResult: unit -> 'TResult1) (awaiter))

                            (continuation result).Invoke(&sm)
                        else
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    else
                        ColdTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )

            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of <c>return!</c> in the
            /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: unit -> ^Awaiter)
                : ColdTaskCode<_, _> =
                this.Bind((fun () -> getAwaiter ()), (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    getAwaiter: unit -> ^Awaiter,
                    f
                ) : ColdTaskCode<'TResult2, 'TResult2> =
                this.Bind((fun () -> getAwaiter ()), (fun v -> this.Return(f v)))


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
                (getAwaiter: ^Awaiter)
                : unit -> ^Awaiter =
                (fun () -> getAwaiter)

            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: unit -> ^Awaiter)
                : unit -> ^Awaiter =
                getAwaiter

            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>^TaskLike</c> into a <c>unit -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline this.Source< ^TaskLike, ^Awaiter, 'T
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'T)>
                (task: ^TaskLike)
                : unit -> ^Awaiter =
                (fun () -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task)))

            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>unit -> ^TaskLike</c> into a <c>unit -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline this.Source< ^TaskLike, ^Awaiter, 'T
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'T)>
                ([<InlineIfLambda>] task: unit -> ^TaskLike)
                : unit -> ^Awaiter =
                (fun () -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task ())))


            /// <summary>Creates an ColdTask that runs <c>binder(resource)</c>.
            /// The action <c>resource.Dispose()</c> is executed as this computation yields its result
            /// or if the ColdTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
            /// <c>coldTask { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An ColdTask that binds and eventually disposes <c>resource</c>.</returns>
            ///
            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    body: 'Resource -> ColdTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(resource, body)

    [<AutoOpen>]
    module HighPriority =
        // High priority extensions
        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitColdTask(t: ColdTask<'T>) =
                async.Delay(fun () ->
                    t ()
                    |> Async.AwaitTask
                )

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitColdTask(t: ColdTask) =
                async.Delay(fun () ->
                    t ()
                    |> Async.AwaitTask
                )

            /// <summary>Runs an asynchronous computation, starting on the current operating system thread.</summary>
            static member inline AsColdTask(computation: Async<'T>) : ColdTask<_> =
                fun () -> Async.StartImmediateAsTask(computation)

        type ColdTaskBuilderBase with

            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns><c>IEnumerable</c></returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s


            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>Task&lt;'T&gt;</c> into a <c>unit -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            member inline _.Source(task: Task<'TResult1>) = (fun () -> task.GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>unit -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            member inline _.Source([<InlineIfLambda>] task: ColdTask<'TResult1>) =
                (fun () -> (task ()).GetAwaiter())

            /// <summary>Allows the computation expression to turn other types into <c>unit -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>Async&lt;'T&gt;</c> into a <c>unit -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>unit -> ^Awaiter</c></returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(Async.AsColdTask(computation))

    [<AutoOpen>]
    module AsyncExtenions =

        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind(coldTask: ColdTask<'T>, binder: ('T -> Async<'U>)) : Async<'U> =
                this.Bind(Async.AwaitColdTask coldTask, binder)

            member inline this.ReturnFrom(coldTask: ColdTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitColdTask coldTask)

            member inline this.Bind(coldTask: ColdTask, binder: (unit -> Async<'U>)) : Async<'U> =
                this.Bind(Async.AwaitColdTask coldTask, binder)

            member inline this.ReturnFrom(coldTask: ColdTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitColdTask coldTask)


        type Microsoft.FSharp.Control.TaskBuilderBase with

            member inline this.Bind(coldTask: ColdTask<'T>, binder: ('T -> _)) =
                this.Bind(coldTask (), binder)

            member inline this.ReturnFrom(coldTask: ColdTask<'T>) = this.ReturnFrom(coldTask ())

            member inline this.Bind(coldTask: ColdTask, binder: (_ -> _)) =
                this.Bind(coldTask (), binder)

            member inline this.ReturnFrom(coldTask: ColdTask) = this.ReturnFrom(coldTask ())

#if NETSTANDARD2_1
        type ValueTaskBuilderBase with

            member inline this.Source(coldTask: ColdTask<'T>) = (coldTask ()).GetAwaiter()

            member inline this.Source(coldTask: ColdTask) = (coldTask ()).GetAwaiter()
#endif

    [<RequireQualifiedAccess>]
    module ColdTask =

        /// <summary>Lifts an item to a ColdTask.</summary>
        /// <param name="item">The item to be the result of the ColdTask.</param>
        /// <returns>A ColdTask with the item as the result.</returns>
        let inline singleton (result: 'item) : ColdTask<'item> = fun () -> Task.FromResult result

        /// <summary>Allows chaining of ColdTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> ColdTask<'output>)
            ([<InlineIfLambda>] cTask: ColdTask<'input>)
            =
            coldTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of ColdTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a ColdTasks.</returns>
        let inline map
            ([<InlineIfLambda>] mapper: 'input -> 'output)
            ([<InlineIfLambda>] cTask: ColdTask<'input>)
            =
            coldTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of ColdTasks.</summary>
        /// <param name="applicable">A function wrapped in a ColdTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply
            ([<InlineIfLambda>] applicable: ColdTask<'input -> 'output>)
            ([<InlineIfLambda>] cTask: ColdTask<'input>)
            =
            coldTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two ColdTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip
            ([<InlineIfLambda>] left: ColdTask<'left>)
            ([<InlineIfLambda>] right: ColdTask<'right>)
            =
            coldTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        /// <summary>Takes two ColdTask, starts them concurrently, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in.</returns>
        let inline parallelZip
            ([<InlineIfLambda>] left: ColdTask<'left>)
            ([<InlineIfLambda>] right: ColdTask<'right>)
            =
            coldTask {
                let r1 = left ()
                let r2 = right ()
                let! r1 = r1
                let! r2 = r2
                return r1, r2
            }

        /// <summary>Coverts a ColdTask to a ColdTask\&lt;unit\&gt;.</summary>
        /// <param name="unitColdTask">The ColdTask to convert.</param>
        /// <returns>a ColdTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitColdTask: ColdTask) = coldTask {
            return! unitColdTask
        }

        /// <summary>Coverts a ColdTask\&lt;_\&gt; to a ColdTask.</summary>
        /// <param name="coldTask">The ColdTask to convert.</param>
        /// <returns>a ColdTask.</returns>
        let inline toUnit ([<InlineIfLambda>] coldTask: ColdTask<_>) : ColdTask =
            fun () -> coldTask () :> Task

        let inline internal getAwaiter ([<InlineIfLambda>] ctask: ColdTask<_>) =
            fun () -> (ctask ()).GetAwaiter()

    [<AutoOpen>]
    module Moreextensions =

        type ColdTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, ^Awaiter1, ^Awaiter2
                when ^Awaiter1 :> ICriticalNotifyCompletion
                and ^Awaiter1: (member get_IsCompleted: unit -> bool)
                and ^Awaiter1: (member GetResult: unit -> 'TResult1)
                and ^Awaiter2 :> ICriticalNotifyCompletion
                and ^Awaiter2: (member get_IsCompleted: unit -> bool)
                and ^Awaiter2: (member GetResult: unit -> 'TResult2)>
                (
                    [<InlineIfLambda>] left: unit -> ^Awaiter1,
                    [<InlineIfLambda>] right: unit -> ^Awaiter2
                ) : unit -> TaskAwaiter<'TResult1 * 'TResult2> =

                coldTask {
                    let leftStarted = left ()
                    let rightStarted = right ()
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> ColdTask.getAwaiter
