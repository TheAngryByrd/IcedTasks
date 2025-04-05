namespace IcedTasks


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
module ValueTasksUnit =
    open System
    open System.Runtime.CompilerServices
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open IcedTasks.TaskBase

    ///<summary>
    /// Contains methods to build ValueTasks using the F# computation expression syntax
    /// </summary>
    type ValueTaskUnitBuilder() =

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
        static member inline RunDynamic
            (code: TaskBaseCode<'T, 'T, AsyncValueTaskMethodBuilder>)
            : ValueTask =

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
#if DEBUG
                                sm.Data.MethodBuilder.SetResult()
#else
                                // SRTP fails here for some reason in debug mode
                                MethodBuilder.SetResult(&sm.Data.MethodBuilder)
#endif
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
            sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder.Create()
            MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
            MethodBuilder.get_Task (&sm.Data.MethodBuilder)

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: TaskBaseCode<'T, 'T, AsyncValueTaskMethodBuilder>) : ValueTask =
            if __useResumableCode then
                __stateMachine<TaskBaseStateMachineData<'T, AsyncValueTaskMethodBuilder>, ValueTask>
                    (MoveNextMethodImpl<_>(fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint
                        let mutable __stack_exn: Exception = null

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
#if DEBUG
                                sm.Data.MethodBuilder.SetResult()
#else
                                // SRTP fails here for some reason in debug mode
                                MethodBuilder.SetResult(&sm.Data.MethodBuilder)
#endif
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
                        sm.Data.MethodBuilder <- AsyncValueTaskMethodBuilder.Create()
                        MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
                        MethodBuilder.get_Task (&sm.Data.MethodBuilder)
                    ))
            else
                ValueTaskUnitBuilder.RunDynamic(code)

        /// Specify a Source of ValueTask on the real type to allow type inference to work
        member inline _.Source(v: ValueTask) = Awaitable.GetAwaiter v

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

    /// Contains the valueTask computation expression builder.
    module ValueTaskBuilder =

        /// <summary>
        /// Builds a valueTask using computation expression syntax.
        /// </summary>
        let valueTaskUnit = ValueTaskUnitBuilder()

        /// <summary>
        /// Builds a valueTask using computation expression syntax.
        /// </summary>
        let vTaskUnit = valueTaskUnit
