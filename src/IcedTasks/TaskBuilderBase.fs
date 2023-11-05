namespace IcedTasks

/// Contains methods to build ValueTasks using the F# computation expression syntax
[<AutoOpen>]
module TaskBase =
    open System
    open System.Runtime.CompilerServices
    open System.Threading.Tasks
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Core.CompilerServices
    open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers
    open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
    open Microsoft.FSharp.Collections

    /// The extra data stored in ResumableStateMachine for tasks
    [<Struct; NoComparison; NoEquality>]
    type TaskBaseStateMachineData<'T, 'Builder> =

        [<DefaultValue(false)>]
        val mutable Result: 'T

        [<DefaultValue(false)>]
        val mutable MethodBuilder: 'Builder

    /// This is used by the compiler as a template for creating state machine structs
    and TaskBaseStateMachine<'TOverall, 'Builder> =
        ResumableStateMachine<TaskBaseStateMachineData<'TOverall, 'Builder>>

    /// Represents the runtime continuation of a valueTask state machine created dynamically
    and TaskBaseResumptionFunc<'TOverall, 'Builder> =
        ResumptionFunc<TaskBaseStateMachineData<'TOverall, 'Builder>>

    /// Represents the runtime continuation of a valueTask state machine created dynamically
    and TaskBaseResumptionDynamicInfo<'TOverall, 'Builder> =
        ResumptionDynamicInfo<TaskBaseStateMachineData<'TOverall, 'Builder>>

    /// A special compiler-recognised delegate type for specifying blocks of valueTask code with access to the state machine
    and TaskBaseCode<'TOverall, 'T, 'Builder> =
        ResumableCode<TaskBaseStateMachineData<'TOverall, 'Builder>, 'T>

    /// <summary>
    /// Contains methods to build TaskLikes using the F# computation expression syntax
    /// </summary>
    type TaskBuilderBase() =
        /// <summary>Creates a ValueTask that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A valueTask that runs generator</returns>
        member inline _.Delay
            ([<InlineIfLambdaAttribute>] generator: unit -> TaskBaseCode<'TOverall, 'T, 'Builder>)
            : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Delay(fun () -> generator ())

        /// <summary>Creates an ValueTask that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// valueTask { ... } computation expression syntax.
        /// </remarks>
        /// <returns>An ValueTask that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : TaskBaseCode<'TOverall, unit, 'Builder> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>An ValueTask that returns value when executed.</returns>
        member inline _.Return(value: 'T) : TaskBaseCode<'T, 'T, 'Builder> =
            TaskBaseCode<'T, 'T, 'Builder>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates an ValueTask that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>An ValueTask that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: TaskBaseCode<'TOverall, unit, 'Builder>,
                task2: TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates an ValueTask that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>An ValueTask that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                guard: unit -> bool,
                computation: TaskBaseCode<'TOverall, unit, 'Builder>
            ) : TaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates an ValueTask that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                catchHandler: exn -> TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                compensation: unit -> unit
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinally(
                computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )

        /// <summary>Creates an ValueTask that enumerates the sequence seq
        /// on demand and runs body for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of for in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// an ValueTask.  Can be seen as the body of the for expression.</param>
        ///
        /// <returns>An ValueTask that will enumerate the sequence and run body
        /// for each element.</returns>
        member inline _.For
            (
                sequence: seq<'T>,
                body: 'T -> TaskBaseCode<'TOverall, unit, 'Builder>
            ) : TaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.For(sequence, body)
#if NETSTANDARD2_1 || NET6_0_OR_GREATER
        /// <summary>Creates an ValueTask that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception.</param>
        ///
        /// <returns>An ValueTask that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal this.TryFinallyAsync
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                compensation: unit -> 'Awaitable
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinallyAsync(
                computation,
                ResumableCode<_, _>(fun sm ->

                    if __useResumableCode then
                        let mutable __stack_condition_fin = true
                        // let __stack_vtask = compensation ()
                        let mutable awaiter = compensation ()

                        if not (Awaiter.IsCompleted awaiter) then
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_condition_fin <- __stack_yield_fin

                        if __stack_condition_fin then
                            Awaiter.GetResult awaiter
                        else
                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                        __stack_condition_fin
                    else
                        // let vtask = compensation ()
                        let mutable awaiter = compensation ()

                        let cont =
                            TaskBaseResumptionFunc<'TOverall, 'Builder>(fun sm ->
                                Awaiter.GetResult awaiter
                                true
                            )

                        // shortcut to continue immediately
                        if Awaiter.IsCompleted awaiter then
                            cont.Invoke(&sm)
                        else
                            sm.ResumptionDynamicInfo.ResumptionData <-
                                (awaiter :> ICriticalNotifyCompletion)

                            sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                            false
                )
            )

        /// <summary>Creates an ValueTask that runs binder(resource).
        /// The action resource.DisposeAsync() is executed as this computation yields its result
        /// or if the ValueTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of use and use! in the
        /// valueTask { ... } computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>An ValueTask that binds and eventually disposes resource.</returns>
        ///
        member inline this.Using
            (
                resource: #IAsyncDisposable,
                binder: #IAsyncDisposable -> TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            this.TryFinallyAsync(
                (fun sm -> (binder resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        resource.DisposeAsync()
                        |> Awaitable.GetAwaiter
                    else
                        ValueTask()
                        |> Awaitable.GetAwaiter
                )
            )
#endif

    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type TaskBuilderBase with

            /// <summary>
            /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
            /// </summary>
            [<NoEagerConstraintApplication>]
            static member inline BindDynamic
                (
                    sm: byref<ResumableStateMachine<TaskBaseStateMachineData<'TOverall, 'Builder>>>,
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> TaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : bool =

                let mutable awaiter = getAwaiter

                let cont =
                    (TaskBaseResumptionFunc<'TOverall, 'Builder>(fun sm ->
                        let result = Awaiter.GetResult awaiter
                        (continuation result).Invoke(&sm)
                    ))

                // shortcut to continue immediately
                if Awaiter.IsCompleted awaiter then
                    cont.Invoke(&sm)
                else
                    sm.ResumptionDynamicInfo.ResumptionData <-
                        (awaiter :> ICriticalNotifyCompletion)

                    sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                    false

            /// <summary>Creates an ValueTask that runs computation, and when
            /// computation generates a result T, runs binder res.</summary>
            ///
            /// <remarks>A cancellation check is performed when the computation is executed.
            ///
            /// The existence of this method permits the use of let! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The computation to provide an unbound result.</param>
            /// <param name="continuation">The function to bind the result of computation.</param>
            ///
            /// <returns>An ValueTask that performs a monadic bind on the result
            /// of computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Bind
                (
                    getAwaiter: 'Awaiter,
                    continuation: ('TResult1 -> TaskBaseCode<'TOverall, 'TResult2, 'Builder>)
                ) : TaskBaseCode<'TOverall, 'TResult2, 'Builder> =

                TaskBaseCode<'TOverall, 'TResult2, 'Builder>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        // Get an awaiter from the Awaiter
                        let mutable awaiter = getAwaiter
                        let mutable __stack_fin = true

                        if not (Awaiter.IsCompleted awaiter) then
                            // This will yield with __stack_yield_fin = false
                            // This will resume with __stack_yield_fin = true
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_fin <- __stack_yield_fin

                        if __stack_fin then
                            let result = Awaiter.GetResult awaiter

                            (continuation result).Invoke(&sm)
                        else
                            MethodBuilder.AwaitUnsafeOnCompleted(
                                &sm.Data.MethodBuilder,
                                &awaiter,
                                &sm
                            )

                            false
                    else
                        TaskBuilderBase.BindDynamic(&sm, getAwaiter, continuation)
                //-- RESUMABLE CODE END
                )


            // MergeSources is used generated like:
            // builder.Bind(builder.MergeSourcesN(e1, ..., eN), (fun (pat1, ..., patN) -> ... )
            // Meaning we'd have to implement some bind in terms of `TaskBaseCode`
            // Currently easier to implement per instance of a CE
            // TODO look at something like: https://github.com/Cysharp/ValueTaskSupplement/blob/9f733d5163e048b192b0d27af28ec0eb0c9b51ec/src/ValueTaskSupplement/ValueTaskEx.WhenAll_NonGenerics.cs#L39
            // [<NoEagerConstraintApplication>]
            // member inline this.MergeSources(left: 'Awaiter1, right: 'Awaiter2) =
            //     this.Bind(left, (fun v -> this.Bind(right, (fun vr -> this.Return(v, vr)))))


            /// <summary>Delegates to the input computation.</summary>
            ///
            /// <remarks>The existence of this method permits the use of return! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="getAwaiter">The input computation.</param>
            ///
            /// <returns>The input computation.</returns>
            [<NoEagerConstraintApplication>]
            member inline this.ReturnFrom(getAwaiter: 'Awaiter) : TaskBaseCode<_, _, 'Builder> =
                this.Bind(getAwaiter, (fun v -> this.Return v))

            [<NoEagerConstraintApplication>]
            member inline this.BindReturn
                (
                    getAwaiter: 'Awaiter,
                    mapper: 'a -> 'TResult2
                ) : TaskBaseCode<'TResult2, 'TResult2, 'Builder> =
                this.Bind(getAwaiter, (fun v -> this.Return(mapper v)))


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (getAwaiter: 'Awaiter)
                : 'Awaiter =
                getAwaiter


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a ^Awaitable into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (task: 'Awaitable)
                : 'Awaiter =
                Awaitable.GetAwaiter task


            /// <summary>Creates an ValueTask that runs binder(resource).
            /// The action resource.Dispose() is executed as this computation yields its result
            /// or if the ValueTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of use and use! in the
            /// valueTask { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>An ValueTask that binds and eventually disposes resource.</returns>
            ///
            member inline _.Using
                (
                    resource: #IDisposable,
                    binder: #IDisposable -> TaskBaseCode<'TOverall, 'T, 'Builder>
                ) =
                ResumableCode.Using(resource, binder)

    /// <exclude/>
    [<AutoOpen>]
    module HighPriority =

        // High priority extensions
        type TaskBuilderBase with

            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(s: #seq<_>) : #seq<_> = s

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline _.Source(task: Task<'T>) = task.GetAwaiter()

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(computation: Async<'TResult1>) =
                this.Source(Async.StartImmediateAsTask(computation))


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a TaskAwaiter&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(awaiter: TaskAwaiter<'TResult1>) = awaiter
