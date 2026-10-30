namespace IcedTasks

open System.Runtime.CompilerServices
open System.Runtime.ExceptionServices
open IcedTasks.TaskLike

module ExceptionCache =
    let private store = ConditionalWeakTable<exn, ExceptionDispatchInfo>()

    let private capture =
        ConditionalWeakTable<exn, ExceptionDispatchInfo>.CreateValueCallback(fun error ->
            ExceptionDispatchInfo.Capture error
        )

    /// Rethrows `error` with the dispatch info captured the first time it was seen, so a failure
    /// propagating through many frames does not grow its stack trace at every frame.
    let throwCached (error: exn) : 'T =
        store.GetValue(error, capture).Throw()
        Unchecked.defaultof<_>

    /// Gets the result of `awaiter`. If the awaited operation failed, rethrows its exception with
    /// `throwCached`, so the stack trace stays the same when each awaiting frame rethrows the failure.
    let inline getResult awaiter =
        try
            Awaiter.GetResult awaiter
        with exn ->
            throwCached exn
