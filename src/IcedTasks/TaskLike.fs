namespace IcedTasks

open System.Runtime.CompilerServices
open System.Threading.Tasks

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
        (
            awaiter: 'Awaiter,
            continuation: System.Action
        ) =
        awaiter.OnCompleted(continuation)

    /// Schedules the continuation action that's invoked when the instance completes.
    static member inline UnsafeOnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (
            awaiter: 'Awaiter,
            continuation: System.Action
        ) =
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


module AsyncMethodBuilder =

    let inline setResult<'MethodBuilder, 'TResult
        when 'MethodBuilder: (member SetResult: 'TResult -> unit)>
        (mb: 'MethodBuilder)
        (result: 'TResult)
        =
        mb.SetResult result

type AsyncMethodBuilder =

    static member inline SetResult<'MethodBuilder, 'TResult
        when 'MethodBuilder: (member SetResult: 'TResult -> unit)>
        (
            mb: 'MethodBuilder,
            result: 'TResult
        ) =
        mb.SetResult result


    static member inline SetException<'MethodBuilder
        when 'MethodBuilder: (member SetException: exn -> unit)>
        (
            mb: 'MethodBuilder,
            ex: exn
        ) =
        mb.SetException(ex)

    static member inline SetStateMachine<'MethodBuilder
        when 'MethodBuilder: (member SetStateMachine: IAsyncStateMachine -> unit)>
        (
            mb: 'MethodBuilder,
            stateMachine: IAsyncStateMachine
        ) =
        mb.SetStateMachine(stateMachine)

    static member inline Start<'TStateMachine, 'MethodBuilder
        when 'MethodBuilder: (member Start: byref<'TStateMachine> -> unit)
        and 'TStateMachine :> IAsyncStateMachine>
        (
            mb: 'MethodBuilder,
            stateMachine: byref<'TStateMachine>
        ) =
        mb.Start(&stateMachine)

    /// <summary>Gets the task for this builder.</summary>
    static member inline Task<'Awaitable, 'Awaiter, 'TResult, 'MethodBuilder
        when 'MethodBuilder: (member get_Task: unit -> Awaitable<'Awaitable, 'Awaiter, 'TResult>)>
        (mb: 'MethodBuilder)
        =
        mb.get_Task ()
