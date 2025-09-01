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
#if NET6_0_OR_GREATER

/// Contains methods to build CancellableTasks using the F# computation expression syntax
[<AutoOpen>]
module CancellablePoolingValueTasks =

    open System
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// CancellationToken -> ValueTask<'T>
    type CancellableValueTask<'T> = CancellationToken -> ValueTask<'T>
    /// CancellationToken -> ValueTask
    type CancellableValueTask = CancellationToken -> ValueTask

    /// Contains methods to build CancellablePoolingValueTaskBuilder using the F# computation expression syntax
    type CancellablePoolingValueTaskBuilder() =

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
            : CancellableValueTask<'T> =

            let mutable sm = CancellableTaskBaseStateMachine<'T, _>()

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
                                Trampoline.Current.Ref,
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
                if ct.IsCancellationRequested then
                    ValueTask.FromCanceled<_>(ct)
                else
                    sm.Data.CancellationToken <- ct
                    sm.ResumptionDynamicInfo <- resumptionInfo ()
                    sm.Data.MethodBuilder <- PoolingAsyncValueTaskMethodBuilder<'T>.Create()
                    sm.Data.MethodBuilder.Start(&sm)
                    sm.Data.MethodBuilder.Task

        /// Hosts the task code in a state machine and starts the task.
        member inline _.Run(code: CancellableTaskBaseCode<'T, 'T, _>) : CancellableValueTask<'T> =
            if __useResumableCode then
                __stateMachine<CancellableTaskBaseStateMachineData<'T, _>, CancellableValueTask<'T>>
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
                                ValueTask.FromCanceled<_>(ct)
                            else
                                let mutable sm = sm
                                sm.Data.CancellationToken <- ct

                                sm.Data.MethodBuilder <-
                                    PoolingAsyncValueTaskMethodBuilder<'T>.Create()

                                sm.Data.MethodBuilder.Start(&sm)
                                sm.Data.MethodBuilder.Task
                    ))
            else
                CancellablePoolingValueTaskBuilder.RunDynamic(code)


        /// Specify a Source of CancellationToken -> ValueTask<_> on the real type to allow type inference to work
        member inline _.Source
            ([<InlineIfLambda>] x: CancellationToken -> ValueTask<_>)
            : CancellationToken -> Awaiter<ValueTaskAwaiter<_>, _> =
            fun ct ->
                BindContext.SetIsBind x ct
                |> Awaitable.GetAwaiter

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

                            (this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            ))
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

                            (this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            ))
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

                            (this.Bind(
                                left,
                                fun leftR ->
                                    this.BindReturn(right, (fun rightR -> struct (leftR, rightR)))
                            ))
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


    /// Contains the cancellableTask computation expression builder.
    [<AutoOpen>]
    module CancellableValueTaskBuilder =

        /// <summary>
        /// Builds a cancellablePoolingValueTask using computation expression syntax.
        ///
        /// This utilizes <see cref="T:System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1">System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder</see>
        /// as described in <see href="https://devblogs.microsoft.com/dotnet/async-valuetask-pooling-in-net-5/">Async ValueTask Pooling in .NET 5</see>.
        /// </summary>
        ///
        /// <remarks>
        /// Instead of needing an attribute the compiler needs to know about like in <see href="https://github.com/dotnet/runtime/issues/49903">dotnet/runtime/issues/49903</see> this is a specific computation expression.
        /// </remarks>
        let cancellablePoolingValueTask = CancellablePoolingValueTaskBuilder()


        /// <summary>
        /// Builds a cancellablePoolingValueTask using computation expression syntax.
        ///
        /// This utilizes <see cref="T:System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1">System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder</see>
        /// as described in <see href="https://devblogs.microsoft.com/dotnet/async-valuetask-pooling-in-net-5/">Async ValueTask Pooling in .NET 5</see>.
        /// </summary>
        ///
        /// <remarks>
        /// Instead of needing an attribute the compiler needs to know about like in <see href="https://github.com/dotnet/runtime/issues/49903">dotnet/runtime/issues/49903</see> this is a specific computation expression.
        /// </remarks>
        let cancelablePVTask = cancellablePoolingValueTask


    /// <exclude />
    [<AutoOpen>]
    module HighPriority =

        type AsyncEx with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableValueTask
                ([<InlineIfLambda>] t: CancellableValueTask<'T>)
                =
                asyncEx {
                    let! ct = Async.CancellationToken
                    return! t ct
                }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            /// <remarks>
            /// This is based on <see href="https://github.com/fsharp/fslang-suggestions/issues/840">Async.Await overload (esp. AwaitTask without throwing AggregateException)</see>
            /// </remarks>
            static member inline AwaitCancellableValueTask
                ([<InlineIfLambda>] t: CancellableValueTask)
                =
                asyncEx {
                    let! ct = Async.CancellationToken
                    return! t ct
                }

        type Microsoft.FSharp.Control.Async with

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableValueTask
                ([<InlineIfLambda>] t: CancellableValueTask<'T>)
                =
                async {
                    let! ct = Async.CancellationToken

                    return!
                        t ct
                        |> Async.AwaitValueTask
                }

            /// <summary>Return an asynchronous computation that will wait for the given task to complete and return
            /// its result.</summary>
            static member inline AwaitCancellableValueTask
                ([<InlineIfLambda>] t: CancellableValueTask)
                =
                async {
                    let! ct = Async.CancellationToken

                    return!
                        t ct
                        |> Async.AwaitValueTask
                }

            /// <summary>Executes a computation in the thread pool.</summary>
            static member inline AsCancellableValueTask
                (computation: Async<'T>)
                : CancellableValueTask<_> =
                fun ct ->
                    Async.StartAsTask(computation, cancellationToken = ct)
                    |> ValueTask<'T>

    /// <summary>
    /// A set of extension methods making it possible to bind against <see cref='T:IcedTasks.CancellableValueTasks.CancellableValueTask`1'/> in async computations.
    /// </summary>
    [<AutoOpen>]
    module AsyncExtensions =
        type AsyncExBuilder with

            member inline this.Source([<InlineIfLambda>] t: CancellableValueTask<'T>) : Async<'T> =
                AsyncEx.AwaitCancellableValueTask t

            member inline this.Source([<InlineIfLambda>] t: CancellableValueTask) : Async<unit> =
                AsyncEx.AwaitCancellableValueTask t

        type Microsoft.FSharp.Control.AsyncBuilder with

            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableValueTask<'T>,
                    [<InlineIfLambda>] binder: ('T -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableValueTask t, binder)

            member inline this.ReturnFrom
                ([<InlineIfLambda>] t: CancellableValueTask<'T>)
                : Async<'T> =
                this.ReturnFrom(Async.AwaitCancellableValueTask t)

            member inline this.Bind
                (
                    [<InlineIfLambda>] t: CancellableValueTask,
                    [<InlineIfLambda>] binder: (unit -> Async<'U>)
                ) : Async<'U> =
                this.Bind(Async.AwaitCancellableValueTask t, binder)

            member inline this.ReturnFrom
                ([<InlineIfLambda>] t: CancellableValueTask)
                : Async<unit> =
                this.ReturnFrom(Async.AwaitCancellableValueTask t)

    // There is explicitly no Binds for `CancellableValueTasks` in `Microsoft.FSharp.Control.TaskBuilderBase`.
    // You need to explicitly pass in a `CancellationToken`to start it, you can use `CancellationToken.None`.
    // Reason is I don't want people to assume cancellation is happening without the caller being explicit about where the CancellationToken came from.
    // Similar reasoning for `IcedTasks.ColdTasks.ColdTaskBuilderBase`.

    /// Contains a set of standard functional helper function
    [<RequireQualifiedAccess>]
    module CancellableValueTask =

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
        ///         cancellableValueTask {
        ///             let! cancellationToken = CancellableValueTask.getCancellationToken()
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

        /// <summary>Lifts an item to a CancellableValueTask.</summary>
        /// <param name="item">The item to be the result of the CancellableValueTask.</param>
        /// <returns>A CancellableValueTask with the item as the result.</returns>
        let inline singleton (item: 'item) : CancellableValueTask<'item> =
            fun (ct: CancellationToken) -> ValueTask<'item> item


        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="binder">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the binder.</returns>
        let inline bind
            ([<InlineIfLambda>] binder: 'input -> CancellableValueTask<'output>)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellablePoolingValueTask {
                let! cResult = cTask
                return! binder cResult
            }

        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="mapper">The continuation.</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the mapper wrapped in a CancellableValueTasks.</returns>
        let inline map
            ([<InlineIfLambda>] mapper: 'input -> 'output)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellablePoolingValueTask {
                let! cResult = cTask
                return mapper cResult
            }

        /// <summary>Allows chaining of CancellableValueTasks.</summary>
        /// <param name="applicable">A function wrapped in a CancellableValueTasks</param>
        /// <param name="cTask">The value.</param>
        /// <returns>The result of the applicable.</returns>
        let inline apply
            ([<InlineIfLambda>] applicable: CancellableValueTask<'input -> 'output>)
            ([<InlineIfLambda>] cTask: CancellableValueTask<'input>)
            =
            cancellablePoolingValueTask {
                let! applier = applicable
                let! cResult = cTask
                return applier cResult
            }

        /// <summary>Takes two CancellableValueTasks, starts them serially in order of left to right, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in</returns>
        let inline zip
            ([<InlineIfLambda>] left: CancellableValueTask<'left>)
            ([<InlineIfLambda>] right: CancellableValueTask<'right>)
            =
            cancellablePoolingValueTask {
                let! r1 = left
                let! r2 = right
                return r1, r2
            }

        /// <summary>Takes two CancellableValueTask, starts them concurrently, and returns a tuple of the pair.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>A tuple of the parameters passed in.</returns>
        let inline parallelZip
            ([<InlineIfLambda>] left: CancellableValueTask<'left>)
            ([<InlineIfLambda>] right: CancellableValueTask<'right>)
            =
            cancellablePoolingValueTask {
                let! ct = getCancellationToken ()
                let r1 = left ct
                let r2 = right ct
                let! r1 = r1
                let! r2 = r2
                return r1, r2
            }


        /// <summary>Coverts a CancellableValueTask to a CancellableValueTask\&lt;unit\&gt;.</summary>
        /// <param name="unitCancellableTask">The CancellableValueTask to convert.</param>
        /// <returns>a CancellableValueTask\&lt;unit\&gt;.</returns>
        let inline ofUnit ([<InlineIfLambda>] unitCancellableTask: CancellableValueTask) =
            cancellablePoolingValueTask { return! unitCancellableTask }

        /// <summary>Coverts a CancellableValueTask\&lt;_\&gt; to a CancellableValueTask.</summary>
        /// <param name="Task">The CancellableValueTask to convert.</param>
        /// <returns>a CancellableValueTask.</returns>
        let inline toUnit
            ([<InlineIfLambda>] cancellableTask: CancellableValueTask<_>)
            : CancellableValueTask =
            fun ct ->
                cancellableTask ct
                |> ValueTask.toUnit
#endif
