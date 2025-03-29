namespace IcedTasks


open System.Threading.Tasks


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
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators

    ///<summary>
    /// Contains methods to build ValueTasks using the F# computation expression syntax
    /// </summary>
    type ValueTaskBuilder() =

        inherit TaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: TaskBaseCode<'T, 'T, _>) : ValueTask<'T> =

            let mutable sm = TaskBaseStateMachine<'T, _>()

            let initialResumptionFunc =
                TaskBaseResumptionFunc<'T, _>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new TaskBaseResumptionDynamicInfo<'T, _>(initialResumptionFunc) with
                    member info.MoveNext(sm) =
                        let mutable savedExn = null

                        try
                            sm.ResumptionDynamicInfo.ResumptionData <- null
                            let step = info.ResumptionFunc.Invoke(&sm)

                            if step then
                                MethodBuilder.SetResult(&sm.Data.MethodBuilder, sm.Data.Result)
                            else
                                match sm.ResumptionDynamicInfo.ResumptionData with
                                | :? ICriticalNotifyCompletion as awaiter ->
                                    let mutable awaiter = awaiter
                                    // assert not (isNull awaiter)
                                    MethodBuilder.AwaitOnCompleted(
                                        &sm.Data.MethodBuilder,
                                        &awaiter,
                                        &sm
                                    )
                                | awaiter -> assert not (isNull awaiter)

                        with exn ->
                            savedExn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match savedExn with
                        | null -> ()
                        | exn -> MethodBuilder.SetException(&sm.Data.MethodBuilder, exn)

                    member _.SetStateMachine(sm, state) =
                        MethodBuilder.SetStateMachine(&sm.Data.MethodBuilder, state)
                }

            sm.ResumptionDynamicInfo <- resumptionInfo
            sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
            MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
            MethodBuilder.get_Task (&sm.Data.MethodBuilder)

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: TaskBaseCode<'T, 'T, _>) : ValueTask<'T> =
            if __useResumableCode then
                __stateMachine<TaskBaseStateMachineData<'T, _>, ValueTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint
                        let mutable __stack_exn = null

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
                                MethodBuilder.SetResult(&sm.Data.MethodBuilder, sm.Data.Result)
                        with exn ->
                            __stack_exn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match __stack_exn with
                        | null -> ()
                        | exn -> MethodBuilder.SetException(&sm.Data.MethodBuilder, exn)
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        MethodBuilder.SetStateMachine(&sm.Data.MethodBuilder, state)
                    ))
                    (AfterCode<_, _>(fun sm ->
                        sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder<'T>.Create()
                        MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
                        MethodBuilder.get_Task (&sm.Data.MethodBuilder)
                    ))
            else
                ValueTaskBuilder.RunDynamic(code)

        /// Specify a Source of ValueTask<_> on the real type to allow type inference to work
        member inline _.Source(v: ValueTask<_>) = Awaitable.GetAwaiter v

        member inline this.MergeSources(left, right) =
            this.Source(
                this.Run(
                    this.Bind(
                        left,
                        fun leftR -> this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                    )
                )
            )

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


    /// Contains a set of standard functional helper function
    [<RequireQualifiedAccess>]
    module ValueTask =

        /// <summary>Lifts an item to a ValueTask.</summary>
        /// <param name="item">The item to be the result of the ValueTask.</param>
        /// <returns>A ValueTask with the item as the result.</returns>
        let inline singleton (item: 'item) : ValueTask<'item> = ValueTask<'item> item

        /// <summary>Allows chaining of ValueTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] (binder: 'input -> ValueTask<'output>))
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
