namespace IcedTasks

open System
open System.Runtime.CompilerServices
open System.Runtime.ExceptionServices

/// The number of failed state machines that one exception went through on this thread. A thread-static
/// count is cheap, and a failure on a shallow chain never touches the cache.
[<AbstractClass; Sealed>]
type internal ExceptionWalk =
    [<ThreadStatic; DefaultValue>]
    static val mutable private last: exn

    [<ThreadStatic; DefaultValue>]
    static val mutable private count: int

    static member Next(error: exn) =
        if obj.ReferenceEquals(ExceptionWalk.last, error) then
            ExceptionWalk.count <-
                ExceptionWalk.count
                + 1
        else
            ExceptionWalk.last <- error
            ExceptionWalk.count <- 1

        ExceptionWalk.count

    static member Continue(walked: int) = ExceptionWalk.count <- walked

    static member Reset() = ExceptionWalk.count <- 0

/// Keeps the stack trace of a failure short on deep recursion. Each awaiting frame adds a line to the
/// trace, and each Task captures the full trace, so the cost of a failure grows with the square of the
/// depth. Below `limit` the cache does nothing, so a shallow failure has its full trace.
[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
module ExceptionCache =
    let private limit = 64
    let private store = ConditionalWeakTable<exn, ExceptionDispatchInfo>()

    let private capture =
        ConditionalWeakTable<exn, ExceptionDispatchInfo>.CreateValueCallback(fun error ->
            ExceptionDispatchInfo.Capture error
        )

    /// Called once for each state machine that fails with `error`, before it sets the exception. From
    /// `limit` on, this restores the trace of the frame at `limit`. This needs a throw, so it runs only on
    /// deep chains. `isTop` is true for a start from user code. The walk ends there, so a later throw of
    /// the same exception object starts a new walk and cannot get an old trace.
    let trim (error: exn) (isTop: bool) =
        let walked = ExceptionWalk.Next error

        // The runtime can move a deep chain of continuations to another thread. The walk then continues
        // there, and the count of the new thread starts at one.
        let cached =
            walked
            >= limit
            || (walked = 1
                && fst (store.TryGetValue error))

        if cached then
            ExceptionWalk.Continue limit

            try
                store.GetValue(error, capture).Throw()
            with _ ->
                ()

        if isTop then
            ExceptionWalk.Reset()

            if cached then
                store.Remove error
                |> ignore
