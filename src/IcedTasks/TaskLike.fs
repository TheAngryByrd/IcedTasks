namespace IcedTasks

open System.Runtime.CompilerServices
open Microsoft.FSharp.Core.CompilerServices
open System.Threading

/// <namespacedoc>
///   <summary>
///     Contains core functionality for
///     <see cref='T:IcedTasks.ValueTasks'/>, <see cref='T:IcedTasks.ParallelAsync'/>,
///     <see cref='T:IcedTasks.ColdTasks'/>, <see cref='T:IcedTasks.CancellableTasks'/>,
///     <see cref='T:IcedTasks.CancellableValueTasks'/>.
///     </summary>
/// </namespacedoc>
///
/// A structure that looks like an Awaiter
type Awaiter<'Awaiter, 'TResult
    when 'Awaiter :> ICriticalNotifyCompletion
    and 'Awaiter: (member get_IsCompleted: unit -> bool)
    and 'Awaiter: (member GetResult: unit -> 'TResult)> = 'Awaiter

/// Functions for Awaiters
type Awaiter =
    /// Gets a value that indicates whether the asynchronous task has completed
    static member inline IsCompleted<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter)
        =
        awaiter.get_IsCompleted ()

    /// Ends the wait for the completion of the asynchronous task.
    static member inline GetResult<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter)
        =
        awaiter.GetResult()

    /// Schedules the continuation action that's invoked when the instance completes
    static member inline OnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter, continuation: System.Action)
        =
        awaiter.OnCompleted(continuation)

    /// Schedules the continuation action that's invoked when the instance completes.
    static member inline UnsafeOnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter, continuation: System.Action)
        =
        awaiter.UnsafeOnCompleted(continuation)

/// A structure looks like an Awaitable
type Awaitable<'Awaitable, 'Awaiter, 'TResult
    when 'Awaitable: (member GetAwaiter: unit -> Awaiter<'Awaiter, 'TResult>)> = 'Awaitable

