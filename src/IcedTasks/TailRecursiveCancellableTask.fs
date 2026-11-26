namespace IcedTasks

#if NET6_0_OR_GREATER

/// Contains methods to build tail-recursive CancellableTasks using the F# computation expression syntax
[<AutoOpen>]
module TailRecursiveCancellableTasks =

    open System
    open System.Collections.Generic
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// <summary>
    /// The result of one step of a <c>tailRecursiveCancellableTask</c>.
    /// When <c>Next</c> is <c>ValueNone</c>, <c>Result</c> is the result of the computation.
    /// When <c>Next</c> is <c>ValueSome step</c>, the computation continues with <c>step</c>, and <c>Result</c> has no meaning.
    /// </summary>
    [<Struct; NoComparison; NoEquality>]
    type TailRecursiveStep<'T> = {
        Result: 'T
        Next: TailRecursiveStepFunc<'T> voption
    }

    /// <summary>
    /// One step of a <c>tailRecursiveCancellableTask</c>: the state machine of one computation expression body.
    /// </summary>
    and TailRecursiveStepFunc<'T> = CancellationToken -> Task<TailRecursiveStep<'T>>

    /// <summary>
    /// The method builder of <c>tailRecursiveCancellableTask</c>.
    /// It wraps an <see cref="T:System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1"/> for one step, and it holds the pending tail call.
    /// </summary>
    /// <remarks>
    /// <c>return!</c> of a CancellableTask writes the next step to <c>TailCall</c> and completes the body.
    /// <c>SetResult</c> then completes the step task with that next step instead of the result.
    /// </remarks>
    [<Struct; NoComparison; NoEquality>]
    type TailRecursiveCancellableTaskMethodBuilder<'T> =
        /// The method builder of the step task.
        [<DefaultValue(false)>]
        val mutable Inner: AsyncTaskMethodBuilder<TailRecursiveStep<'T>>

        /// The pending tail call, or <c>ValueNone</c>.
        [<DefaultValue(false)>]
        val mutable TailCall: TailRecursiveStepFunc<'T> voption

        /// Creates a method builder with no pending tail call.
        static member Create() : TailRecursiveCancellableTaskMethodBuilder<'T> =
            let mutable builder = TailRecursiveCancellableTaskMethodBuilder<'T>()
            builder.Inner <- AsyncTaskMethodBuilder<TailRecursiveStep<'T>>.Create()
            builder

        /// Begins running the state machine.
        member this.Start<'TStateMachine when 'TStateMachine :> IAsyncStateMachine>
            (stateMachine: byref<'TStateMachine>)
            =
            this.Inner.Start(&stateMachine)

        /// Associates the builder with the specified state machine.
        member this.SetStateMachine(stateMachine: IAsyncStateMachine) =
            this.Inner.SetStateMachine stateMachine

        /// Marks the step as failed and binds the specified exception to the step task.
        member this.SetException(``exception``: exn) = this.Inner.SetException ``exception``

        /// Completes the step task. When a tail call is pending, the step task gets the tail call. Otherwise it gets the result.
        member this.SetResult(result: 'T) =
            let next = this.TailCall
            this.TailCall <- ValueNone
            this.Inner.SetResult { Result = result; Next = next }

        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        member this.AwaitOnCompleted<'TAwaiter, 'TStateMachine
            when 'TAwaiter :> INotifyCompletion and 'TStateMachine :> IAsyncStateMachine>
            (awaiter: byref<'TAwaiter>, stateMachine: byref<'TStateMachine>)
            =
            this.Inner.AwaitOnCompleted(&awaiter, &stateMachine)

        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        member this.AwaitUnsafeOnCompleted<'TAwaiter, 'TStateMachine
            when 'TAwaiter :> ICriticalNotifyCompletion and 'TStateMachine :> IAsyncStateMachine>
            (awaiter: byref<'TAwaiter>, stateMachine: byref<'TStateMachine>)
            =
            this.Inner.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)

        /// Gets the step task.
        member this.Task: Task<TailRecursiveStep<'T>> = this.Inner.Task

    /// <summary>
    /// The value of a <c>tailRecursiveCancellableTask { ... }</c> expression.
    /// It is a <c>CancellableTask&lt;'T&gt;</c>: an <c>FSharpFunc</c> from <see cref="T:System.Threading.CancellationToken"/> to <see cref="T:System.Threading.Tasks.Task`1"/>.
    /// It also exposes its first step, so that <c>return!</c> in another <c>tailRecursiveCancellableTask</c> can continue with that step directly.
    /// </summary>
    /// <remarks>
    /// <c>Invoke</c> runs the chain of steps in one loop. A step that completes synchronously does not add a stack frame,
    /// and a completed step is not retained. Thus a recursive loop that tail calls itself runs in constant stack and constant heap.
    /// </remarks>
    [<Sealed>]
    type TailRecursiveCancellableTask<'T>(step: TailRecursiveStepFunc<'T>) =
        inherit FSharpFunc<CancellationToken, Task<'T>>()

        /// The first step of this computation.
        member _.Step = step

        /// Runs a chain of steps to completion.
        static member Drive
            (step: TailRecursiveStepFunc<'T>, cancellationToken: CancellationToken)
            : Task<'T> =
            task {
                let mutable current = step
                let mutable result = Unchecked.defaultof<'T>
                let mutable finished = false

                while not finished do
                    let! s = current cancellationToken

                    match s.Next with
                    | ValueNone ->
                        result <- s.Result
                        finished <- true
                    | ValueSome next -> current <- next

                return result
            }

        /// Runs this computation with the given cancellation token.
        override _.Invoke(cancellationToken: CancellationToken) : Task<'T> =
            TailRecursiveCancellableTask<'T>.Drive(step, cancellationToken)

        /// Views this object as a CancellableTask. The compiler does not coerce an FSharpFunc subclass to a function type, so this unboxes.
        member this.AsCancellableTask: CancellableTask<'T> = unbox (box this)

        /// Gets the step that <c>return!</c> continues with for a CancellableTask.
        /// For a <c>tailRecursiveCancellableTask</c> value, this is its first step. Any other CancellableTask becomes a step that awaits it.
        static member StepOf(cancellableTask: CancellableTask<'T>) : TailRecursiveStepFunc<'T> =
            match box cancellableTask with
            | :? TailRecursiveCancellableTask<'T> as tailRecursive -> tailRecursive.Step
            | _ ->
                fun ct ->
                    task {
                        let! result = cancellableTask ct
                        return { Result = result; Next = ValueNone }
                    }

    /// <summary>
    /// A CancellableTask that <c>Source</c> has marked for <c>return!</c>.
    /// <c>ReturnFrom</c> makes it a tail call. <c>Bind</c> and <c>BindReturn</c> await it.
    /// </summary>
    [<Struct; NoComparison; NoEquality>]
    type TailRecursiveCancellableTaskSource<'T> = { CancellableTask: CancellableTask<'T> }

    /// <summary>
    /// Contains methods to build tail-recursive CancellableTasks using the F# computation expression syntax.
    /// </summary>
    /// <remarks>
    /// This builder makes <c>return!</c> of a CancellableTask a tail call. The combinators that run code after a sub-computation
    /// (<c>Combine</c>, <c>While</c>, <c>For</c>, <c>TryWith</c>, <c>TryFinally</c>, <c>Using</c>) run a pending tail call to completion
    /// inside their scope, so <c>try</c>, <c>finally</c>, and <c>use</c> keep the semantics of <c>cancellableTask</c> and F# <c>async</c>.
    /// </remarks>
    type TailRecursiveCancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        /// <summary>
        /// Runs <c>code</c>, then runs a pending tail call to completion before this scope ends.
        /// </summary>
        /// <remarks>
        /// The combinators that run code after a sub-computation call this. It is generic in <c>'Builder</c> so that they keep the
        /// signatures of the base members, which they hide. <c>Run</c> fixes <c>'Builder</c> to
        /// <c>TailRecursiveCancellableTaskMethodBuilder&lt;'TOverall&gt;</c>, so the <c>Unsafe.As</c> view is exact.
        /// The view is taken in place: a helper that takes the state machine by reference reads a copy on the static path.
        /// </remarks>
        member inline this.FlushTailCall
            (code: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>)
            : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            let mutable pending = true

            CancellableTaskBaseCode<'TOverall, 'T, 'Builder>(fun sm ->
                ResumableCode
                    .Combine(
                        CancellableTaskBaseCode<'TOverall, unit, 'Builder>(fun sm ->
                            pending <- true
                            code.Invoke(&sm)
                        ),
                        ResumableCode.While(
                            (fun () -> pending),
                            CancellableTaskBaseCode<'TOverall, unit, 'Builder>(fun sm ->
                                let builder =
                                    &
                                        Unsafe.As<
                                            'Builder,
                                            TailRecursiveCancellableTaskMethodBuilder<'TOverall>
                                         >(
                                            &sm.Data.MethodBuilder
                                        )

                                match builder.TailCall with
                                | ValueNone ->
                                    pending <- false
                                    true
                                | ValueSome next ->
                                    builder.TailCall <- ValueNone

                                    this
                                        .Bind(
                                            getAwaiter =
                                                (fun ct -> Awaitable.GetTaskAwaiter(next ct)),
                                            continuation =
                                                (fun (step: TailRecursiveStep<'TOverall>) ->
                                                    CancellableTaskBaseCode<
                                                        'TOverall,
                                                        unit,
                                                        'Builder
                                                     >
                                                        (fun sm ->
                                                            match step.Next with
                                                            | ValueNone ->
                                                                sm.Data.Result <- step.Result
                                                            | ValueSome next ->
                                                                let builder =
                                                                    &
                                                                        Unsafe.As<
                                                                            'Builder,
                                                                            TailRecursiveCancellableTaskMethodBuilder<
                                                                                'TOverall
                                                                             >
                                                                         >(
                                                                            &
                                                                                sm.Data
                                                                                    .MethodBuilder
                                                                        )

                                                                builder.TailCall <-
                                                                    ValueSome next

                                                            true
                                                        )
                                                )
                                        )
                                        .Invoke(&sm)
                            )
                        )
                    )
                    .Invoke(&sm)
            )

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic
            (code: CancellableTaskBaseCode<'T, 'T, TailRecursiveCancellableTaskMethodBuilder<'T>>)
            : CancellableTask<'T> =

            let initialResumptionFunc =
                CancellableTaskBaseResumptionFunc<'T, _>(fun sm -> code.Invoke(&sm))

            let newResumptionInfo () =
                { new CancellableTaskBaseResumptionDynamicInfo<'T, _>(initialResumptionFunc) with
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

            let step (ct: CancellationToken) : Task<TailRecursiveStep<'T>> =
                if ct.IsCancellationRequested then
                    Task.FromCanceled<_>(ct)
                else
                    let mutable sm = CancellableTaskBaseStateMachine<'T, _>()
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- newResumptionInfo ()

                    sm.Data.MethodBuilder <- TailRecursiveCancellableTaskMethodBuilder<'T>.Create()

                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task

            TailRecursiveCancellableTask<'T>(step).AsCancellableTask

        /// Hosts the task code in a state machine. The result is a CancellableTask that runs the chain of tail calls in one loop.
        member inline _.Run
            (code: CancellableTaskBaseCode<'T, 'T, TailRecursiveCancellableTaskMethodBuilder<'T>>)
            : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<
                    CancellableTaskBaseStateMachineData<
                        'T,
                        TailRecursiveCancellableTaskMethodBuilder<'T>
                     >,
                    CancellableTask<'T>
                 >
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
                        let sm = sm

                        let step (ct: CancellationToken) : Task<TailRecursiveStep<'T>> =
                            if ct.IsCancellationRequested then
                                Task.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct

                                sm.Data.MethodBuilder <-
                                    TailRecursiveCancellableTaskMethodBuilder<'T>.Create()

                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task

                        TailRecursiveCancellableTask<'T>(step).AsCancellableTask
                    ))
            else
                TailRecursiveCancellableTaskBuilder.RunDynamic(code)

        /// <summary>Marks a CancellableTask for <c>return!</c>, <c>let!</c>, and <c>do!</c>.</summary>
        ///
        /// <remarks>This member is on the builder type, not in an extension module, so that it has priority over the
        /// <c>Source</c> members of <c>CancellableTaskBuilderBase</c> for a <c>CancellationToken -> Task&lt;'T&gt;</c>.</remarks>
        member inline _.Source
            ([<InlineIfLambda>] cancellableTask: CancellableTask<'T>)
            : TailRecursiveCancellableTaskSource<'T> =
            { CancellableTask = cancellableTask }

        /// <summary>Awaits a CancellableTask, then runs the continuation with its result.</summary>
        ///
        /// <remarks>The existence of this method permits the use of let! in the
        /// tailRecursiveCancellableTask { ... } computation expression syntax.</remarks>
        [<NoEagerConstraintApplication>]
        member inline this.Bind
            (
                source: TailRecursiveCancellableTaskSource<'TResult1>,
                continuation: ('TResult1 -> CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder>)
            ) : CancellableTaskBaseCode<'TOverall, 'TResult2, 'Builder> =
            this.Bind(
                getAwaiter = (fun ct -> Awaitable.GetTaskAwaiter(source.CancellableTask ct)),
                continuation = continuation
            )

        /// <summary>Awaits a CancellableTask, then returns the mapped result.</summary>
        [<NoEagerConstraintApplication>]
        member inline this.BindReturn
            (
                source: TailRecursiveCancellableTaskSource<'TResult1>,
                [<InlineIfLambda>] mapper: 'TResult1 -> 'TResult2
            ) : CancellableTaskBaseCode<'TResult2, 'TResult2, 'Builder> =
            this.Bind(source, (fun result -> this.Return(mapper result)))

        /// <summary>Tail calls a CancellableTask.</summary>
        ///
        /// <remarks>The computation does not await the CancellableTask. It gives the next step to the method builder and completes.
        /// The loop in <see cref="T:IcedTasks.TailRecursiveCancellableTasks.TailRecursiveCancellableTask`1"/> runs the next step.
        ///
        /// The existence of this method permits the use of return! in the
        /// tailRecursiveCancellableTask { ... } computation expression syntax.</remarks>
        member inline _.ReturnFrom
            (source: TailRecursiveCancellableTaskSource<'T>)
            : CancellableTaskBaseCode<'T, 'T, TailRecursiveCancellableTaskMethodBuilder<'T>> =
            CancellableTaskBaseCode<'T, 'T, TailRecursiveCancellableTaskMethodBuilder<'T>>(fun sm ->
                sm.Data.MethodBuilder.TailCall <-
                    ValueSome(TailRecursiveCancellableTask<'T>.StepOf source.CancellableTask)

                true
            )

        /// <summary>Runs task1, runs its pending tail call to completion, and then runs task2.</summary>
        member inline this.Combine
            (
                task1: CancellableTaskBaseCode<'TOverall, unit, 'Builder>,
                task2: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Combine(this.FlushTailCall task1, task2)

        /// <summary>Runs computation repeatedly until guard() becomes false. A tail call in the body runs to completion in each iteration.</summary>
        member inline this.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                computation: CancellableTaskBaseCode<'TOverall, unit, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.While(guard, this.FlushTailCall computation)

        /// <summary>Runs computation. A tail call in computation runs to completion inside the protected region, so catchHandler sees its exceptions.
        /// A tail call in catchHandler is a tail call.</summary>
        member inline this.TryWith
            (
                computation: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>,
                catchHandler: exn -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryWith(this.FlushTailCall computation, catchHandler)

        /// <summary>Runs computation, then compensation. A tail call in computation runs to completion before compensation.</summary>
        member inline this.TryFinally
            (
                computation: CancellableTaskBaseCode<'TOverall, 'T, 'Builder>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinally(
                this.FlushTailCall computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Runs body for each element of the sequence. A tail call in the body runs to completion in each iteration.</summary>
        member inline this.For
            (sequence: seq<'T>, body: 'T -> CancellableTaskBaseCode<'TOverall, unit, 'Builder>)
            : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.For(sequence, (fun element -> this.FlushTailCall(body element)))

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =

        type TailRecursiveCancellableTaskBuilder with

            /// <summary>Runs binder(resource), then disposes resource. A tail call in the body runs to completion before the dispose.</summary>
            member inline this.Using
                (
                    resource: #IDisposableNull,
                    binder: #IDisposableNull -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
                ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
                ResumableCode.Using(resource, (fun resource -> this.FlushTailCall(binder resource)))

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        type TailRecursiveCancellableTaskBuilder with

            /// <summary>Runs binder(resource), then disposes resource asynchronously. A tail call in the body runs to completion before the dispose.</summary>
            member inline this.Using
                (
                    resource: #IAsyncDisposableNull,
                    binder:
                        #IAsyncDisposableNull -> CancellableTaskBaseCode<'TOverall, 'T, 'Builder>
                ) : CancellableTaskBaseCode<'TOverall, 'T, 'Builder> =
                this.TryFinallyAsync(
                    (fun sm -> (this.FlushTailCall(binder resource)).Invoke(&sm)),
                    (fun () ->
                        if not (isNull (box resource)) then
                            resource.DisposeAsync()
                            |> Awaitable.GetAwaiter
                        else
                            ValueTask()
                            |> Awaitable.GetAwaiter
                    )
                )

            /// <summary>Runs body for each element of the async sequence. A tail call in the body runs to completion in each iteration.</summary>
            member inline this.For
                (
                    source: #IAsyncEnumerable<'T>,
                    body: 'T -> CancellableTaskBaseCode<'TOverall, unit, 'Builder>
                ) : CancellableTaskBaseCode<'TOverall, unit, 'Builder> =
                CancellableTaskBaseCode<'TOverall, unit, 'Builder>(fun sm ->
                    this
                        .Using(
                            source.GetAsyncEnumerator sm.Data.CancellationToken,
                            (fun (e: IAsyncEnumerator<'T>) ->
                                this.WhileAsync(
                                    (fun () ->
                                        __debugPoint "ForLoop.InOrToKeyword"
                                        Awaitable.GetAwaiter(e.MoveNextAsync())
                                    ),
                                    (fun sm -> (this.FlushTailCall(body e.Current)).Invoke(&sm))
                                )
                            )
                        )
                        .Invoke(&sm)
                )

    /// Contains the tailRecursiveCancellableTask computation expression builder.
    [<AutoOpen>]
    module TailRecursiveCancellableTaskBuilder =

        /// <summary>
        /// Builds a CancellableTask using computation expression syntax, where <c>return!</c> of a CancellableTask is a tail call.
        /// Use it for a recursive loop, such as a message loop, whose awaits often complete synchronously.
        /// </summary>
        let tailRecursiveCancellableTask = TailRecursiveCancellableTaskBuilder()

#endif
