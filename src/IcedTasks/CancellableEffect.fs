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

[<AutoOpen>]
module CancellableEffect =

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
    type CancellableEffect<'Env, 'T> = 'Env -> CancellationToken -> ValueTask<'T>

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableEffectStateMachineData<'Env, 'T> =

        [<DefaultValue(false)>]
        val mutable Environemnt: 'Env

        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncValueTaskMethodBuilder<'T>

        member inline this.ThrowIfCancellationRequested() =
            this.CancellationToken.ThrowIfCancellationRequested()

    and CancellableEffectStateMachine<'Env, 'TOverall> =
        ResumableStateMachine<CancellableEffectStateMachineData<'Env, 'TOverall>>

    and CancellableEffectResumptionFunc<'Env, 'TOverall> =
        ResumptionFunc<CancellableEffectStateMachineData<'Env, 'TOverall>>

    and CancellableEffectResumptionDynamicInfo<'Env, 'TOverall> =
        ResumptionDynamicInfo<CancellableEffectStateMachineData<'Env, 'TOverall>>

    and CancellableEffectCode<'Env, 'TOverall, 'T> =
        ResumableCode<CancellableEffectStateMachineData<'Env, 'TOverall>, 'T>

    type CancellableEffectBuilderBase() =


        /// <summary>Creates a CancellableEffect that runs <c>generator</c></summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A CancellableEffect that runs <c>generator</c></returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator:
                unit -> CancellableEffectCode<'Env, 'TOverall, 'T>)
            : CancellableEffectCode<'Env, 'TOverall, 'T> =
            ResumableCode.Delay(fun () ->
                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    (generator ()).Invoke(&sm)
                )
            )


        /// <summary>Creates an CancellableEffect that just returns <c>()</c>.</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty <c>else</c> branches in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.
        /// </remarks>
        /// <returns>An CancellableEffect that returns <c>()</c>.</returns>
        [<DefaultValue>]
        member inline _.Zero() : CancellableEffectCode<'Env, 'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result <c>v</c>.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of <c>return</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An Reader that returns <c>value</c> when executed.</returns>
        member inline _.Return(value: 'T) : CancellableEffectCode<'Env, 'T, 'T> =
            CancellableEffectCode(fun sm ->
                sm.Data.ThrowIfCancellationRequested()
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an CancellableEffect that first runs <c>task1</c>
        /// and then runs <c>computation2</c>, returning the result of <c>computation2</c>.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An CancellableEffect that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: CancellableEffectCode<'Env, 'TOverall, unit>,
                task2: CancellableEffectCode<'Env, 'TOverall, 'T>
            ) : CancellableEffectCode<'Env, 'TOverall, 'T> =
            ResumableCode.Combine(
                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task1.Invoke(&sm)
                ),

                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    task2.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableEffect that runs <c>computation</c> repeatedly
        /// until <c>guard()</c> becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>while</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing <c>computation</c>.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a <c>while</c> expression.</param>
        ///
        /// <returns>An CancellableEffect that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: CancellableEffectCode<'Env, 'TOverall, unit>
            ) : CancellableEffectCode<'Env, 'TOverall, unit> =
            ResumableCode.While(
                guard,
                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                )
            )

        /// <summary>Creates an CancellableEffect that runs <c>computation</c> and returns its result.
        /// If an exception happens then <c>catchHandler(exn)</c> is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/with</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when <c>computation</c> throws an exception.</param>
        ///
        /// <returns>An CancellableEffect that executes <c>computation</c> and calls <c>catchHandler</c> if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: CancellableEffectCode<'Env, 'TOverall, 'T>,
                catchHandler: exn -> CancellableEffectCode<'Env, 'TOverall, 'T>
            ) : CancellableEffectCode<'Env, 'TOverall, 'T> =
            ResumableCode.TryWith(
                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                catchHandler
            )

        /// <summary>Creates an CancellableEffect that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An CancellableEffect that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: CancellableEffectCode<'Env, 'TOverall, 'T>,
                compensation: unit -> unit
            ) : CancellableEffectCode<'Env, 'TOverall, 'T> =
            ResumableCode.TryFinally(

                CancellableEffectCode(fun sm ->
                    sm.Data.ThrowIfCancellationRequested()
                    computation.Invoke(&sm)
                ),
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an CancellableEffect that enumerates the sequence <c>seq</c>
        /// on demand and runs <c>body</c> for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of <c>for</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an CancellableEffect.  Can be seen as the body of the <c>for</c> expression.</param>
        ///
        /// <returns>An CancellableEffect that will enumerate the sequence and run <c>body</c>
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> CancellableEffectCode<'Env, 'TOverall, unit>
            ) : CancellableEffectCode<'Env, 'TOverall, unit> =
            ResumableCode.For(
                sequence,
                fun item ->
                    CancellableEffectCode(fun sm ->
                        sm.Data.ThrowIfCancellationRequested()
                        (body item).Invoke(&sm)
                    )
            )

