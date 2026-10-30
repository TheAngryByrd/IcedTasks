namespace IcedTasks

namespace IcedTasks.PoolingValueTasks

#if NET6_0_OR_GREATER

open IcedTasks
open IcedTasks.TaskLike
open IcedTasks.TaskBase
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


/// Contains methods to build PoolingValueTasks using the F# computation expression syntax
[<AutoOpen>]
module PoolingValueTasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators

    ///<summary>
    /// Contains methods to build PoolingValueTasks using the F# computation expression syntax
    /// </summary>
    type PoolingValueTaskBuilder() =
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
            sm.Data.MethodBuilder <- PoolingAsyncValueTaskMethodBuilder<'T>.Create()
            sm.Data.MethodBuilder.Start(&sm)
            sm.Data.MethodBuilder.Task

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
                        sm.Data.MethodBuilder <- PoolingAsyncValueTaskMethodBuilder<'T>.Create()
                        sm.Data.MethodBuilder.Start(&sm)
                        sm.Data.MethodBuilder.Task
                    ))
            else
                PoolingValueTaskBuilder.RunDynamic(code)


        /// Specify a Source of ValueTask<_> on the real type to allow type inference to work
        member inline _.Source(v: ValueTask<_>) = Awaitable.GetAwaiter v

        [<NoEagerConstraintApplication>]
        member inline this.MergeSources(left, right) =
            this.Source(
                this.Run(
                    this.Bind(
                        left,
                        fun leftR -> this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                    )
                )
            )


    /// Contains the poolingValueTask computation expression builder.
    [<AutoOpen>]
    module ValueTaskBuilder =

        /// <summary>
        /// Builds a poolingValueTask using computation expression syntax.
        /// </summary>
        let poolingValueTask = PoolingValueTaskBuilder()

        /// <summary>
        /// Alias for <see cref="F:IcedTasks.PoolingValueTasks.PoolingValueTasks.ValueTaskBuilder.poolingValueTask" />.
        /// </summary>
        let pvTask = poolingValueTask

        /// Contains functional helper functions for composing and converting pooling-backed <see cref="T:System.Threading.Tasks.ValueTask`1" /> values.
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
            let inline map
                ([<InlineIfLambda>] mapper: 'input -> 'output)
                (cTask: ValueTask<'input>)
                =
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

            /// <summary>Converts a non-generic <see cref="T:System.Threading.Tasks.ValueTask" /> to a pooling-backed <see cref="T:System.Threading.Tasks.ValueTask`1" /> of unit.</summary>
            /// <param name="vtask">The non-generic ValueTask to convert.</param>
            /// <returns>A ValueTask whose result is unit.</returns>
            let inline ofUnit (vtask: ValueTask) : ValueTask<unit> =
                // this implementation follows Stephen Toub's advice, see:
                // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
                if vtask.IsCompletedSuccessfully then
                    ValueTask<unit>()
                else
                    poolingValueTask { return! vtask }

            /// <summary>Wraps a <see cref="T:System.Threading.Tasks.Task`1" /> as a <see cref="T:System.Threading.Tasks.ValueTask`1" />.</summary>
            /// <param name="task">The task to wrap.</param>
            /// <returns>A ValueTask that represents the same operation as <paramref name="task" />.</returns>
            let inline ofTask (task: Task<'T>) = ValueTask<'T> task

            /// <summary>Wraps a non-generic <see cref="T:System.Threading.Tasks.Task" /> as a non-generic <see cref="T:System.Threading.Tasks.ValueTask" />.</summary>
            /// <param name="task">The task to wrap.</param>
            /// <returns>A ValueTask that represents the same operation as <paramref name="task" />.</returns>
            let inline ofTaskUnit (task: Task) = ValueTask task

            /// <summary>Retrieves a <see cref="T:System.Threading.Tasks.Task`1" /> that represents the supplied <see cref="T:System.Threading.Tasks.ValueTask`1" />.</summary>
            /// <param name="vtask">The ValueTask to convert.</param>
            /// <typeparam name="'T">The result type of the ValueTask.</typeparam>
            /// <returns>
            /// The wrapped Task if one exists, or a new Task that represents the ValueTask result.
            /// </returns>
            let inline toTask (vtask: ValueTask<'T>) = vtask.AsTask()

            /// <summary>Retrieves a non-generic <see cref="T:System.Threading.Tasks.Task" /> that represents the supplied non-generic <see cref="T:System.Threading.Tasks.ValueTask" />.</summary>
            /// <param name="vtask">The ValueTask to convert.</param>
            /// <returns>The Task representation of <paramref name="vtask" />.</returns>
            let inline toTaskUnit (vtask: ValueTask) = vtask.AsTask()

            /// <summary>Converts a <see cref="T:System.Threading.Tasks.ValueTask`1" /> to its non-generic counterpart.</summary>
            /// <param name="vtask">The ValueTask whose result should be discarded.</param>
            /// <typeparam name="'T">The result type to discard.</typeparam>
            /// <returns>A non-generic ValueTask that completes when <paramref name="vtask" /> completes.</returns>
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

#endif
