namespace IcedTasks


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
                                let mutable awaiter =
                                    sm.ResumptionDynamicInfo.ResumptionData
                                    :?> ICriticalNotifyCompletion

                                assert not (isNull awaiter)

                                MethodBuilder.AwaitUnsafeOnCompleted(
                                    &sm.Data.MethodBuilder,
                                    &awaiter,
                                    &sm
                                )

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
                        let mutable __stack_exn: Exception = null

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

// /// Contains a set of standard functional helper function
// [<RequireQualifiedAccess>]
// module ValueTask =
//     open System.Threading.Tasks

//     /// <summary>Lifts an item to a ValueTask.</summary>
//     /// <param name="item">The item to be the result of the ValueTask.</param>
//     /// <returns>A ValueTask with the item as the result.</returns>
//     let inline singleton (item: 'item) : ValueTask<'item> = ValueTask<'item> item


//     /// <summary>Allows chaining of PoolingValueTasks.</summary>
//     /// <param name="binder">The continuation.</param>
//     /// <param name="cTask">The value.</param>
//     /// <returns>The result of the binder.</returns>
//     let inline bind
//         ([<InlineIfLambda>] binder: 'input -> ValueTask<'output>)
//         (cTask: ValueTask<'input>)
//         =
//         poolingValueTask {
//             let! cResult = cTask
//             return! binder cResult
//         }

//     /// <summary>Allows chaining of PoolingValueTasks.</summary>
//     /// <param name="mapper">The continuation.</param>
//     /// <param name="cTask">The value.</param>
//     /// <returns>The result of the mapper wrapped in a PoolingValueTasks.</returns>
//     let inline map ([<InlineIfLambda>] mapper: 'input -> 'output) (cTask: ValueTask<'input>) =
//         poolingValueTask {
//             let! cResult = cTask
//             return mapper cResult
//         }

//     /// <summary>Allows chaining of PoolingValueTasks.</summary>
//     /// <param name="applicable">A function wrapped in a PoolingValueTasks</param>
//     /// <param name="cTask">The value.</param>
//     /// <returns>The result of the applicable.</returns>
//     let inline apply (applicable: ValueTask<'input -> 'output>) (cTask: ValueTask<'input>) =
//         poolingValueTask {
//             let! applier = applicable
//             let! cResult = cTask
//             return applier cResult
//         }

//     /// <summary>Takes two PoolingValueTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
//     /// <param name="left">The left value.</param>
//     /// <param name="right">The right value.</param>
//     /// <returns>A tuple of the parameters passed in</returns>
//     let inline zip (left: ValueTask<'left>) (right: ValueTask<'right>) =
//         poolingValueTask {
//             let! r1 = left
//             let! r2 = right
//             return r1, r2
//         }

//     let inline ofUnit (vtask: ValueTask) : ValueTask<unit> =
//         // this implementation follows Stephen Toub's advice, see:
//         // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
//         if vtask.IsCompletedSuccessfully then
//             ValueTask<unit>()
//         else
//             poolingValueTask { return! vtask }

//     /// <summary>Initializes a new instance of the System.Threading.Tasks.ValueTask class using the supplied task that represents the operation.</summary>
//     /// <param name="task">The task.</param>
//     let inline ofTask (task: Task<'T>) = ValueTask<'T> task

//     /// <summary>Initializes a new instance of the System.Threading.Tasks.ValueTask class using the supplied task that represents the operation.</summary>
//     /// <param name="task"> The task that represents the operation</param>
//     /// <returns></returns>
//     let inline ofTaskUnit (task: Task) = ValueTask task

//     /// <summary>Retrieves a System.Threading.Tasks.Task object that represents this System.Threading.Tasks.ValueTask`1</summary>
//     /// <param name="vtask"></param>
//     /// <typeparam name="'T"></typeparam>
//     /// <returns>
//     /// The System.Threading.Tasks.Task object that is wrapped in this  System.Threading.Tasks.ValueTask if one exists,
//     /// or a new  System.Threading.Tasks.Task object that represents the result.
//     /// </returns>
//     let inline toTask (vtask: ValueTask<'T>) = vtask.AsTask()

//     /// <summary>Retrieves a System.Threading.Tasks.Task object that represents this System.Threading.Tasks.ValueTask.</summary>
//     let inline toTaskUnit (vtask: ValueTask) = vtask.AsTask()

//     /// <summary>Converts a ValueTask&lt;T&gt; to its non-generic counterpart.</summary>
//     /// <param name="vtask"></param>
//     /// <typeparam name="'T"></typeparam>
//     /// <returns></returns>
//     let inline toUnit (vtask: ValueTask<'T>) : ValueTask =
//         // this implementation follows Stephen Toub's advice, see:
//         // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
//         if vtask.IsCompletedSuccessfully then
//             // ensure any side effect executes
//             vtask.Result
//             |> ignore

//             ValueTask()
//         else
//             ValueTask(vtask.AsTask())

/// <exclude/>
// [<AutoOpen>]
// module MergeSourcesExtensions =

//     type PoolingValueTaskBuilderBase with

//         [<NoEagerConstraintApplication>]
//         member inline this.MergeSources<'TResult1, 'TResult2, 'Awaiter1, 'Awaiter2
//             when Awaiter<'Awaiter1, 'TResult1> and Awaiter<'Awaiter2, 'TResult2>>
//             (
//                 left: 'Awaiter1,
//                 right: 'Awaiter2
//             ) : ValueTaskAwaiter<'TResult1 * 'TResult2> =

//             poolingValueTask {
//                 let leftStarted = left
//                 let rightStarted = right
//                 let! leftResult = leftStarted
//                 let! rightResult = rightStarted
//                 return leftResult, rightResult
//             }
//             |> Awaitable.GetAwaiter

#endif