#if NETSTANDARD2_1
        /// <summary>Creates an CancellableEffect that runs <c>computation</c>. The action <c>compensation</c> is executed
        /// after <c>computation</c> completes, whether <c>computation</c> exits normally or by an exception. If <c>compensation</c> raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>try/finally</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after <c>computation</c> completes or raises an
        /// exception.</param>
        ///
        /// <returns>An CancellableEffect that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: CancellableEffectCode<'Env, 'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : CancellableEffectCode<'Env, 'TOverall, 'T> =
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
                            CancellableEffectResumptionFunc<'Env, 'TOverall>(fun sm ->
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

        /// <summary>Creates an CancellableEffect that runs <c>binder(resource)</c>.
        /// The action <c>resource.DisposeAsync()</c> is executed as this computation yields its result
        /// or if the CancellableEffect exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
        /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An CancellableEffect that binds and eventually disposes <c>resource</c>.</returns>
        ///
        member inline this.Using<'Env, 'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> CancellableEffectCode<'Env, 'TOverall, 'T>
            ) : CancellableEffectCode<'Env, 'TOverall, 'T> =
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

    type CancellableEffectBuilder() =

        inherit CancellableEffectBuilderBase()

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
            (code: CancellableEffectCode<'Env, 'T, 'T>)
            : CancellableEffect<'Env, 'T> =

            let mutable sm = CancellableEffectStateMachine<'Env, 'T>()

            let initialResumptionFunc =
                CancellableEffectResumptionFunc<'Env, 'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new CancellableEffectResumptionDynamicInfo<'Env, 'T>(initialResumptionFunc) with
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

            fun environment (ct) ->
                if ct.IsCancellationRequested then
                    ValueTask.FromCanceled<_>(ct)
                else
                    sm.Data.Environemnt <- environment
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo
                    sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create ()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run
            (code: CancellableEffectCode<'Env, 'T, 'T>)
            : CancellableEffect<'Env, 'T> =
            if __useResumableCode then
                __stateMachine<CancellableEffectStateMachineData<'Env, 'T>, CancellableEffect<'Env, 'T>>
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

                        fun environment (ct) ->
                            if ct.IsCancellationRequested then
                                ValueTask.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.Environemnt <- environment
                                sm.Data.CancellationToken <- ct
                                sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create ()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                CancellableEffectBuilder.RunDynamic(code)

    type BackgroundCancellableEffectBuilder() =

        inherit CancellableEffectBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic
            (code: CancellableEffectCode<'Env, 'T, 'T>)
            : CancellableEffect<'Env, 'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                CancellableEffectBuilder.RunDynamic(code)
            else
                fun environemnt (ct) ->
                    Task.Run<'T>(
                        (fun () ->
                            (CancellableEffectBuilder.RunDynamic code environemnt ct).AsTask()
                        ),
                        ct
                    )
                    |> ValueTask<'T>

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run
            (code: CancellableEffectCode<'Env, 'T, 'T>)
            : CancellableEffect<'Env, 'T> =
            if __useResumableCode then
                __stateMachine<CancellableEffectStateMachineData<'Env, 'T>, CancellableEffect<'Env, 'T>>
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
                    (AfterCode<_, CancellableEffect<'Env, 'T>>(fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            fun environment (ct) ->
                                if ct.IsCancellationRequested then
                                    ValueTask.FromCanceled<_>(ct)
                                else
                                    sm.Data.Environemnt <- environment
                                    sm.Data.CancellationToken <- ct

                                    sm.Data.MethodBuilder <-
                                        AsyncValueTaskMethodBuilder<'T>.Create ()

                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy contents of state machine so we can capture it

                            fun environment (ct) ->
                                if ct.IsCancellationRequested then
                                    ValueTask.FromCanceled<_>(ct)
                                else
                                    Task.Run<'T>(
                                        (fun () ->
                                            let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                            sm.Data.Environemnt <- environment
                                            sm.Data.CancellationToken <- ct

                                            sm.Data.MethodBuilder <-
                                                AsyncValueTaskMethodBuilder<'T>.Create ()

                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task.AsTask()
                                        ),
                                        ct
                                    )
                                    |> ValueTask<'T>
                    ))

            else
                BackgroundCancellableEffectBuilder.RunDynamic(code)


    [<AutoOpen>]
    module CancellableEffectBuilder =

        /// <summary>
        /// Builds a readerCancellableEffect using computation expression syntax.
        /// </summary>
        let cancellableEffect = CancellableEffectBuilder()

        /// <summary>
        /// Builds a readerCancellableEffect using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundCancellableEffect = BackgroundCancellableEffectBuilder()

    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableEffectBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm:
                        byref<ResumableStateMachine<CancellableEffectStateMachineData<'Env, 'TOverall>>>,
                    [<InlineIfLambda>] getAwaiter: 'Env -> CancellationToken -> ^Awaiter,
                    continuation: ('TResult1 -> CancellableEffectCode<'Env, 'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.ThrowIfCancellationRequested()

                let mutable awaiter = getAwaiter sm.Data.Environemnt sm.Data.CancellationToken

                let cont =
                    (CancellableEffectResumptionFunc<'Env, 'TOverall>(fun sm ->
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

            /// <summary>Creates an CancellableEffect that runs <c>computation</c>, and when
            /// <c>computation</c> generates a result <c>T</c>, runs <c>binder res</c>.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of <c>let!</c> in the
            /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of <c>computation</c>.</param>
            ///
            /// <returns>An CancellableEffect that performs a monadic bind on the result
            /// of <c>computation</c>.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    [<InlineIfLambda>] getAwaiter: 'Env -> CancellationToken -> ^Awaiter,
                    continuation: ('TResult1 -> CancellableEffectCode<'Env, 'TOverall, 'TResult2>)
                ) : CancellableEffectCode<'Env, 'TOverall, 'TResult2> =

                CancellableEffectCode<'Env, 'TOverall, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.ThrowIfCancellationRequested()
                        // Get an awaiter from the awaitable
                        let mutable awaiter =
                            getAwaiter sm.Data.Environemnt sm.Data.CancellationToken

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
                        CancellableEffectBuilderBase.BindDynamic<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of <c>return!</c> in the
            /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: 'Env -> CancellationToken -> ^Awaiter)
                : CancellableEffectCode<_, _, _> =
                this.Bind((fun env ct -> getAwaiter env ct), (fun v -> this.Return v))


            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    getAwaiter: 'Env -> CancellationToken -> ^Awaiter,
                    f
                ) : CancellableEffectCode<'Env, 'TResult2, 'TResult2> =
                this.Bind((fun env ct -> getAwaiter env ct), (fun v -> this.Return(f v)))


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Env, 'TResult1, 'TResult2, ^Awaiter, 'TOverall
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (getAwaiter: ^Awaiter)
                : 'Env -> CancellationToken -> ^Awaiter =
                (fun env ct -> getAwaiter)


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambdaAttribute>] getAwaiter: 'Env -> CancellationToken -> ^Awaiter)
                : 'Env -> CancellationToken -> ^Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (task: ^TaskLike)
                : 'Env -> CancellationToken -> ^Awaiter =
                (fun env (ct: CancellationToken) ->
                    (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))
                )

            /// <summary>Creates an CancellableEffect that runs <c>binder(resource)</c>.
            /// The action <c>resource.Dispose()</c> is executed as this computation yields its result
            /// or if the CancellableEffect exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of <c>use</c> and <c>use!</c> in the
            /// <c>readerCancellableEffect { ... }</c> computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An CancellableEffect that binds and eventually disposes <c>resource</c>.</returns>
            ///
            member inline _.Using<'Env, 'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    binder: 'Resource -> CancellableEffectCode<'Env, 'TOverall, 'T>
                ) =
                ResumableCode.Using(
                    resource,
                    fun resource ->
                        CancellableEffectCode<'Env, 'TOverall, 'T>(fun sm ->
                            sm.Data.ThrowIfCancellationRequested()
                            (binder resource).Invoke(&sm)
                        )
                )


    [<AutoOpen>]
    module LOL3Priority =
        type CancellableEffectBuilderBase with

            member inline this.Source
                ([<InlineIfLambda>] func: 'Env -> CancellationToken -> 'Result)
                =
                fun env (ct: CancellationToken) -> ValueTask<'Result>(func env ct).GetAwaiter()


    [<AutoOpen>]
    module LOL6Priority =

        type CancellableEffectBuilderBase with

            member inline this.Source([<InlineIfLambda>] func: 'Env -> 'Result) =
                fun env (ct: CancellationToken) -> ValueTask<'Result>(func env).GetAwaiter()

    [<AutoOpen>]
    module HighPriority =
        // High priority extensions
        type CancellableEffectBuilderBase with

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellationToken -> ^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambda>] task: 'Env -> CancellationToken -> ^TaskLike)
                : 'Env -> CancellationToken -> ^Awaiter =
                (fun env ct -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task env ct)))


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
                (fun env (ct: CancellationToken) -> task.GetAwaiter())


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>Async&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source(computation: Async<'TResult1>) =
                fun env ct -> Async.StartImmediateAsTask(computation, ct).GetAwaiter()


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellableTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline _.Source(awaiter: TaskAwaiter<'TResult1>) =
                (fun env (ct: CancellationToken) -> awaiter)


    [<AutoOpen>]
    module LOL2Priority =
        type CancellableEffectBuilderBase with


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellationToken -> ^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambda>] task: unit -> ^TaskLike)
                : 'Env -> CancellationToken -> ^Awaiter =
                (fun env ct -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task ())))

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>CancellationToken -> ^TaskLike</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall, 'Env
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                ([<InlineIfLambda>] task: CancellationToken -> ^TaskLike)
                : 'Env -> CancellationToken -> ^Awaiter =
                (fun env ct -> (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task ct)))


    [<AutoOpen>]
    module LOL4Priority =
        type CancellableEffectBuilderBase with

            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source([<InlineIfLambda>] task: unit -> Task<'TResult1>) =
                (fun env (ct: CancellationToken) -> (task ()).GetAwaiter())


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source
                ([<InlineIfLambda>] task: CancellationToken -> Task<'TResult1>)
                =
                (fun env (ct: CancellationToken) -> (task ct).GetAwaiter())


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source
                ([<InlineIfLambda>] task: 'Env -> CancellationToken -> Task<'out>)
                =
                (fun env (ct: CancellationToken) -> (task env ct).GetAwaiter())


            /// <summary>Allows the computation expression to turn other types into <c>CancellationToken -> ^Awaiter</c></summary>
            ///
            /// <remarks>This turns a <c>ColdTask&lt;'T&gt;</c> into a <c>CancellationToken -> ^Awaiter</c>.</remarks>
            ///
            /// <returns><c>CancellationToken -> ^Awaiter</c></returns>
            member inline this.Source([<InlineIfLambda>] task: 'Env -> Async<'out>) =
                (fun env (ct: CancellationToken) ->
                    Async.StartImmediateAsTask(task env, ct).GetAwaiter()
                )


    [<RequireQualifiedAccess>]
    module CancellableEffect =

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
        ///         readerCancellableEffect {
        ///             let! cancellationToken = CancellableEffect.getCancellationToken()
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
            CancellableEffectBuilder.cancellableEffect.Run(
                CancellableEffectCode<_, _, _>(fun sm ->
                    sm.Data.Result <- sm.Data.CancellationToken
                    true
                )
            )

        let inline getEnvironment<'Environment> () =
            CancellableEffectBuilder.cancellableEffect.Run(
                CancellableEffectCode<'Environment, _, _>(fun sm ->
                    sm.Data.Result <- sm.Data.Environemnt
                    true
                )
            )

        let inline ask<'Environment> () = getEnvironment<'Environment> ()

// /// <summary>Lifts an item to a CancellableEffect.</summary>
// /// <param name="item">The item to be the result of the CancellableEffect.</param>
// /// <returns>A CancellableEffect with the item as the result.</returns>
// let inline singleton (item: 'item) : CancellableEffect<'item> =
//     fun (ct: CancellationToken) -> ValueTask<'item> item


// /// <summary>Allows chaining of CancellableEffects.</summary>
// /// <param name="binder">The continuation.</param>
// /// <param name="cTask">The value.</param>
// /// <returns>The result of the binder.</returns>
// let inline bind
//     ([<InlineIfLambda>] binder: 'input -> CancellableEffect<'env, 'output>)
//     ([<InlineIfLambda>] cTask: CancellableEffect<'env, 'input>)
//     =
//     readerCancellableEffect {
//         let! cResult = cTask
//         return! binder cResult
//     }

// /// <summary>Allows chaining of CancellableEffects.</summary>
// /// <param name="mapper">The continuation.</param>
// /// <param name="cTask">The value.</param>
// /// <returns>The result of the mapper wrapped in a CancellableEffects.</returns>
// let inline map
//     ([<InlineIfLambda>] mapper: 'input -> 'output)
//     ([<InlineIfLambda>] cTask: CancellableEffect<'env, 'input>)
//     =
//     readerCancellableEffect {
//         let! cResult = cTask
//         return mapper cResult
//     }

// /// <summary>Allows chaining of CancellableEffects.</summary>
// /// <param name="applicable">A function wrapped in a CancellableEffects</param>
// /// <param name="cTask">The value.</param>
// /// <returns>The result of the applicable.</returns>
// let inline apply
//     ([<InlineIfLambda>] applicable: CancellableEffect<'env, 'input -> 'output>)
//     ([<InlineIfLambda>] cTask: CancellableEffect<'env, 'input>)
//     =
//     readerCancellableEffect {
//         let! applier = applicable
//         let! cResult = cTask
//         return applier cResult
//     }

// /// <summary>Takes two CancellableEffects, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
// /// <param name="left">The left value.</param>
// /// <param name="right">The right value.</param>
// /// <returns>A tuple of the parameters passed in</returns>
// let inline zip
//     ([<InlineIfLambda>] left: CancellableEffect<'env, 'left>)
//     ([<InlineIfLambda>] right: CancellableEffect<'env, 'right>)
//     =
//     readerCancellableEffect {
//         let! r1 = left
//         let! r2 = right
//         return r1, r2
//     }

// /// <summary>Takes two CancellableEffect, starts them concurrently, and returns a tuple of the pair.</summary>
// /// <param name="left">The left value.</param>
// /// <param name="right">The right value.</param>
// /// <returns>A tuple of the parameters passed in.</returns>
// let inline parallelZip
//     ([<InlineIfLambda>] left: CancellableEffect<'env, 'left>)
//     ([<InlineIfLambda>] right: CancellableEffect<'env, 'right>)
//     =
//     readerCancellableEffect {
//         let! env = getEnvironment ()
//         let! ct = getCancellationToken ()
//         let r1 = left env ct
//         let r2 = right env ct
//         let! r1 = r1
//         let! r2 = r2
//         return r1, r2
//     }


// let inline internal getAwaiter
//     ([<InlineIfLambda>] ctask: CancellableEffect<'env, _>)
//     =
//     fun env ct -> (ctask env ct).GetAwaiter()


// [<AutoOpen>]
// module Moreextensions =

//     type CancellableEffectBuilderBase with

//         [<NoEagerConstraintApplication>]
//         member inline this.MergeSources<'TResult1, 'TResult2, ^Awaiter1, ^Awaiter2, 'Env
//             when ^Awaiter1 :> ICriticalNotifyCompletion
//             and ^Awaiter1: (member get_IsCompleted: unit -> bool)
//             and ^Awaiter1: (member GetResult: unit -> 'TResult1)
//             and ^Awaiter2 :> ICriticalNotifyCompletion
//             and ^Awaiter2: (member get_IsCompleted: unit -> bool)
//             and ^Awaiter2: (member GetResult: unit -> 'TResult2)>
//             (
//                 [<InlineIfLambda>] left: 'Env -> CancellationToken -> ^Awaiter1,
//                 [<InlineIfLambda>] right: 'Env -> CancellationToken -> ^Awaiter2
//             ) : 'Env -> CancellationToken -> ValueTaskAwaiter<'TResult1 * 'TResult2> =

//             readerCancellableEffect {
//                 let! env = CancellableEffect.getEnvironment ()
//                 let! ct = CancellableEffect.getCancellationToken ()
//                 let leftStarted = left env ct
//                 let rightStarted = right env ct
//                 let! leftResult = leftStarted
//                 let! rightResult = rightStarted
//                 return leftResult, rightResult
//             }
//             |> CancellableEffect.getAwaiter


#endif


module Reader =
    open System.Threading
    open System.Threading.Tasks

    let inline apply
        ([<InlineIfLambda>] fn: 'env -> CancellationToken -> ValueTask<'out>)
        : CancellableEffect<'env, 'out> =
        fn

    let inline applyTask
        ([<InlineIfLambda>] fn: 'env -> CancellationToken -> Task<'out>)
        : CancellableEffect<'env, 'out> =
        fun env ct ->
            fn env ct
            |> ValueTask<'out>


    let inline applyAsync
        ([<InlineIfLambda>] fn: 'env -> Async<'out>)
        : CancellableEffect<'env, 'out> =
        fun env ct ->
            Async.StartImmediateAsTask(fn env, ct)
            |> ValueTask<'out>


    let inline applySync ([<InlineIfLambda>] fn: 'env -> 'out) : CancellableEffect<'env, 'out> =
        fun env _ ->
            fn env
            |> ValueTask<'out>


module Example =
    open System.Threading.Tasks
    open System.Threading
    open System

    [<Interface>]
    type ILogger =
        abstract Debug: string -> unit
        abstract Error: string -> unit

    [<Interface>]
    type IProvideLogger =
        abstract Logger: ILogger

    module Log =
        let debug message (x: #IProvideLogger) = x.Logger.Debug message

        let debug2 message =
            Reader.applySync (fun (x: #IProvideLogger) -> x.Logger.Debug message)

        let debugDid message (x: #IProvideLogger) =
            x.Logger.Debug message
            true

        let debugCancellable message (x: #IProvideLogger) (ct: CancellationToken) =
            ct.ThrowIfCancellationRequested()
            x.Logger.Debug message
            true

    [<Interface>]
    type IDatabase =
        abstract Query<'i, 'o> : string * 'i * CancellationToken -> Task<'o>
        abstract Execute: string * 'i * CancellationToken -> Task<unit>

    [<Interface>]
    type IProvideDatabase =
        abstract Database: IDatabase

    type User = { Name: string }

    module Db =
        let fetchUser userId (env: #IProvideDatabase) ct =
            env.Database.Query<_, User>("Sql.FetchUser", {| userId = userId |}, ct)


        let fetchUser2 userId =
            Reader.applyTask (fun (env: #IProvideDatabase) ct ->
                env.Database.Query<_, User>("Sql.FetchUser", {| userId = userId |}, ct)
            )

        let updateUser user (env: #IProvideDatabase) ct =
            env.Database.Execute("Sql.UpdateUser", user, ct)


        let updateUser2 user =
            Reader.applyTask (fun (env: #IProvideDatabase) ct ->
                env.Database.Execute("Sql.UpdateUser", user, ct)
            )


    [<Interface>]
    type IClock =
        abstract UtcNow: DateTimeOffset

    [<Interface>]
    type IProvideClock =
        abstract Clock: IClock

    module Time =
        let utcNow (env: #IProvideClock) = env.Clock.UtcNow

    let foo (userId: string) env ct = valueTask {
        let! user = Db.fetchUser userId env ct
        let now = Time.utcNow env
        Log.debug (sprintf "User: %A" user) env
        return user
    }

    let foo2 (userId: string) = cancellableEffect {
        let! user = Db.fetchUser userId
        // let! now = Time.utcNow
        do! Log.debug (sprintf "User: %A" user)
        return user
    }


    let foo3 userId = cancellableEffect {

        // Task/Task<'T>
        do! Task.Yield()
        do! Task.Delay(0)
        do! Task.FromResult(())
        do! task { return () }

        // ValueTask/ValueTask<'T>
        do! ValueTask<unit>()
        do! ValueTask()
        do! ValueTask<unit>(task { return () })
        do! ValueTask(Task.Delay(0))
        do! valueTask { return () }

        // Async<'T>
        do! Async.Sleep 0
        do! async { return () }
        do! fun (logger: #IProvideLogger) -> async { logger.Logger.Debug("lol") }

        // ColdTask/ColdTask<'T>
        do! fun () -> Task.Yield()
        do! fun () -> Task.Delay(0)
        do! fun () -> Task.FromResult(())
        do! fun () -> task { return () }
        do! coldTask { return () }

        // CancellableTask/CancellableTask<'T>
        do! fun (ct: CancellationToken) -> Task.Yield()
        do! fun (ct: CancellationToken) -> Task.Delay(0, ct)
        do! fun (ct: CancellationToken) -> Task.FromResult(())
        do! fun (ct: CancellationToken) -> task { return () }
        do! cancellableTask { return () }

        do!
            fun (logger: #IProvideLogger) (ct: CancellationToken) -> task {
                logger.Logger.Debug("lol")
            }

        do! fun (logger: #IProvideLogger) -> cancellableTask { logger.Logger.Debug("lol") }

        // CancellableValueTask/CancellableValueTask<'T>
        do! fun (ct: CancellationToken) -> ValueTask<unit>(())
        do! fun (ct: CancellationToken) -> ValueTask(Task.Delay(0, ct))
        do! fun (ct: CancellationToken) -> Task.FromResult(())
        do! fun (ct: CancellationToken) -> ValueTask<unit>(task { return () })
        do! cancellableValueTask { return () }
        do! fun (logger: #IProvideLogger) -> cancellableValueTask { logger.Logger.Debug("lol") }

        do!
            fun (logger: #IProvideLogger) (ct: CancellationToken) -> valueTask {
                logger.Logger.Debug("lol")
            }

        // Sync
        do! fun (logger: #IProvideLogger) -> logger.Logger.Debug(" ")
        let! (utcNow: DateTimeOffset) = fun (env: #IProvideClock) -> env.Clock.UtcNow
        do! Log.debug "lol"
        do! Log.debug2 "lol2"
        let! (didIt: bool) = Log.debugDid "lol"
        let! didit = Log.debugCancellable "lol"


        // Get environment directly
        let! (env: #IProvideLogger) = CancellableEffect.getEnvironment ()
        env.Logger.Debug "got it"

        // Get CancellationToken directly
        let! (ct: CancellationToken) = CancellableEffect.getCancellationToken ()

        // CancellableEffect<'T>
        let! user1 = foo2 userId

        for i = 0 to 100 do
            do! fun ct -> Task.Delay(0, ct)

        let mutable loopI = 0

        while loopI < 100 do
            do! Async.Sleep(0)
            loopI <- loopI + 1

        return user1, didIt
    }

    [<Struct>]
    type AppEnv =
        interface IProvideLogger with
            member _.Logger = Unchecked.defaultof<_>

        interface IProvideDatabase with
            member _.Database = Unchecked.defaultof<_>

        interface IProvideClock with
            member _.Clock = Unchecked.defaultof<_>


    let result =
        let appEnv = AppEnv()

        (foo3 "213" (appEnv) CancellationToken.None).GetAwaiter().GetResult()
