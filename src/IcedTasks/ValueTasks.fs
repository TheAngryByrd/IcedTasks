namespace IcedTasks


open System.Threading.Tasks

#if NETSTANDARD2_1 || NET6_0_OR_GREATER

/// <summary>
/// Module with extension methods for <see cref="T:System.Threading.Tasks.ValueTask`1"/>.
/// </summary>
[<AutoOpen>]
module ValueTaskExtensions =

    type ValueTask with

        /// <summary>Creates a <see cref="T:System.Threading.Tasks.ValueTask" /> that's completed due to cancellation with a specified cancellation token.</summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Cancellation has not been requested for <paramref name="cancellationToken" />; its <see cref="P:System.Threading.CancellationToken.IsCancellationRequested" /> property is <see langword="false" />.</exception>
        static member FromCanceled(cancellationToken) =
            new ValueTask(Task.FromCanceled(cancellationToken))

        /// <summary>Creates a <see cref="T:System.Threading.Tasks.ValueTask`1" /> that's completed due to cancellation with a specified cancellation token.</summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <returns>The canceled task.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Cancellation has not been requested for <paramref name="cancellationToken" />; its <see cref="P:System.Threading.CancellationToken.IsCancellationRequested" /> property is <see langword="false" />.</exception>
        static member FromCanceled<'T>(cancellationToken) =
            new ValueTask<'T>(Task.FromCanceled<'T>(cancellationToken))

    type Microsoft.FSharp.Control.Async with

        /// <summary>
        /// Return an asynchronous computation that will check if ValueTask is completed or wait for
        /// the given task to complete and return its result.
        /// </summary>
        /// <param name="vTask">The task to await.</param>
        static member inline AwaitValueTask(vTask: ValueTask<_>) : Async<_> =
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if vTask.IsCompletedSuccessfully then
                async.Return vTask.Result
            else
                Async.AwaitTask(vTask.AsTask())


        /// <summary>
        /// Return an asynchronous computation that will check if ValueTask is completed or wait for
        /// the given task to complete and return its result.
        /// </summary>
        /// <param name="vTask">The task to await.</param>
        static member inline AwaitValueTask(vTask: ValueTask) : Async<unit> =
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if vTask.IsCompletedSuccessfully then
                async.Return()
            else
                Async.AwaitTask(vTask.AsTask())


        /// <summary>
        /// Runs an asynchronous computation, starting immediately on the current operating system thread,
        /// but also returns the execution as <see cref="T:System.Threading.Tasks.ValueTask`1" />.
        /// </summary>
        static member inline AsValueTask(computation: Async<'T>) : ValueTask<'T> =
            Async.StartImmediateAsTask(computation)
            |> ValueTask<'T>


// Task builder for F# that compiles to allocation-free paths for synchronous code.
//
// Originally written in 2016 by Robert Peele (humbobst@gmail.com)
// New operator-based overload resolution for F# 4.0 compatibility by Gustavo Leon in 2018.
// Revised for insertion into FSharp.Core by Microsoft, 2019.
// Revised to implement ValueTask semantics
//
// Original notice:
// To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights
// to this software to the public domain worldwide. This software is distributed without any warranty.

namespace IcedTasks

/// Contains methods to build ValueTasks using the F# computation expression syntax
[<AutoOpen>]
module ValueTasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type ValueTaskStateMachineData<'T> =

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncValueTaskMethodBuilder<'T>

    /// This is used by the compiler as a template for creating state machine structs
    and ValueTaskStateMachine<'TOverall> =
        ResumableStateMachine<ValueTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a valueTask state machine created dynamically
    and ValueTaskResumptionFunc<'TOverall> = ResumptionFunc<ValueTaskStateMachineData<'TOverall>>

    /// Represents the runtime continuation of a valueTask state machine created dynamically
    and ValueTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<ValueTaskStateMachineData<'TOverall>>

    /// A special compiler-recognised delegate type for specifying blocks of valueTask code with access to the state machine
    and ValueTaskCode<'TOverall, 'T> = ResumableCode<ValueTaskStateMachineData<'TOverall>, 'T>

    /// <summary>
    /// Contains methods to build ValueTasks using the F# computation expression syntax
    /// </summary>
    type ValueTaskBuilderBase() =


        /// <summary>Creates a ValueTask that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A valueTask that runs generator</returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> ValueTaskCode<'TOverall, 'T>)
            : ValueTaskCode<'TOverall, 'T> =
            ResumableCode.Delay(fun () -> generator ())


        /// <summary>Creates an ValueTask that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// valueTask { ... } computation expression syntax.
        /// </remarks>
        /// <returns>An ValueTask that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : ValueTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An ValueTask that returns value when executed.</returns>
        member inline _.Return(value: 'T) : ValueTaskCode<'T, 'T> =
            ValueTaskCode<'T, _>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an ValueTask that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An ValueTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: ValueTaskCode<'TOverall, unit>,
                task2: ValueTaskCode<'TOverall, 'T>
            ) : ValueTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates an ValueTask that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>An ValueTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: ValueTaskCode<'TOverall, unit>
            ) : ValueTaskCode<'TOverall, unit> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates an ValueTask that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: ValueTaskCode<'TOverall, 'T>,
                catchHandler: exn -> ValueTaskCode<'TOverall, 'T>
            ) : ValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: ValueTaskCode<'TOverall, 'T>,
                compensation: unit -> unit
            ) : ValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(
                computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an ValueTask that enumerates the sequence seq
        /// on demand and runs body for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of for in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an ValueTask.  Can be seen as the body of the for expression.</param>
        ///
        /// <returns>An ValueTask that will enumerate the sequence and run body
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> ValueTaskCode<'TOverall, unit>
            ) : ValueTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: ValueTaskCode<'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : ValueTaskCode<'TOverall, 'T> =
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
                            ValueTaskResumptionFunc<'TOverall>(fun sm ->
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

        /// <summary>Creates an ValueTask that runs binder(resource).
        /// The action resource.DisposeAsync() is executed as this computation yields its result
        /// or if the ValueTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of use and use! in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An ValueTask that binds and eventually disposes resource.</returns>
        ///
        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                binder: 'Resource -> ValueTaskCode<'TOverall, 'T>
            ) : ValueTaskCode<'TOverall, 'T> =
            this.TryFinallyAsync(
                (fun sm -> (binder resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                    else
                        ValueTask()
                )
            )

    ///<summary>
    /// Contains methods to build ValueTasks using the F# computation expression syntax
    /// </summary>
    type ValueTaskBuilder() =

        inherit ValueTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: ValueTaskCode<'T, 'T>) : ValueTask<'T> =

            let mutable sm = ValueTaskStateMachine<'T>()

            let initialResumptionFunc = ValueTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new ValueTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
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

            sm.ResumptionDynamicInfo <- resumptionInfo
            sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
            AsyncMethodBuilder.Start(sm.Data.MethodBuilder, &sm)
            AsyncMethodBuilder.Task sm.Data.MethodBuilder

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: ValueTaskCode<'T, 'T>) : ValueTask<'T> =
            if __useResumableCode then
                __stateMachine<ValueTaskStateMachineData<'T>, ValueTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint
                        let mutable __stack_exn: Exception = null

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then

                                // AsyncMethodBuilder.setResult sm.Data.MethodBuilder sm.Data.Result
                                // sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                                AsyncMethodBuilder.SetResult(sm.Data.MethodBuilder, sm.Data.Result)
                        // printfn "gothere 3"

                        with exn ->
                            __stack_exn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match __stack_exn with
                        | null -> ()
                        | exn -> sm.Data.MethodBuilder.SetException(exn)

                    // AsyncMethodBuilder.SetException(sm.Data.MethodBuilder, exn)
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        sm.Data.MethodBuilder.SetStateMachine(state)
                    // AsyncMethodBuilder.SetStateMachine(sm.Data.MethodBuilder, state)
                    ))
                    (AfterCode<_, _>(fun sm ->
                        sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
                        sm.Data.MethodBuilder.Start(&sm)
                        // AsyncMethodBuilder.Start(sm.Data.MethodBuilder, &sm)
                        // AsyncMethodBuilder.Task sm.Data.MethodBuilder
                        sm.Data.MethodBuilder.Task

                    ))
            else
                ValueTaskBuilder.RunDynamic(code)

    /// Contains methods to build ValueTasks using the F# computation expression syntax
    type BackgroundValueTaskBuilder() =

        inherit ValueTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: ValueTaskCode<'T, 'T>) : ValueTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                ValueTaskBuilder.RunDynamic(code)
            else
                Task.Run<'T>((fun () -> (ValueTaskBuilder.RunDynamic code).AsTask()))
                |> ValueTask<'T>

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run(code: ValueTaskCode<'T, 'T>) : ValueTask<'T> =
            if __useResumableCode then
                __stateMachine<ValueTaskStateMachineData<'T>, ValueTask<'T>>
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
                    (AfterCode<_, ValueTask<'T>>(fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then

                            sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()

                            sm.Data.MethodBuilder.Start(&sm)
                            sm.Data.MethodBuilder.Task
                        else
                            let sm = sm

                            Task.Run<'T>(
                                (fun () ->
                                    let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread

                                    sm.Data.MethodBuilder <-
                                        AsyncValueTaskMethodBuilder<'T>.Create()

                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task.AsTask()
                                )
                            )
                            |> ValueTask<'T>
                    ))

            else
                BackgroundValueTaskBuilder.RunDynamic(code)


    /// Contains the valueTask computation expression builder.
    [<AutoOpen>]
    module ValueTaskBuilder =

        /// <summary>
        /// Builds a valueTask using computation expression syntax.
        /// </summary>
        let valueTask = ValueTaskBuilder()

        /// <summary>
        /// Builds a valueTask using computation expression syntax.
        /// </summary>
        let vTask = valueTask

        /// <summary>
        /// Builds a valueTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundValueTask = BackgroundValueTaskBuilder()

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type ValueTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    sm: byref<ResumableStateMachine<ValueTaskStateMachineData<'TOverall>>>,
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> ValueTaskCode<'TOverall, 'TResult2>)
                ) : bool =

                let mutable awaiter = getAwaiter

                let cont =
                    (ValueTaskResumptionFunc<'TOverall>(fun sm ->
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

            /// <summary>Creates an ValueTask that runs computation, and when
            /// computation generates a result T, runs binder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of let! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of computation.</param>
            ///
            /// <returns>An ValueTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> ValueTaskCode<'TOverall, 'TResult2>)
                ) : ValueTaskCode<'TOverall, 'TResult2> =

                ValueTaskCode<'TOverall, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = getAwaiter

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
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    else
                        ValueTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of return! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : ValueTaskCode<_, _> =
                this.Bind(getAwaiter, (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: 'Awaiter,
                    f
                ) : ValueTaskCode<'TResult2, 'TResult2> =
                this.Bind(getAwaiter, (fun v -> this.Return(f v)))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : 'Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a ^Awaitable into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (task: 'Awaitable)
                : 'Awaiter =
                Awaitable.GetAwaiter task


            /// <summary>Creates an ValueTask that runs binder(resource).
            /// The action resource.Dispose() is executed as this computation yields its result
            /// or if the ValueTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of use and use! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An ValueTask that binds and eventually disposes resource.</returns>
            ///
            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    binder: 'Resource -> ValueTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(resource, binder)

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        // High priority extensions
        type ValueTaskBuilderBase with

            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline _.Source(task: Task<'T>) = task.GetAwaiter()

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(Async.StartImmediateAsTask(computation))


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(awaiter: TaskAwaiter<'TResult1>) = awaiter

    /// Contains a set of standard functional helper function
    [<RequireQualifiedAccess>]
    module ValueTask =
        open System.Threading.Tasks

        /// <summary>Lifts an item to a ValueTask.</summary>
        /// <param name="item">The item to be the result of the ValueTask.</param>
        /// <returns>A ValueTask with the item as the result.</returns>
        let inline singleton (item: 'item) : ValueTask<'item> = ValueTask<'item> item


        /// <summary>Allows chaining of ValueTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> ValueTask<'output>)
            (cTask: ValueTask<'input>)
            =
            valueTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of ValueTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a ValueTasks.</returns>
        let inline map ([<InlineIfLambda>] mapper: 'input -> 'output) (cTask: ValueTask<'input>) =
            valueTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of ValueTasks.</summary>
        /// <param name="applicable">A function wrapped in a ValueTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply (applicable: ValueTask<'input -> 'output>) (cTask: ValueTask<'input>) =
            valueTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two ValueTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip (left: ValueTask<'left>) (right: ValueTask<'right>) =
            valueTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        let inline ofUnit (vtask: ValueTask) : ValueTask<unit> =
            // this implementation follows Stephen Toub's advice, see:
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if vtask.IsCompletedSuccessfully then
                ValueTask<unit>()
            else
                valueTask { return! vtask }

        /// <summary>Initializes a new instance of the System.Threading.Tasks.ValueTask class using the supplied task that represents the operation.</summary>
        /// <param name="task">The task.</param>
        let inline ofTask (task: Task<'T>) = ValueTask<'T> task

        /// <summary>Initializes a new instance of the System.Threading.Tasks.ValueTask class using the supplied task that represents the operation.</summary>
        /// <param name="task"> The task that represents the operation</param>
        /// <returns></returns>
        let inline ofTaskUnit (task: Task) = ValueTask task

        /// <summary>Retrieves a System.Threading.Tasks.Task object that represents this System.Threading.Tasks.ValueTask`1</summary>
        /// <param name="vtask"></param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>
        /// The System.Threading.Tasks.Task object that is wrapped in this  System.Threading.Tasks.ValueTask if one exists,
        /// or a new  System.Threading.Tasks.Task object that represents the result.
        /// </returns>
        let inline toTask (vtask: ValueTask<'T>) = vtask.AsTask()

        /// <summary>Retrieves a System.Threading.Tasks.Task object that represents this System.Threading.Tasks.ValueTask.</summary>
        let inline toTaskUnit (vtask: ValueTask) = vtask.AsTask()

        /// <summary>Converts a ValueTask&lt;T&gt; to its non-generic counterpart.</summary>
        /// <param name="vtask"></param>
        /// <typeparam name="'T"></typeparam>
        /// <returns></returns>
        let inline toUnit (vtask: ValueTask<'T>) : ValueTask =
            // this implementation follows Stephen Toub's advice, see:
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if vtask.IsCompletedSuccessfully then
                // ensure any side effect executes
                vtask.Result
                |> ignore

                ValueTask()
            else
                ValueTask(vtask.AsTask())

    /// <exclude/>
    [<AutoOpen>]
    module MergeSourcesExtensions =

        type ValueTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    left: 'Awaiter1,
                    right: 'Awaiter2
                ) : ValueTaskAwaiter<'TResult1 * 'TResult2> =

                valueTask {
                    let leftStarted = left
                    let rightStarted = right
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> Awaitable.GetAwaiter

#endif
