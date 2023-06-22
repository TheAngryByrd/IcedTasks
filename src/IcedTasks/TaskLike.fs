namespace IcedTasks

open System.Runtime.CompilerServices

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
        (x: 'Awaiter)
        =
        x.get_IsCompleted ()

    /// Ends the wait for the completion of the asynchronous task.
    static member inline GetResult<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
        (x: 'Awaiter)
        =
        x.GetResult()


    static member inline OnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (
            x: 'Awaiter,
            continuation
        ) =
        x.OnCompleted(continuation)

    static member inline UnsafeOnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (
            x: 'Awaiter,
            continuation
        ) =
        x.UnsafeOnCompleted(continuation)

/// A structure looks like an Awaitable
type Awaitable<'Awaitable, 'Awaiter, 'TResult
    when 'Awaitable: (member GetAwaiter: unit -> Awaiter<'Awaiter, 'TResult>)> = 'Awaitable

/// Functions for Awaitables
type Awaitable =
    /// Creates an awaiter for this value.
    static member inline GetAwaiter<'Awaitable, 'Awaiter, 'TResult
        when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
        (x: 'Awaitable)
        =
        x.GetAwaiter()