/// Functions for Awaitables
type Awaitable =
    /// Creates an awaiter for this value.
    static member inline GetAwaiter<'Awaitable, 'Awaiter, 'TResult
        when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
        (awaitable: 'Awaitable)
        =
        awaitable.GetAwaiter()

    /// Creates an awaiter for this value.
    static member inline GetTaskAwaiter(t: System.Threading.Tasks.Task<'T>) = t.GetAwaiter()


/// <summary>Represents a builder for asynchronous methods.</summary>
type MethodBuilder =

    /// <summary>Marks the task as successfully completed.</summary>
    static member inline SetResult< ^Builder, 'TResult
        when ^Builder: (member SetResult: unit -> unit)>
        (builder: byref< ^Builder >)
        =
        // An unoptimized builder.SetResult() call completes a copy of a struct builder.
        (^Builder: (member SetResult: unit -> unit) builder)


    /// <summary>Marks the task as successfully completed.</summary>
    /// <param name="result">The result to use to complete the task.</param>
    static member inline SetResult<'Builder, 'TResult
        when 'Builder: (member SetResult: 'TResult -> unit)>
        (builder: byref<'Builder>, result: 'TResult)
        =
        builder.SetResult(result)

    /// <summary>Marks the task as failed and binds the specified exception to the task.</summary>
    /// <param name="ex">The exception to bind to the task.</param>
    static member inline SetException<'Builder when 'Builder: (member SetException: exn -> unit)>
        (builder: byref<'Builder>, ex: exn)
        =
        builder.SetException(ex)

    /// <summary>Associates the builder with the specified state machine.</summary>
    /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
    ///
    static member inline SetStateMachine<'Builder, 'TStateMachine
        when 'Builder: (member SetStateMachine: 'TStateMachine -> unit)>
        (builder: byref<'Builder>, stateMachine: 'TStateMachine)
        =
        builder.SetStateMachine(stateMachine)

    /// <summary>Begins running the builder with the associated state machine.</summary>
    /// <param name="stateMachine">The state machine instance, passed by reference.</param>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    static member inline Start<'Builder, 'TStateMachine
        when 'Builder: (member Start: byref<'TStateMachine> -> unit)>
        (builder: byref<'Builder>, stateMachine: byref<'TStateMachine>)
        =
        builder.Start(&stateMachine)

    /// <summary>Gets the task for this builder.</summary>
    /// <returns>The task for this builder.</returns>
    static member inline get_Task< ^Builder, 'TResult
        when ^Builder: (member get_Task: unit -> 'TResult)>
        (builder: byref< ^Builder >)
        =
        (^Builder: (member get_Task: unit -> 'TResult) builder)

    /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    static member inline AwaitUnsafeOnCompleted<'Builder, 'TAwaiter, 'TStateMachine
        when 'Builder: (member AwaitUnsafeOnCompleted:
            byref<'TAwaiter> * byref<'TStateMachine> -> unit)
        and 'TAwaiter :> ICriticalNotifyCompletion
        and 'TStateMachine :> IAsyncStateMachine>
        (builder: byref<'Builder>, awaiter: byref<'TAwaiter>, stateMachine: byref<'TStateMachine>)
        =
        builder.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)

    /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.
    /// On .NET 6 and later, the .NET method builders get the awaiter with its own type, so they can use their fast paths for known awaiters.
    /// Other method builders get the awaiter as an <see cref="T:System.Runtime.CompilerServices.ICriticalNotifyCompletion"/>.</summary>
    /// <param name="builder">The method builder.</param>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <typeparam name="Builder">The type of the method builder.</typeparam>
    /// <typeparam name="TOverall">The result type of the method builder.</typeparam>
    /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    static member inline internal AwaitUnsafeOnCompletedTyped<'Builder, 'TOverall, 'TAwaiter, 'TStateMachine
        when 'Builder: (member AwaitUnsafeOnCompleted:
            byref<ICriticalNotifyCompletion> * byref<'TStateMachine> -> unit)
        and 'TAwaiter :> ICriticalNotifyCompletion
        and 'TStateMachine :> IAsyncStateMachine>
        (builder: byref<'Builder>, awaiter: byref<'TAwaiter>, stateMachine: byref<'TStateMachine>)
        =
#if NET6_0_OR_GREATER
        if
            System.Type.op_Equality (typeof<'Builder>, typeof<AsyncTaskMethodBuilder<'TOverall>>)
        then
            let typed = &Unsafe.As<'Builder, AsyncTaskMethodBuilder<'TOverall>>(&builder)
            typed.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
        elif
            System.Type.op_Equality (
                typeof<'Builder>,
                typeof<AsyncValueTaskMethodBuilder<'TOverall>>
            )
        then
            let typed = &Unsafe.As<'Builder, AsyncValueTaskMethodBuilder<'TOverall>>(&builder)
            typed.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
        elif
            System.Type.op_Equality (
                typeof<'Builder>,
                typeof<PoolingAsyncValueTaskMethodBuilder<'TOverall>>
            )
        then
            let typed =
                &Unsafe.As<'Builder, PoolingAsyncValueTaskMethodBuilder<'TOverall>>(&builder)

            typed.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
        elif System.Type.op_Equality (typeof<'Builder>, typeof<AsyncTaskMethodBuilder>) then
            let typed = &Unsafe.As<'Builder, AsyncTaskMethodBuilder>(&builder)
            typed.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
        elif System.Type.op_Equality (typeof<'Builder>, typeof<AsyncValueTaskMethodBuilder>) then
            let typed = &Unsafe.As<'Builder, AsyncValueTaskMethodBuilder>(&builder)
            typed.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
        else
            let mutable awaiter = awaiter :> ICriticalNotifyCompletion
            builder.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
#else
        let mutable awaiter = awaiter :> ICriticalNotifyCompletion
        builder.AwaitUnsafeOnCompleted(&awaiter, &stateMachine)
#endif


    /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    static member inline AwaitOnCompleted<'Builder, 'TAwaiter, 'TStateMachine
        when 'Builder: (member AwaitOnCompleted: byref<'TAwaiter> * byref<'TStateMachine> -> unit)
        and 'TAwaiter :> INotifyCompletion
        and 'TStateMachine :> IAsyncStateMachine>
        (builder: byref<'Builder>, awaiter: byref<'TAwaiter>, stateMachine: byref<'TStateMachine>)
        =
        builder.AwaitOnCompleted(&awaiter, &stateMachine)
