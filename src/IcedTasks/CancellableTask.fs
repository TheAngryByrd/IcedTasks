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


/// Contains methods to build CancellableTasks using the F# computation expression syntax
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

    /// CancellationToken -> Task<'T>
    type CancellableTask<'T> = CancellationToken -> Task<'T>
    /// CancellationToken -> Task
    type CancellableTask = CancellationToken -> Task

    /// Contains methods to build CancellableTasks using the F# computation expression syntax
    type CancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

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
            (code: CancellableTaskBaseCode<'T, 'T, _>)
            : CancellableTask<'T> =

            let initialResumptionFunc =
                CancellableTaskBaseResumptionFunc<'T, _>(fun sm -> code.Invoke(&sm))

            let resumptionInfo () =
                let mutable state = InitialYield

                { new CancellableTaskBaseResumptionDynamicInfo<'T, _>(initialResumptionFunc) with
                    member info.MoveNext(sm) =
                        let current = state
                        let mutable continuation = Stop

                        match current with
                        | InitialYield ->
                            state <- Running

                            continuation <-
                                if BindContext.CheckWhenIsBind() then Bounce else Immediate
                        | Running ->
                            try
                                let step = info.ResumptionFunc.Invoke(&sm)

                                if step then
                                    state <- SetResult

                                    continuation <-
                                        if BindContext.Check() then Bounce else Immediate
                                else
                                    continuation <-
                                        Await(downcast sm.ResumptionDynamicInfo.ResumptionData)
                            with exn ->
                                state <- SetException(ExceptionCache.CaptureOrRetrieve exn)
                                continuation <- if BindContext.Check() then Bounce else Immediate
                        | SetResult ->
                            MethodBuilder.SetResult(&sm.Data.MethodBuilder, sm.Data.Result)
                        | SetException edi ->
                            MethodBuilder.SetException(&sm.Data.MethodBuilder, edi.SourceException)

                        let continuation = continuation

                        match continuation with
                        | Stop -> ()
                        | Immediate -> info.MoveNext(&sm)
                        | Bounce ->
                            MethodBuilder.AwaitOnCompleted(
                                &sm.Data.MethodBuilder,
                                Trampoline.AwaiterRef,
                                &sm
                            )
                        | Await awaiter ->
                            let mutable awaiter = awaiter

                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                    member _.SetStateMachine(sm, state) =
                        MethodBuilder.SetStateMachine(&sm.Data.MethodBuilder, state)
                }

            fun (ct) ->
                let mutable sm = CancellableTaskBaseStateMachine<'T, _>()

                if ct.IsCancellationRequested then
                    Task.FromCanceled<_>(ct)
                else
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo ()
                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task


        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: CancellableTaskBaseCode<'T, 'T, _>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskBaseStateMachineData<'T, _>, CancellableTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        __resumeAt sm.ResumptionPoint
                        let mutable error = ValueNone

                        let __stack_go1 = yieldOnBindLimitWhenIsBind().Invoke(&sm)

                        if __stack_go1 then
                            try
                                let __stack_code_fin = code.Invoke(&sm)

                                if __stack_code_fin then
                                    let __stack_go2 = yieldOnBindLimit().Invoke(&sm)

                                    if __stack_go2 then
                                        MethodBuilder.SetResult(
                                            &sm.Data.MethodBuilder,
                                            sm.Data.Result
                                        )
                            with exn ->
                                error <-
                                    ValueSome
                                    <| ExceptionCache.CaptureOrRetrieve exn

                            if error.IsSome then
                                let __stack_go2 = yieldOnBindLimit().Invoke(&sm)

                                if __stack_go2 then
                                    MethodBuilder.SetException(
                                        &sm.Data.MethodBuilder,
                                        error.Value.SourceException
                                    )
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        MethodBuilder.SetStateMachine(&sm.Data.MethodBuilder, state)
                    ))
                    (AfterCode<_, _>(fun sm ->
                        let sm = sm

                        fun (ct) ->
                            if ct.IsCancellationRequested then
                                Task.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct
                                sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                CancellableTaskBuilder.RunDynamic(code)

        /// Specify a Source of CancellationToken -> Task<_> on the real type to allow type inference to work
        member inline _.Source
            ([<InlineIfLambda>] x: CancellationToken -> Task<_>)
            : CancellationToken -> Awaiter<TaskAwaiter<_>, _> =
            fun ct ->
                BindContext.SetIsBind()
                Awaitable.GetTaskAwaiter(x ct)

        [<NoEagerConstraintApplication>]
        member inline this.MergeSources
            (
                [<InlineIfLambda>] left: CancellationToken -> 'Awaiter1,
                [<InlineIfLambda>] right: CancellationToken -> 'Awaiter2
            ) =
            this.Source(
                this.Run(
                    this.Bind(
                        (fun ct -> this.Source(ValueTask<_> ct)),
                        fun ct ->
                            let left = left ct
                            let right = right ct

                            this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            )
                    )
                )
            )

        [<NoEagerConstraintApplication>]
        member inline this.MergeSources
            (left: 'Awaiter1, [<InlineIfLambda>] right: CancellationToken -> 'Awaiter2)
            =
            this.Source(
                this.Run(
                    this.Bind(
                        (fun ct -> this.Source(ValueTask<_> ct)),
                        fun ct ->
                            let right = right ct

                            this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            )
                    )
                )
            )

        [<NoEagerConstraintApplication>]
        member inline this.MergeSources
            ([<InlineIfLambda>] left: CancellationToken -> 'Awaiter1, right: 'Awaiter2)
            =

            this.Source(
                this.Run(
                    this.Bind(
                        (fun ct -> this.Source(ValueTask<_> ct)),
                        fun ct ->
                            let left = left ct

                            this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            )
                    )
                )
            )

        [<NoEagerConstraintApplication>]
        member inline this.MergeSources(left: 'Awaiter1, right: 'Awaiter2) =
            this.Source(
                this.Run(
                    this.Bind(
                        left,
                        fun leftR -> this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                    )
                )
            )


    /// Contains methods to build CancellableTasks using the F# computation expression syntax
    type BackgroundCancellableTaskBuilder() =

        inherit CancellableTaskBuilderBase()

        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        static member inline RunDynamic
            (code: CancellableTaskBaseCode<'T, 'T, _>)
            : CancellableTask<'T> =
            // backgroundTask { .. } escapes to a background thread where necessary
            // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
            if
                isNull SynchronizationContext.Current
                && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
            then
                CancellableTaskBuilder.RunDynamic(code)
            else
                fun (ct) ->
                    Task.Run<'T>((fun () -> CancellableTaskBuilder.RunDynamic (code) (ct)), ct)

        /// <summary>
        /// Hosts the task code in a state machine and starts the task, executing in the ThreadPool using Task.Run
        /// </summary>
        member inline _.Run(code: CancellableTaskBaseCode<'T, 'T, _>) : CancellableTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskBaseStateMachineData<'T, _>, CancellableTask<'T>>
                    (MoveNextMethodImpl<_>(fun sm ->
                        __resumeAt sm.ResumptionPoint
                        let mutable error = ValueNone

                        let __stack_go1 = yieldOnBindLimitWhenIsBind().Invoke(&sm)

                        if __stack_go1 then
                            try
                                let __stack_code_fin = code.Invoke(&sm)

                                if __stack_code_fin then
                                    let __stack_go2 = yieldOnBindLimit().Invoke(&sm)

                                    if __stack_go2 then
                                        MethodBuilder.SetResult(
                                            &sm.Data.MethodBuilder,
                                            sm.Data.Result
                                        )
                            with exn ->
                                error <-
                                    ValueSome
                                    <| ExceptionCache.CaptureOrRetrieve exn

                            if error.IsSome then
                                let __stack_go2 = yieldOnBindLimit().Invoke(&sm)

                                if __stack_go2 then
                                    MethodBuilder.SetException(
                                        &sm.Data.MethodBuilder,
                                        error.Value.SourceException
                                    )
                    ))
                    (SetStateMachineMethodImpl<_>(fun sm state ->
                        MethodBuilder.SetStateMachine(&sm.Data.MethodBuilder, state)
                    ))
                    (AfterCode<_, CancellableTask<'T>>(fun sm ->
                        let sm = sm

                        // backgroundTask { .. } escapes to a background thread where necessary
                        // See spec of ConfigureAwait(false) at https://devblogs.microsoft.com/dotnet/configureawait-faq/
                        if
                            isNull SynchronizationContext.Current
                            && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)
                        then

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    let mutable sm = sm
                                    sm.Data.CancellationToken <- ct
                                    sm.Data.MethodBuilder <- AsyncTaskMethodBuilder<'T>.Create()
                                    sm.Data.MethodBuilder.Start(&sm)
                                    sm.Data.MethodBuilder.Task
                        else

                            fun (ct) ->
                                if ct.IsCancellationRequested then
                                    Task.FromCanceled<_>(ct)
                                else
                                    Task.Run<'T>(
                                        (fun () ->
                                            let mutable sm = sm
                                            sm.Data.CancellationToken <- ct

                                            sm.Data.MethodBuilder <-
                                                AsyncTaskMethodBuilder<'T>.Create()

                                            sm.Data.MethodBuilder.Start(&sm)
                                            sm.Data.MethodBuilder.Task
                                        ),
                                        ct
                                    )
                    ))

            else
                BackgroundCancellableTaskBuilder.RunDynamic(code)

    /// Contains the cancellableTask computation expression builder.
    [<AutoOpen>]
    module CancellableTaskBuilder =

        /// <summary>
        /// Builds a cancellableTask using computation expression syntax.
        /// </summary>
        let cancellableTask = CancellableTaskBuilder()

        /// <summary>
        /// Builds a cancellableTask using computation expression syntax which switches to execute on a background thread if not already doing so.
        /// </summary>
        let backgroundCancellableTask = BackgroundCancellableTaskBuilder()


    /// <summary>
    /// A set of extension methods making it possible to bind against <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in async computations.
    /// </summary>
    [<AutoOpen>]
    module AsyncExtensions =

        type AsyncExBuilder with

            member inline this.Source([<InlineIfLambda>] t: CancellableTask<'T>) : Async<'T> =
                AsyncEx.AwaitCancellableTask t

            member inline this.Source([<InlineIfLambda>] t: CancellableTask) : Async<unit> =
                AsyncEx.AwaitCancellableTask t

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

    // Contains a set of standard functional helper function

    [<RequireQualifiedAccess>]
    module CancellableTask =

        /// <summary>Gets the default cancellation token for executing computations.</summary>
        ///
        /// <returns>The default CancellationToken.</returns>
        ///
        /// <category index="3">Cancellation and Exceptions</category>
        ///
        /// <example id="default-cancellation-token-1">
        /// <code lang="F#">
        /// use tokenSource = new CancellationTokenSource()
        /// let primes = [ 2; 3; 5; 7; 11 ]
        /// for i in primes do
        ///     let computation =
        ///         cancellableTask {
        ///             let! cancellationToken = CancellableTask.getCancellationToken()
        ///             do! Task.Delay(i * 1000, cancellationToken)
        ///             printfn $"{i}"
        ///         }
        ///     computation tokenSource.Token |> ignore
        /// Thread.Sleep(6000)
        /// tokenSource.Cancel()
        /// printfn "Tasks Finished"
        /// </code>
        /// This will print "2" 2 seconds from start, "3" 3 seconds from start, "5" 5 seconds from start, cease computation and then
        /// followed by "Tasks Finished".
        /// </example>
        let inline getCancellationToken () =
            fun (ct: CancellationToken) -> ValueTask<CancellationToken> ct

        /// <summary>Lifts an item to a CancellableTask.</summary>
        /// <param name="item">The item to be the result of the CancellableTask.</param>
        /// <returns>A CancellableTask with the item as the result.</returns>
        let inline singleton (item: 'item) : CancellableTask<'item> = fun _ -> Task.FromResult(item)


        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> CancellableTask<'output>)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a CancellableTasks.</returns>
        let inline map
            ([<InlineIfLambda>] mapper: 'input -> 'output)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of CancellableTasks.</summary>
        /// <param name="applicable">A function wrapped in a CancellableTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply<'input, 'output>
            ([<InlineIfLambda>] applicable: CancellableTask<'input -> 'output>)
            ([<InlineIfLambda>] cTask: CancellableTask<'input>)
            =
            cancellableTask {
                let! (applier: 'input -> 'output) = applicable
                let! (cResult: 'input) = cTask
                return applier cResult
            }

        /// <summary>Takes two CancellableTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip
            ([<InlineIfLambda>] left: CancellableTask<'left>)
            ([<InlineIfLambda>] right: CancellableTask<'right>)
            =
            cancellableTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        /// <summary>Takes two CancellableTask, starts them concurrently, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in.</returns>
        let inline parallelZip
            ([<InlineIfLambda>] left: CancellableTask<'left>)
            ([<InlineIfLambda>] right: CancellableTask<'right>)
            =
            cancellableTask {
                let! ct = getCancellationToken ()
                let r1 = left ct
                let r2 = right ct
                let! r1 = r1
                let! r2 = r2
                return r1, r2
            }


        /// <summary>Creates a task that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in an enumerable collection have completed.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks" /> argument was <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="tasks" /> collection contained a <see langword="null" /> task.</exception>
        let inline whenAll (tasks: CancellableTask<_> seq) =
            cancellableTask {
                let! ct = getCancellationToken ()

                let! results =
                    tasks
                    |> Seq.map (fun t -> t ct)
                    |> Task.WhenAll

                return results
            }

        /// <summary>Creates a task that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> in an enumerable collection have completed.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of tasks to run concurrently.</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks" /> argument was <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="tasks" /> collection contained a <see langword="null" /> task.</exception>
        let inline whenAllThrottled (maxDegreeOfParallelism: int) (tasks: CancellableTask<_> seq) =
            cancellableTask {
                let! ct = getCancellationToken ()

                use semaphore =
                    new SemaphoreSlim(
                        initialCount = maxDegreeOfParallelism,
                        maxCount = maxDegreeOfParallelism
                    )

                let! results =
                    tasks
                    |> Seq.map (fun t ->
                        task {
                            do! semaphore.WaitAsync ct

                            try
                                return! t ct
                            finally
                                semaphore.Release()
                                |> ignore

                        }
                    )
                    |> Task.WhenAll

                return results
            }

        /// <summary>Creates a <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/> that will complete when all of the <see cref='T:IcedTasks.CancellableTasks.CancellableTask`1'/>s in an enumerable collection have completed sequentially.</summary>
        /// <param name="tasks">The tasks to wait on for completion</param>
        /// <returns>A CancellableTask that represents the completion of all of the supplied tasks.</returns>
        let inline sequential (tasks: CancellableTask<'a> seq) =
            cancellableTask {
                let mutable results = ArrayCollector<'a>()

                for t in tasks do
                    let! result = t
                    results.Add result

                return results.Close()
            }


        /// <summary>Coverts a CancellableTask to a CancellableTask\&lt;unit\&gt;.</summary>
        /// <param name="unitCancellableTask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitCancellableTask: CancellableTask) =
            cancellableTask { return! unitCancellableTask }

        /// <summary>Coverts a CancellableTask\&lt;_\&gt; to a CancellableTask.</summary>
        /// <param name="ctask">The CancellableTask to convert.</param>
        /// <returns>a CancellableTask.</returns>
        let inline toUnit ([<InlineIfLambda>] ctask: CancellableTask<_>) : CancellableTask =
            fun ct -> ctask ct
