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

    type ColdTask<'T> = unit -> Task<'T>
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
    and ColdTaskResumptionDynamicInfo<'TOverall> = ResumptionDynamicInfo<ColdTaskStateMachineData<'TOverall>>
    and ColdTaskCode<'TOverall, 'T> = ResumableCode<ColdTaskStateMachineData<'TOverall>, 'T>

    type ColdTaskBuilderBase() =

        member inline _.Delay
            ([<InlineIfLambda>] generator: unit -> ColdTaskCode<'TOverall, 'T>)
            : ColdTaskCode<'TOverall, 'T> =
            ColdTaskCode<'TOverall, 'T>(fun sm -> (generator ()).Invoke(&sm))

        /// Used to represent no-ops like the implicit empty "else" branch of an "if" expression.
        [<DefaultValue>]
        member inline _.Zero() : ColdTaskCode<'TOverall, unit> = ResumableCode.Zero()

        member inline _.Return(value: 'T) : ColdTaskCode<'T, 'T> =
            ColdTaskCode<'T, _> (fun sm ->
                sm.Data.Result <- value
                true)

        /// Chains together a step with its following step.
        /// Note that this requires that the first step has no result.
        /// This prevents constructs like `task { return 1; return 2; }`.
        member inline _.Combine
            (
                [<InlineIfLambda>] task1: ColdTaskCode<'TOverall, unit>,
                [<InlineIfLambda>] task2: ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// Builds a step that executes the body while the condition predicate is true.
        member inline _.While
            (
                [<InlineIfLambda>] condition: unit -> bool,
                body: ColdTaskCode<'TOverall, unit>
            ) : ColdTaskCode<'TOverall, unit> =
            ResumableCode.While(condition, body)

        /// Wraps a step in a try/with. This catches exceptions both in the evaluation of the function
        /// to retrieve the step, and in the continuation of the step (if any).
        member inline _.TryWith
            (
                body: ColdTaskCode<'TOverall, 'T>,
                catch: exn -> ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(body, catch)

        /// Wraps a step in a try/finally. This catches exceptions both in the evaluation of the function
        /// to retrieve the step, and in the continuation of the step (if any).
        member inline _.TryFinally
            (
                body: ColdTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(
                body,
                ResumableCode<_, _> (fun _sm ->
                    compensation ()
                    true)
            )

        member inline _.For(sequence: seq<'T>, body: 'T -> ColdTaskCode<'TOverall, unit>) : ColdTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

    #if NETSTANDARD2_1
        member inline internal this.TryFinallyAsync
            (
                [<InlineIfLambda>] body: ColdTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] compensation: unit -> ValueTask
            ) : ColdTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinallyAsync(
                body,
                ResumableCode<_, _> (fun sm ->
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
                            ColdTaskResumptionFunc<'TOverall> (fun sm ->
                                awaiter.GetResult() |> ignore
                                true)

                        // shortcut to continue immediately
                        if awaiter.IsCompleted then
                            true
                        else
                            sm.ResumptionDynamicInfo.ResumptionData <- (awaiter :> ICriticalNotifyCompletion)
                            sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                            false)
            )

        member inline this.Using<'Resource, 'TOverall, 'T when 'Resource :> IAsyncDisposable>
            (
                resource: 'Resource,
                [<InlineIfLambda>] body: 'Resource -> ColdTaskCode<'TOverall, 'T>
            ) : ColdTaskCode<'TOverall, 'T> =
            this.TryFinallyAsync(
                (fun sm -> (body resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                    else
                        ValueTask())
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
        static member inline RunDynamic([<InlineIfLambda>] code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =

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
                                    sm.ResumptionDynamicInfo.ResumptionData :?> ICriticalNotifyCompletion

                                assert not (isNull awaiter)
                                sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)

                        with
                        | exn -> savedExn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match savedExn with
                        | null -> ()
                        | exn -> sm.Data.MethodBuilder.SetException exn

                    member _.SetStateMachine(sm, state) =
                        sm.Data.MethodBuilder.SetStateMachine(state) }

            fun () ->
                sm.ResumptionDynamicInfo <- resumptionInfo
                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                sm.Data.MethodBuilder.Start(&sm)
                sm.Data.MethodBuilder.Task

        member inline _.Run([<InlineIfLambda>] code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            if __useResumableCode then
                __stateMachine<ColdTaskStateMachineData<'T>, ColdTask<'T>>
                    (MoveNextMethodImpl<_> (fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint
                        let mutable __stack_exn: Exception = null

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
                                sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                        with
                        | exn -> __stack_exn <- exn
                        // Run SetException outside the stack unwind, see https://github.com/dotnet/roslyn/issues/26567
                        match __stack_exn with
                        | null -> ()
                        | exn -> sm.Data.MethodBuilder.SetException exn
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state -> sm.Data.MethodBuilder.SetStateMachine(state)))
                    (AfterCode<_, _> (fun sm ->
                        let mutable sm = sm
                        fun () ->
                            sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                            sm.Data.MethodBuilder.Start(&sm)
                            sm.Data.MethodBuilder.Task))
            else
                ColdTaskBuilder.RunDynamic(code)

    type BackgroundColdTaskBuilder() =

        inherit ColdTaskBuilderBase()

        static member inline RunDynamic([<InlineIfLambda>] code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                ColdTaskBuilder.RunDynamic(code)
            else

                fun () -> Task.Run<'T>(fun () -> ColdTaskBuilder.RunDynamic(code) ())

        //// Same as ColdTaskBuilder.Run except the start is inside Task.Run if necessary
        member inline _.Run([<InlineIfLambda>] code: ColdTaskCode<'T, 'T>) : ColdTask<'T> =
            if __useResumableCode then
                __stateMachine<ColdTaskStateMachineData<'T>, ColdTask<'T>>
                    (MoveNextMethodImpl<_> (fun sm ->
                        //-- RESUMABLE CODE START
                        __resumeAt sm.ResumptionPoint

                        try
                            let __stack_code_fin = code.Invoke(&sm)

                            if __stack_code_fin then
                                sm.Data.MethodBuilder.SetResult(sm.Data.Result)
                        with
                        | exn -> sm.Data.MethodBuilder.SetException exn
                    //-- RESUMABLE CODE END
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state -> sm.Data.MethodBuilder.SetStateMachine(state)))
                    (AfterCode<_, ColdTask<'T>> (fun sm ->
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
                                Task.Run<'T> (fun () ->
                                    let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task)))
            else
                BackgroundColdTaskBuilder.RunDynamic(code)

    [<AutoOpen>]
    module ColdTaskBuilder =

        let coldTask = ColdTaskBuilder()
        let backgroundColdTask = BackgroundColdTaskBuilder()




    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type ColdTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            static member inline BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm: byref<_>,
                    task: ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : bool =

                let mutable awaiter = (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))

                let cont =
                    (ColdTaskResumptionFunc<'TOverall> (fun sm ->
                        let result = (^Awaiter: (member GetResult: unit -> 'TResult1) (awaiter))
                        (continuation result).Invoke(&sm)))

                // shortcut to continue immediately
                if (^Awaiter: (member get_IsCompleted: unit -> bool) (awaiter)) then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <- (awaiter :> ICriticalNotifyCompletion)
                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false

            [<NoEagerConstraintApplication>]
            member inline _.Bind< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    task: ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =

                ColdTaskCode<'TOverall, _> (fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the awaitable
                        let mutable awaiter = (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))

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
                        ColdTaskBuilderBase.BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall>(
                            &sm,
                            task,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            [<NoEagerConstraintApplication>]
            member inline this.Bind< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    [<InlineIfLambda>] task: unit -> ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =
                this.Bind(task (), continuation)

            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom< ^TaskLike, ^Awaiter, 'T when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'T)>
                (task: ^TaskLike)
                : ColdTaskCode<'T, 'T> =

                this.Bind(task, (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom< ^TaskLike, ^Awaiter, 'T when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'T)>
                ([<InlineIfLambda>] task: unit -> ^TaskLike)
                : ColdTaskCode<'T, 'T> =
                this.Bind(task, (fun v -> this.Return v))


            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    [<InlineIfLambda>] body: 'Resource -> ColdTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(resource, body)

    [<AutoOpen>]
    module HighPriority =
        // High priority extensions
        type Microsoft.FSharp.Control.Async with
            static member inline AwaitColdTask([<InlineIfLambda>] t: ColdTask<'T>) =
                async.Delay(fun () -> t () |> Async.AwaitTask)

            static member inline AwaitColdTask([<InlineIfLambda>] t: ColdTask) =
                async.Delay(fun () -> t () |> Async.AwaitTask)

            static member inline AsColdTask (computation : Async<'T>) : ColdTask<_> =
                fun () -> Async.StartAsTask(computation)
        type ColdTaskBuilderBase with
            static member inline BindDynamic
                (
                    sm: byref<_>,
                    task: Task<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                let mutable awaiter = task.GetAwaiter()

                let cont =
                    (ColdTaskResumptionFunc<'TOverall> (fun sm ->
                        let result = awaiter.GetResult()
                        (continuation result).Invoke(&sm)))

                // shortcut to continue immediately
                if awaiter.IsCompleted then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <- (awaiter :> ICriticalNotifyCompletion)
                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false

            member inline _.Bind
                (
                    task: Task<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =

                ColdTaskCode<'TOverall, _> (fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the task
                        let mutable awaiter = task.GetAwaiter()

                        let mutable __stack_fin = true

                        if not awaiter.IsCompleted then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = awaiter.GetResult()
                            (continuation result).Invoke(&sm)
                        else
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    else
                        ColdTaskBuilderBase.BindDynamic(&sm, task, continuation)
                //-- RESUMABLE CODE END
                )

            member inline this.Bind
                (
                    [<InlineIfLambda>] task: ColdTask<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =
                this.Bind(task (), continuation)


            member inline this.ReturnFrom(task: Task<'T>) : ColdTaskCode<'T, 'T> =
                this.Bind(task, (fun v -> this.Return v))

            member inline this.ReturnFrom([<InlineIfLambda>] task: ColdTask<'T>) : ColdTaskCode<'T, 'T> =
                this.ReturnFrom(task ())

    [<AutoOpen>]
    module MediumPriority =
        open HighPriority

        // Medium priority extensions
        type ColdTaskBuilderBase with
            member inline this.Bind
                (
                    computation: Async<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> ColdTaskCode<'TOverall, 'TResult2>)
                ) : ColdTaskCode<'TOverall, 'TResult2> =
                this.Bind(Async.AsColdTask computation, continuation)

            member inline this.ReturnFrom(computation: Async<'T>) : ColdTaskCode<'T, 'T> =
                this.ReturnFrom(Async.AsColdTask computation)

    [<AutoOpen>]
    module AsyncExtenions =


        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind
                (
                    [<InlineIfLambda>] coldTask: ColdTask<'T>,
                    [<InlineIfLambda>] binder: ('T -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitColdTask coldTask, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] coldTask: ColdTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitColdTask coldTask)

            member inline this.Bind
                (
                    [<InlineIfLambda>] coldTask: ColdTask,
                    [<InlineIfLambda>] binder: (unit -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitColdTask coldTask, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] coldTask: ColdTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitColdTask coldTask)


        type Microsoft.FSharp.Control.TaskBuilderBase with
            member inline this.Bind([<InlineIfLambda>] coldTask: ColdTask<'T>, [<InlineIfLambda>] binder: ('T -> _)) =
                this.Bind(coldTask (), binder)

            member inline this.ReturnFrom([<InlineIfLambda>] coldTask: ColdTask<'T>) = this.ReturnFrom(coldTask ())

            member inline this.Bind([<InlineIfLambda>] coldTask: ColdTask, [<InlineIfLambda>] binder: (_ -> _)) =
                this.Bind(coldTask (), binder)

            member inline this.ReturnFrom([<InlineIfLambda>] coldTask: ColdTask) = this.ReturnFrom(coldTask ())
