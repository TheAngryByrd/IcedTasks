namespace IcedTasks


open System.Threading.Tasks

#if NET6_0_OR_GREATER


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

/// Contains methods to build PoolingValueTasks using the F# computation expression syntax
[<AutoOpen>]
module PoolingValueTasks =
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
    type PoolingValueTaskstateMachineData<'T> =

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: PoolingAsyncValueTaskMethodBuilder<'T>

    /// This is used by the compiler as a template for creating state machine structs
    and PoolingValueTaskstateMachine<'TOverall> =
        ResumableStateMachine<PoolingValueTaskstateMachineData<'TOverall>>

    /// Represents the runtime continuation of a poolingValueTask state machine created dynamically
    and PoolingValueTaskResumptionFunc<'TOverall> =
        ResumptionFunc<PoolingValueTaskstateMachineData<'TOverall>>

    /// Represents the runtime continuation of a poolingValueTask state machine created dynamically
    and PoolingValueTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<PoolingValueTaskstateMachineData<'TOverall>>

    /// A special compiler-recognised delegate type for specifying blocks of poolingValueTask code with access to the state machine
    and PoolingValueTaskCode<'TOverall, 'T> =
        ResumableCode<PoolingValueTaskstateMachineData<'TOverall>, 'T>

    /// <summary>
    /// Contains methods to build PoolingValueTasks using the F# computation expression syntax
    /// </summary>
    type PoolingValueTaskBuilderBase() =


        /// <summary>Creates a ValueTask that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A poolingValueTask that runs generator</returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> PoolingValueTaskCode<'TOverall, 'T>)
            : PoolingValueTaskCode<'TOverall, 'T> =
            ResumableCode.Delay(fun () -> generator ())


        /// <summary>Creates an ValueTask that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// poolingValueTask { ... } computation expression syntax.
        /// </remarks>
        /// <returns>An ValueTask that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : PoolingValueTaskCode<'TOverall, unit> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An ValueTask that returns value when executed.</returns>
        member inline _.Return(value: 'T) : PoolingValueTaskCode<'T, 'T> =
            PoolingValueTaskCode<'T, _>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an ValueTask that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An ValueTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: PoolingValueTaskCode<'TOverall, unit>,
                task2: PoolingValueTaskCode<'TOverall, 'T>
            ) : PoolingValueTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates an ValueTask that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>An ValueTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: PoolingValueTaskCode<'TOverall, unit>
            ) : PoolingValueTaskCode<'TOverall, unit> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates an ValueTask that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: PoolingValueTaskCode<'TOverall, 'T>,
                catchHandler: exn -> PoolingValueTaskCode<'TOverall, 'T>
            ) : PoolingValueTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: PoolingValueTaskCode<'TOverall, 'T>,
                compensation: unit -> unit
            ) : PoolingValueTaskCode<'TOverall, 'T> =
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
        /// poolingValueTask { ... } computation expression syntax.</remarks>
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
                body: 'T -> PoolingValueTaskCode<'TOverall, unit>
            ) : PoolingValueTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// poolingValueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: PoolingValueTaskCode<'TOverall, 'T>,
                compensation: unit -> ValueTask
            ) : PoolingValueTaskCode<'TOverall, 'T> =
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
                            PoolingValueTaskResumptionFunc<'TOverall>(fun sm ->
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
        /// poolingValueTask { ... } computation expression syntax.</remarks>
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
                binder: 'Resource -> PoolingValueTaskCode<'TOverall, 'T>
            ) : PoolingValueTaskCode<'TOverall, 'T> =
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
    /// Contains methods to build PoolingValueTasks using the F# computation expression syntax
    /// </summary>
    type PoolingValueTaskBuilder() =

        inherit PoolingValueTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: PoolingValueTaskCode<'T, 'T>) : ValueTask<'T> =

            let mutable sm = PoolingValueTaskstateMachine<'T>()

            let initialResumptionFunc =
                PoolingValueTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new PoolingValueTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
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
            sm.Data.MethodBuilder <- PoolingAsyncValueTaskMethodBuilder<'T>.Create()
            sm.Data.MethodBuilder.Start(&sm)
            sm.Data.MethodBuilder.Task

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: PoolingValueTaskCode<'T, 'T>) : ValueTask<'T> =
            if __useResumableCode then
                __stateMachine<PoolingValueTaskstateMachineData<'T>, ValueTask<'T>>
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
                        sm.Data.MethodBuilder <- PoolingAsyncValueTaskMethodBuilder<'T>.Create()
                        sm.Data.MethodBuilder.Start(&sm)
                        sm.Data.MethodBuilder.Task
                    ))
            else
                PoolingValueTaskBuilder.RunDynamic(code)

    /// Contains methods to build PoolingValueTasks using the F# computation expression syntax
    type BackgroundPoolingValueTaskBuilder() =

        inherit PoolingValueTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: PoolingValueTaskCode<'T, 'T>) : ValueTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                PoolingValueTaskBuilder.RunDynamic(code)
            else
                Task.Run<'T>((fun () -> (PoolingValueTaskBuilder.RunDynamic code).AsTask()))
                |> ValueTask<'T>

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run(code: PoolingValueTaskCode<'T, 'T>) : ValueTask<'T> =
            if __useResumableCode then
                __stateMachine<PoolingValueTaskstateMachineData<'T>, ValueTask<'T>>
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

                            sm.Data.MethodBuilder <-
                                PoolingAsyncValueTaskMethodBuilder<'T>.Create()

                            sm.Data.MethodBuilder.Start(&sm)
                            sm.Data.MethodBuilder.Task
                        else
                            let sm = sm

                            Task.Run<'T>(
                                (fun () ->
                                    let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread

                                    sm.Data.MethodBuilder <-
                                        PoolingAsyncValueTaskMethodBuilder<'T>.Create()

                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task.AsTask()
                                )
                            )
                            |> ValueTask<'T>
                    ))

            else
                BackgroundPoolingValueTaskBuilder.RunDynamic(code)


    /// Contains the poolingValueTask computation expression builder.
    [<AutoOpen>]
    module ValueTaskBuilder =

        /// <summary>
        /// Builds a poolingValueTask using computation expression syntax.
        /// </summary>
        let poolingValueTask = PoolingValueTaskBuilder()

        /// <summary>
        /// Builds a poolingValueTask using computation expression syntax.
        /// </summary>
        let pvTask = poolingValueTask

        /// <summary>
        /// Builds a poolingValueTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundPoolingValueTask = BackgroundPoolingValueTaskBuilder()

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type PoolingValueTaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    sm: byref<ResumableStateMachine<PoolingValueTaskstateMachineData<'TOverall>>>,
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> PoolingValueTaskCode<'TOverall, 'TResult2>)
                ) : bool =

                let mutable awaiter = getAwaiter

                let cont =
                    (PoolingValueTaskResumptionFunc<'TOverall>(fun sm ->
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
            /// poolingValueTask { ... } computation expression syntax.</remarks>
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
                    continuation: ('TResult1 -> PoolingValueTaskCode<'TOverall, 'TResult2>)
                ) : PoolingValueTaskCode<'TOverall, 'TResult2> =

                PoolingValueTaskCode<'TOverall, _>(fun sm ->
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
                        PoolingValueTaskBuilderBase.BindDynamic<'TResult1, 'TResult2, 'Awaiter, 'TOverall>(
                            &sm,
                            getAwaiter,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of return! in the
            /// poolingValueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : PoolingValueTaskCode<_, _> =
                this.Bind(getAwaiter, (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.BindReturn<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (
                    getAwaiter: 'Awaiter,
                    f
                ) : PoolingValueTaskCode<'TResult2, 'TResult2> =
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
            /// poolingValueTask { ... } computation expression syntax.</remarks>
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
                    binder: 'Resource -> PoolingValueTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(resource, binder)

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        // High priority extensions
        type PoolingValueTaskBuilderBase with

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


        /// <summary>Allows chaining of PoolingValueTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> ValueTask<'output>)
            (cTask: ValueTask<'input>)
            =
            poolingValueTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of PoolingValueTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a PoolingValueTasks.</returns>
        let inline map ([<InlineIfLambda>] mapper: 'input -> 'output) (cTask: ValueTask<'input>) =
            poolingValueTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of PoolingValueTasks.</summary>
        /// <param name="applicable">A function wrapped in a PoolingValueTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply (applicable: ValueTask<'input -> 'output>) (cTask: ValueTask<'input>) =
            poolingValueTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two PoolingValueTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip (left: ValueTask<'left>) (right: ValueTask<'right>) =
            poolingValueTask {
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
                poolingValueTask { return! vtask }

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

        type PoolingValueTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
                when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
                (
                    left: 'Awaiter1,
                    right: 'Awaiter2
                ) : ValueTaskAwaiter<'TResult1 * 'TResult2> =

                poolingValueTask {
                    let leftStarted = left
                    let rightStarted = right
                    let! leftResult = leftStarted
                    let! rightResult = rightStarted
                    return leftResult, rightResult
                }
                |> Awaitable.GetAwaiter

#endif
