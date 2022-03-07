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


[<AutoOpen>]
module CancellableTasks =

    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    type CancellableTask<'T> = CancellationToken -> Task<'T>
    type CancellableTask = CancellationToken -> Task

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type CancellableTaskStateMachineData<'T> =
        [<DefaultValue(false)>]
        val mutable CancellationToken: CancellationToken

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: AsyncTaskMethodBuilder<'T>

    and CancellableTaskStateMachine<'TOverall> = ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>
    and CancellableTaskResumptionFunc<'TOverall> = ResumptionFunc<CancellableTaskStateMachineData<'TOverall>>

    and CancellableTaskResumptionDynamicInfo<'TOverall> =
        ResumptionDynamicInfo<CancellableTaskStateMachineData<'TOverall>>

    and CancellableTaskCode<'TOverall, 'T> = ResumableCode<CancellableTaskStateMachineData<'TOverall>, 'T>

    type CancellableTaskBuilderBase() =


        member inline _.Delay
            ([<InlineIfLambda>] generator: unit -> CancellableTaskCode<'TOverall, 'T>)
            : CancellableTaskCode<'TOverall, 'T> =
            CancellableTaskCode<'TOverall, 'T>(fun sm -> (generator ()).Invoke(&sm))

        /// Used to represent no-ops like the implicit empty "else" branch of an "if" expression.
        [<DefaultValue>]
        member inline _.Zero() : CancellableTaskCode<'TOverall, unit> = ResumableCode.Zero()

        member inline _.Return(value: 'T) : CancellableTaskCode<'T, 'T> =
            CancellableTaskCode<'T, _> (fun sm ->
                sm.Data.Result <- value
                true)

        /// Chains together a step with its following step.
        /// Note that this requires that the first step has no result.
        /// This prevents constructs like `task { return 1; return 2; }`.
        member inline _.Combine
            (
                [<InlineIfLambda>] task1: CancellableTaskCode<'TOverall, unit>,
                [<InlineIfLambda>] task2: CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.Combine(task1, task2)

        /// Builds a step that executes the body while the condition predicate is true.
        member inline _.While
            (
                [<InlineIfLambda>] condition: unit -> bool,
                [<InlineIfLambda>] body: CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.While(condition, body)

        /// Wraps a step in a try/with. This catches exceptions both in the evaluation of the function
        /// to retrieve the step, and in the continuation of the step (if any).
        member inline _.TryWith
            (
                [<InlineIfLambda>] body: CancellableTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] catch: exn -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryWith(body, catch)

        /// Wraps a step in a try/finally. This catches exceptions both in the evaluation of the function
        /// to retrieve the step, and in the continuation of the step (if any).
        member inline _.TryFinally
            (
                [<InlineIfLambda>] body: CancellableTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : CancellableTaskCode<'TOverall, 'T> =
            ResumableCode.TryFinally(
                body,
                ResumableCode<_, _> (fun _sm ->
                    compensation ()
                    true)
            )

        member inline _.For
            (
                sequence: seq<'T>,
                [<InlineIfLambda>] body: 'T -> CancellableTaskCode<'TOverall, unit>
            ) : CancellableTaskCode<'TOverall, unit> =
            ResumableCode.For(sequence, body)

#if NETSTANDARD2_1
        member inline internal this.TryFinallyAsync
            (
                [<InlineIfLambda>] body: CancellableTaskCode<'TOverall, 'T>,
                [<InlineIfLambda>] compensation: unit -> ValueTask
            ) : CancellableTaskCode<'TOverall, 'T> =
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
                            CancellableTaskResumptionFunc<'TOverall> (fun sm ->
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
                [<InlineIfLambda>] body: 'Resource -> CancellableTaskCode<'TOverall, 'T>
            ) : CancellableTaskCode<'TOverall, 'T> =
            this.TryFinallyAsync(
                (fun sm -> (body resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                    else
                        ValueTask())
            )
#endif


    type CancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        // This is the dynamic implementation - this is not used
        // for statically compiled tasks.  An executor (resumptionFuncExecutor) is
        // registered with the state machine, plus the initial resumption.
        // The executor stays constant throughout the execution, it wraps each step
        // of the execution in a try/with.  The resumption is changed at each step
        // to represent the continuation of the computation.
        static member inline RunDynamic([<InlineIfLambda>] code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =

            let mutable sm = CancellableTaskStateMachine<'T>()

            let initialResumptionFunc =
                CancellableTaskResumptionFunc<'T>(fun sm -> code.Invoke(&sm))

            let resumptionInfo =
                { new CancellableTaskResumptionDynamicInfo<'T>(initialResumptionFunc) with
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

            fun (ct) ->
                if ct.IsCancellationRequested then
                    Task.FromCanceled<_>(ct)
                else
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo
                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task

        member inline _.Run([<InlineIfLambda>] code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskStateMachineData<'T>, CancellableTask<'T>>
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
                        let sm = sm
                        fun (ct) ->
                            if ct.IsCancellationRequested then
                                Task.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task))
            else
                CancellableTaskBuilder.RunDynamic(code)

    type BackgroundCancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        static member inline RunDynamic([<InlineIfLambda>] code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                CancellableTaskBuilder.RunDynamic(code)
            else
                fun (ct) -> Task.Run<'T>((fun () -> CancellableTaskBuilder.RunDynamic(code) (ct)), ct)

        //// Same as CancellableTaskBuilder.Run except the start is inside Task.Run if necessary
        member inline _.Run([<InlineIfLambda>] code: CancellableTaskCode<'T, 'T>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskStateMachineData<'T>, CancellableTask<'T>>
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
                    (AfterCode<_, CancellableTask<'T>> (fun sm ->
                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then
                            let mutable sm = sm

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    sm.Data.CancellationToken <- ct
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                        else
                            let sm = sm // copy contents of state machine so we can capture it

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    Task.Run<'T>(
                                        (fun () ->
                                            let mutable sm = sm // host local mutable copy of contents of state machine on this thread pool thread
                                            sm.Data.CancellationToken <- ct
                                            sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create ()
                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task),
                                        ct
                                    )))

            else
                BackgroundCancellableTaskBuilder.RunDynamic(code)


    [<AutoOpen>]
    module CancellableTaskBuilder =

        let cancellableTask = CancellableTaskBuilder()
        let backgroundCancellableTask = BackgroundCancellableTaskBuilder()

    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type CancellableTaskBuilderBase with

            [<NoEagerConstraintApplication>]
            static member inline BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    task: ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.CancellationToken.ThrowIfCancellationRequested()
                let mutable awaiter = (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall> (fun sm ->
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
            static member inline BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    [<InlineIfLambda>] task: CancellationToken -> ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                sm.Data.CancellationToken.ThrowIfCancellationRequested()

                let mutable awaiter =
                    (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task sm.Data.CancellationToken))

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall> (fun sm ->
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
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _> (fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.CancellationToken.ThrowIfCancellationRequested()
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
                        CancellableTaskBuilderBase.BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall>(
                            &sm,
                            task,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )


            [<NoEagerConstraintApplication>]
            member inline _.Bind< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall when ^TaskLike: (member GetAwaiter:
                unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'TResult1)>
                (
                    [<InlineIfLambda>] task: CancellationToken -> ^TaskLike,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _> (fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        sm.Data.CancellationToken.ThrowIfCancellationRequested()
                        // Get an awaiter from the awaitable
                        let mutable awaiter =
                            (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task sm.Data.CancellationToken))

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
                        CancellableTaskBuilderBase.BindDynamic< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall>(
                            &sm,
                            task,
                            continuation
                        )
                //-- RESUMABLE CODE END
                )

            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom< ^TaskLike, ^Awaiter, 'T when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'T)>
                (task: ^TaskLike)
                : CancellableTaskCode<'T, 'T> =

                this.Bind(task, (fun v -> this.Return v))


            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom< ^TaskLike, ^Awaiter, 'T when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter) and ^Awaiter :> ICriticalNotifyCompletion and ^Awaiter: (member get_IsCompleted:
                unit -> bool) and ^Awaiter: (member GetResult: unit -> 'T)>
                ([<InlineIfLambda>] task: CancellationToken -> ^TaskLike)
                : CancellableTaskCode<'T, 'T> =

                this.Bind(task, (fun v -> this.Return v))

            member inline _.Using<'Resource, 'TOverall, 'T when 'Resource :> IDisposable>
                (
                    resource: 'Resource,
                    [<InlineIfLambda>] body: 'Resource -> CancellableTaskCode<'TOverall, 'T>
                ) =
                ResumableCode.Using(resource, body)

    [<AutoOpen>]
    module HighPriority =
        type Microsoft.FSharp.Control.Async with
            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask<'T>) =
                async {
                    let! ct = Async.CancellationToken
                    return! t ct |> Async.AwaitTask
                }

            static member inline AwaitCancellableTask([<InlineIfLambda>] t: CancellableTask) =
                async {
                    let! ct = Async.CancellationToken
                    return! t ct |> Async.AwaitTask
                }

            static member inline AsCancellableTask(computation: Async<'T>) =
                fun ct -> Async.StartAsTask(computation, cancellationToken = ct)

        // High priority extensions
        type CancellableTaskBuilderBase with
            static member inline BindDynamic
                (
                    sm: byref<ResumableStateMachine<CancellableTaskStateMachineData<'TOverall>>>,
                    [<InlineIfLambda>] task: CancellableTask<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : bool =
                let mutable awaiter =
                    let ct = sm.Data.CancellationToken

                    if ct.IsCancellationRequested then
                        Task.FromCanceled<_>(ct).GetAwaiter()
                    else
                        task(ct).GetAwaiter()

                let cont =
                    (CancellableTaskResumptionFunc<'TOverall> (fun sm ->
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
                    task: CancellableTask<'TResult1>,
                    continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =

                CancellableTaskCode<'TOverall, _> (fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the task
                        let mutable awaiter =
                            let ct = sm.Data.CancellationToken

                            if ct.IsCancellationRequested then
                                Task.FromCanceled<_>(ct).GetAwaiter()
                            else
                                task(ct).GetAwaiter()

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
                        CancellableTaskBuilderBase.BindDynamic(&sm, task, continuation)
                //-- RESUMABLE CODE END
                )


            member inline this.ReturnFrom([<InlineIfLambda>] task: CancellableTask<'T>) : CancellableTaskCode<'T, 'T> =
                this.Bind((task), (fun v -> this.Return v))

    [<AutoOpen>]
    module MediumPriority =
        open HighPriority
        // Medium priority extensions
        type CancellableTaskBuilderBase with
            member inline this.Bind
                (
                    [<InlineIfLambda>] computation: ColdTask<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =
                this.Bind((fun (_: CancellationToken) -> computation ()), continuation)

            member inline this.ReturnFrom([<InlineIfLambda>] computation: ColdTask<'T>) : CancellableTaskCode<'T, 'T> =
                this.ReturnFrom(fun (_: CancellationToken) -> computation ())

            member inline this.Bind
                (
                    [<InlineIfLambda>] computation: ColdTask,
                    [<InlineIfLambda>] continuation: (unit -> CancellableTaskCode<_, _>)
                ) : CancellableTaskCode<_, _> =
                let foo = fun (_: CancellationToken) -> computation ()
                this.Bind(foo, continuation)

            member inline this.ReturnFrom([<InlineIfLambda>] computation: ColdTask) : CancellableTaskCode<_, _> =
                this.ReturnFrom(fun (_: CancellationToken) -> computation ())

            member inline this.Bind
                (
                    computation: Async<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =
                this.Bind(Async.AsCancellableTask computation, continuation)

            member inline this.ReturnFrom(computation: Async<'T>) : CancellableTaskCode<'T, 'T> =
                this.ReturnFrom(Async.AsCancellableTask computation)

            member inline this.Bind
                (
                    computation: Task<'TResult1>,
                    [<InlineIfLambda>] continuation: ('TResult1 -> CancellableTaskCode<'TOverall, 'TResult2>)
                ) : CancellableTaskCode<'TOverall, 'TResult2> =
                this.Bind((fun (_: CancellationToken) -> computation), continuation)

            member inline this.ReturnFrom(computation: Task<'T>) : CancellableTaskCode<'T, 'T> =
                this.ReturnFrom(fun (_: CancellationToken) -> computation)





    [<AutoOpen>]
    module AsyncExtenions =


        type Microsoft.FSharp.Control.AsyncBuilder with
            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableTask<'T>,
                    [<InlineIfLambda>] binder: ('T -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] t: CancellableTask<'T>) : Async<'T> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableTask,
                    [<InlineIfLambda>] binder: (unit -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableTask t, binder)

            member inline this.ReturnFrom([<InlineIfLambda>] t: CancellableTask) : Async<unit> =
                this.ReturnFrom(Async.AwaitCancellableTask t)

    // There is explicitly no Binds for `CancellableTasks` in `Microsoft.FSharp.Control.TaskBuilderBase`.
    // You need to explicitly pass in a `CancellationToken`to start it, you can use `CancellationToken.None`.
    // Reason is I don't want people to assume cancellation is happening without the caller being explicit about where the CancellationToken came from.
    // Similar reasoning for `IcedTasks.ColdTasks.ColdTaskBuilderBase`.

    [<RequireQualifiedAccess>]
    module CancellableTask =
        let getCancellationToken =
            CancellableTaskBuilder.cancellableTask.Run(
                CancellableTaskCode<_, _> (fun sm ->
                    sm.Data.Result <- sm.Data.CancellationToken
                    true)
            )
