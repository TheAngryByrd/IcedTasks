namespace IcedTasks.TaskBase
open IcedTasks.Nullness
open IcedTasks.TaskLike
/// Contains methods to build Tasks using the F# computation expression syntax
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
    open System.Collections.Generic
    open System.Threading

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

    /// Represents the runtime continuation of a task state machine created dynamically
    and TaskBaseResumptionFunc<'TOverall, 'Builder> =
        ResumptionFunc<TaskBaseStateMachineData<'TOverall, 'Builder>>

    /// Represents the runtime continuation of a task state machine created dynamically
    and TaskBaseResumptionDynamicInfo<'TOverall, 'Builder> =
        ResumptionDynamicInfo<TaskBaseStateMachineData<'TOverall, 'Builder>>

    /// A special compiler-recognised delegate type for specifying blocks of task code with access to the state machine
    and TaskBaseCode<'TOverall, 'T, 'Builder> =
        ResumableCode<TaskBaseStateMachineData<'TOverall, 'Builder>, 'T>

    /// <summary>
    /// Contains methods to build TaskLikes using the F# computation expression syntax
    /// </summary>
    type TaskBuilderBase() =
        /// <summary>Creates a ValueTask that runs generator</summary>
        /// <param name="generator">The function to run</param>
        /// <returns>A task that runs generator</returns>
        member inline _.Delay(generator: unit -> ResumableCode<'TOverall, 'T>) =
            ResumableCode.Delay(generator)

        /// <summary>Creates a Task that just returns ().</summary>
        /// <remarks>
        /// The existence of this method permits the use of empty else branches in the
        /// task { ... } computation expression syntax.
        /// </remarks>
        /// <returns>a Task that returns ().</returns>
        [<DefaultValue>]
        member inline _.Zero() : TaskBaseCode<'TOverall, unit, 'Builder> = ResumableCode.Zero()

        /// <summary>Creates an computation that returns the result v.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of return in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="value">The value to return from the computation.</param>
        ///
        /// <returns>a Task that returns value when executed.</returns>
        member inline _.Return(value: 'T) : TaskBaseCode<'T, 'T, 'Builder> =
            TaskBaseCode<'T, 'T, 'Builder>(fun sm ->
                sm.Data.Result <- value
                true
            )

        /// <summary>Creates a Task that first runs task1
        /// and then runs computation2, returning the result of computation2.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of expression sequencing in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="task1">The first part of the sequenced computation.</param>
        /// <param name="task2">The second part of the sequenced computation.</param>
        ///
        /// <returns>a Task that runs both of the computations sequentially.</returns>
        member inline _.Combine
            (
                task1: TaskBaseCode<'TOverall, unit, 'Builder>,
                task2: TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.Combine(task1, task2)

        /// <summary>Creates a Task that runs computation repeatedly
        /// until guard() becomes false.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of while in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="guard">The function to determine when to stop executing computation.</param>
        /// <param name="computation">The function to be executed.  Equivalent to the body
        /// of a while expression.</param>
        ///
        /// <returns>a Task that behaves similarly to a while loop when run.</returns>
        member inline _.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                computation: TaskBaseCode<'TOverall, unit, 'Builder>
            ) : TaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.While(guard, computation)

        /// <summary>Creates a Task that runs computation and returns its result.
        /// If an exception happens then catchHandler(exn) is called and the resulting computation executed instead.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/with in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="catchHandler">The function to run when computation throws an exception.</param>
        ///
        /// <returns>a Task that executes computation and calls catchHandler if an
        /// exception is thrown.</returns>
        member inline _.TryWith
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                catchHandler: exn -> TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryWith(computation, catchHandler)

        /// <summary>Creates a Task that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception (including cancellation).</param>
        ///
        /// <returns>a Task that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline _.TryFinally
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinally(
                computation,
                ResumableCode<_, _>(fun _ ->
                    compensation ()
                    true
                )
            )


        /// <summary>
        /// The entry point for the dynamic implementation of the corresponding operation. Do not use directly, only used when executing quotations that involve tasks or other reflective execution of F# code.
        /// </summary>
        [<NoEagerConstraintApplication>]
        static member inline BindDynamic
            (
                sm: byref<ResumableStateMachine<TaskBaseStateMachineData<'TOverall, 'Builder>>>,
                awaiter: 'Awaiter,
                continuation: ('TResult1 -> TaskBaseCode<'TOverall, 'TResult2, 'Builder>)
            ) : bool =

            let cont =
                TaskBaseResumptionFunc<'TOverall, 'Builder>(fun sm ->
                    let result = Awaiter.GetResult awaiter
                    (continuation result).Invoke(&sm)
                )

            // shortcut to continue immediately
            if Awaiter.IsCompleted awaiter then
                cont.Invoke(&sm)
            else
                sm.ResumptionDynamicInfo.ResumptionData <- (awaiter :> ICriticalNotifyCompletion)

                sm.ResumptionDynamicInfo.ResumptionFunc <- cont
                false

        /// <summary>Creates a Task that runs computation, and when
        /// computation generates a result T, runs binder res.</summary>
        ///
        /// <remarks>A cancellation check is performed when the computation is executed.
        ///
        /// The existence of this method permits the use of let! in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="getAwaiter">The computation to provide an unbound result.</param>
        /// <param name="continuation">The function to bind the result of computation.</param>
        ///
        /// <returns>a Task that performs a monadic bind on the result
        /// of computation.</returns>
        [<NoEagerConstraintApplication>]
        member inline _.Bind
            (
                awaiter: 'Awaiter,
                continuation: ('TResult1 -> TaskBaseCode<'TOverall, 'TResult2, 'Builder>)
            ) : TaskBaseCode<'TOverall, 'TResult2, 'Builder> =

            TaskBaseCode<'TOverall, 'TResult2, 'Builder>(fun sm ->
                if __useResumableCode then
                    //-- RESUMABLE CODE START

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

                        let mutable awaiter = awaiter :> ICriticalNotifyCompletion

                        MethodBuilder.AwaitUnsafeOnCompleted(&sm.Data.MethodBuilder, &awaiter, &sm)

                        false
                else
                    TaskBuilderBase.BindDynamic(&sm, awaiter, continuation)
            //-- RESUMABLE CODE END
            )

        /// <summary>Delegates to the input computation.</summary>
        ///
        /// <remarks>The existence of this method permits the use of return! in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="getAwaiter">The input computation.</param>
        ///
        /// <returns>The input computation.</returns>
        [<NoEagerConstraintApplication>]
        member inline this.ReturnFrom(awaiter: 'Awaiter) : TaskBaseCode<_, _, 'Builder> =
            this.Bind(awaiter, (fun v -> this.Return v))


        [<NoEagerConstraintApplication>]
        member inline this.BindReturn
            (awaiter: 'Awaiter, [<InlineIfLambda>] mapper: 'a -> 'TResult)
            =
            this.Bind(awaiter, (fun v -> this.Return(mapper v)))


        /// <summary>Creates a Task that runs computation. The action compensation is executed
        /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
        /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of try/finally in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="computation">The input computation.</param>
        /// <param name="compensation">The action to be run after computation completes or raises an
        /// exception.</param>
        ///
        /// <returns>a Task that executes computation and compensation afterwards or
        /// when an exception is raised.</returns>
        member inline internal x.TryFinallyAsync
            (
                computation: TaskBaseCode<'TOverall, 'T, 'Builder>,
                [<InlineIfLambda>] compensation: unit -> 'Awaitable
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            ResumableCode.TryFinallyAsync(
                computation,
                ResumableCode<_, _>(fun sm -> x.Bind(compensation (), x.Zero).Invoke(&sm))
            )

        /// <summary>Creates a Task that runs binder(resource).
        /// The action resource.DisposeAsync() is executed as this computation yields its result
        /// or if the ValueTask exits by an exception or by cancellation.</summary>
        ///
        /// <remarks>
        ///
        /// The existence of this method permits the use of use and use! in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="resource">The resource to be used and disposed.</param>
        /// <param name="binder">The function that takes the resource and returns an asynchronous
        /// computation.</param>
        ///
        /// <returns>a Task that binds and eventually disposes resource.</returns>
        ///
        member inline this.Using
            (
                resource: #IAsyncDisposableNull,
                binder: #IAsyncDisposableNull -> TaskBaseCode<'TOverall, 'T, 'Builder>
            ) : TaskBaseCode<'TOverall, 'T, 'Builder> =
            this.TryFinallyAsync(
                (fun sm -> (binder resource).Invoke(&sm)),
                (fun () ->
                    if not (isNull (box resource)) then
                        Awaitable.GetAwaiter(resource.DisposeAsync())
                    else
                        Awaitable.GetAwaiter(ValueTask())
                )
            )

        member inline internal x.WhileAsync
            ( // Fantomas ignore
                [<InlineIfLambda>] condition: unit -> 'Awaitable,
                body: TaskBaseCode<_, unit, 'Builder>
            ) : TaskBaseCode<_, _, 'Builder> =
            let mutable condition_res = true

            x.While(
                (fun () -> condition_res),
                ResumableCode<_, _>(fun sm ->
                    x
                        .Bind(
                            condition (),
                            (fun result ->
                                ResumableCode<_, _>(fun sm ->
                                    condition_res <- result
                                    if condition_res then body.Invoke(&sm) else true
                                )
                            )
                        )
                        .Invoke(&sm)
                )
            )

        member inline this.For
            (source: #IAsyncEnumerable<'T>, body: 'T -> TaskBaseCode<_, unit, 'Builder>)
            : TaskBaseCode<_, _, 'Builder> =

            this.Using(
                source.GetAsyncEnumerator CancellationToken.None,
                (fun (e: IAsyncEnumerator<'T>) ->
                    this.WhileAsync(
                        (fun () ->
                            __debugPoint "ForLoop.InOrToKeyword"
                            Awaitable.GetAwaiter(e.MoveNextAsync())
                        ),
                        (fun sm -> (body e.Current).Invoke(&sm))
                    )
                )
            )

        /// <summary>Creates a Task that enumerates the sequence seq
        /// on demand and runs body for each element.</summary>
        ///
        /// <remarks>A cancellation check is performed on each iteration of the loop.
        ///
        /// The existence of this method permits the use of for in the
        /// task { ... } computation expression syntax.</remarks>
        ///
        /// <param name="sequence">The sequence to enumerate.</param>
        /// <param name="body">A function to take an item from the sequence and create
        /// a Task.  Can be seen as the body of the for expression.</param>
        ///
        /// <returns>a Task that will enumerate the sequence and run body
        /// for each element.</returns>
        member inline _.For
            (sequence: seq<'T>, body: 'T -> TaskBaseCode<'TOverall, unit, 'Builder>)
            : TaskBaseCode<'TOverall, unit, 'Builder> =
            ResumableCode.For(sequence, body)


    /// <exclude/>
    [<AutoOpen>]
    module LowPriority =
        // Low priority extensions
        type TaskBuilderBase with


            /// <summary>Allows the computation expression to turn other types into CancellationToken -> 'Awaiter</summary>
            ///
            /// <remarks>This is the identify function.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaiter<'Awaiter, 'TResult1>>
                (awaiter: 'Awaiter)
                : 'Awaiter =
                awaiter


            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a ^Awaitable into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            [<NoEagerConstraintApplication>]
            member inline _.Source<'Awaitable, 'TResult1, 'TResult2, 'Awaiter, 'TOverall
                when Awaitable<'Awaitable, 'Awaiter, 'TResult1>>
                (awaitable: 'Awaitable)
                : 'Awaiter =
                Awaitable.GetAwaiter awaitable


            /// <summary>Creates a Task that runs binder(resource).
            /// The action resource.Dispose() is executed as this computation yields its result
            /// or if the ValueTask exits by an exception or by cancellation.</summary>
            ///
            /// <remarks>
            ///
            /// The existence of this method permits the use of use and use! in the
            /// task { ... } computation expression syntax.</remarks>
            ///
            /// <param name="resource">The resource to be used and disposed.</param>
            /// <param name="binder">The function that takes the resource and returns an asynchronous
            /// computation.</param>
            ///
            /// <returns>a Task that binds and eventually disposes resource.</returns>
            ///
            member inline _.Using
                (
                    resource: #IDisposableNull,
                    binder: #IDisposableNull -> TaskBaseCode<'TOverall, 'T, 'Builder>
                ) =
                ResumableCode.Using(resource, binder)

            /// <summary>Allows the computation expression to turn other types into other types</summary>
            ///
            /// <remarks>This is the identify function for For binds.</remarks>
            ///
            /// <returns>IEnumerable</returns>
            member inline _.Source(enumerable: #seq<_>) : #seq<_> = enumerable

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
            member inline _.Source(asyncEnumerable: #IAsyncEnumerable<_>) = asyncEnumerable

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Task&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline _.Source(task: Task<'T>) = Awaitable.GetTaskAwaiter task

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a Async&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(async: Async<'TResult1>) =
                this.Source(Async.StartImmediateAsTask(async))

            /// <summary>Allows the computation expression to turn other types into 'Awaiter</summary>
            ///
            /// <remarks>This turns a TaskAwaiter&lt;'T&gt; into a 'Awaiter.</remarks>
            ///
            /// <returns>'Awaiter</returns>
            member inline this.Source(taskAwaiter: TaskAwaiter<'TResult1>) = taskAwaiter
