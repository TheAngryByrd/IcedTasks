// Task builder for F# that compiles to allocation-free paths for synchronous code.
//
// Originally written in 2016 by Robert Peele (humbobst@gmail.com)
// New operator-based overload resolution for F# 4.0 compatibility by Gustavo Leon in 2018.
// Revised for insertion into FSharp.Core by Microsoft, 2019.
// Revised to implement Task semantics
//
// Original notice:
// To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights
// to this software to the public domain worldwide. This software is distributed without any warranty.

namespace IcedTasks.Polyfill.Task

/// <namespacedoc>
///   <summary>
///     Namespace contains polyfills for <see cref='T:IcedTasks.Polyfill.Task.Tasks.TaskBuilder'/>. Opening this namespace will <a href="https://en.wikipedia.org/wiki/Variable_shadowing">shadow</a> the <c>task {...}</c> builder with the version in IcedTasks.
///     </summary>
/// </namespacedoc>
/// Contains methods to build Tasks using the F# computation expression syntax
[<AutoOpen>]
module Tasks =
    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open IcedTasks
    open IcedTasks.TaskBase

    ///<summary>
    /// Contains methods to build Tasks using the F# computation expression syntax
    /// </summary>
    type TaskBuilder() =

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
        static member inline RunDynamic(code: TaskBaseCode<'T, 'T, _>) : Task<'T> =

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
            sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
            MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
            MethodBuilder.get_Task (&sm.Data.MethodBuilder)

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: TaskBaseCode<'T, 'T, _>) : Task<'T> =
            if __useResumableCode then
                __stateMachine<TaskBaseStateMachineData<'T, _>, _>
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
                        sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                        MethodBuilder.Start(&sm.Data.MethodBuilder, &sm)
                        MethodBuilder.get_Task (&sm.Data.MethodBuilder)
                    ))
            else
                TaskBuilder.RunDynamic(code)

        /// Specify a Source of Task<_> on the real type to allow type inference to work
        member inline _.Source(v: Task<_>) = Awaitable.GetTaskAwaiter v

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


    /// Contains methods to build Tasks using the F# computation expression syntax
    type BackgroundTaskBuilder() =

        inherit TaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic(code: TaskBaseCode<'T, 'T, _>) =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                TaskBuilder.RunDynamic(code)
            else

                Task.Run<'T>(fun () -> TaskBuilder.RunDynamic(code))

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the threadpool using Task.Run
        /// </summary>
        member inline _.Run(code: TaskBaseCode<'T, 'T, _>) =
            if __useResumableCode then
                __stateMachine<TaskBaseStateMachineData<'T, _>, _>
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
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                            sm.Data.MethodBuilder.Start(&sm)
                            sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy

                            Task.Run<'T>(fun () ->
                                let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                            )
                    ))
            else
                BackgroundTaskBuilder.RunDynamic(code)


        /// Specify a Source of Task<_> on the real type to allow type inference to work
        member inline _.Source(v: Task<_>) = Awaitable.GetTaskAwaiter v

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

    /// Contains the task computation expression builder.
    module TaskBuilder =

        /// <summary>
        /// Builds a task using computation expression syntax
        ///
        /// <b>NOTE:</b> This is the TaskBuilder defined in IcedTasks. This fixes any issues with the TaskBuilder defined in FSharp.Core that can't be backported.
        /// </summary>
        let task = TaskBuilder()

        /// <summary>
        /// Builds a task using computation expression syntax which switches to execute on a background thread if not already doing so.
        ///
        /// <b>NOTE:</b> This is the BackgroundTaskBuilder defined in IcedTasks. This fixes any issues with the BackgroundTaskBuilder defined in FSharp.Core that can't be backported.
        /// </summary>
        let backgroundTask = BackgroundTaskBuilder()
