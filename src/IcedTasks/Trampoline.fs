namespace IcedTasks

open System
open System.Runtime.ExceptionServices
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices
open IcedTasks.TaskLike

type DynamicState =
    | Running
    | SetResult
    | SetException of ExceptionDispatchInfo
    | Awaiting of ICriticalNotifyCompletion
    | Bounce of DynamicState
    | Immediate of DynamicState

type Trampoline private (previous: Trampoline voption) =

    [<ThreadStatic; DefaultValue>]
    static val mutable private current: Trampoline

    let queue = Queue<Action>()

    // A trampoline that a resumption owns runs from the start and counts the MoveNext frames on it.
    // A start owns the other trampolines, and RunAsOwner sets running.
    let mutable running = previous.IsSome
    let mutable frames = if previous.IsSome then 1 else 0

    let mutable primed = false

    let mutable trusted = false

    static let runInContext =
        ContextCallback(fun state ->
            match state with
            | :? (unit -> unit) as run -> run ()
            | _ -> ()
        )

    member private _.Drain() =
        try
            while queue.Count > 0 do
                let continuation = queue.Dequeue()
                trusted <- true
                continuation.Invoke()
                trusted <- false
        finally
            trusted <- false
            running <- false

    member private this.Enqueue(continuation: Action) =
        queue.Enqueue continuation

        if not running then
            running <- true
            this.Drain()

    /// Runs `start` as the owner of this trampoline, then drains what it bounced. The owner is the
    /// outermost start on the thread, so a bounce unwinds to a shallow stack before it runs again.
    member private this.RunAsOwner(start: 'Arg -> 'Result, arg: 'Arg, prime: bool) : 'Result =
        running <- true

        try
            this.Run(start, arg, prime)
        finally
            this.Drain()

    member private _.Run(start: 'Arg -> 'Result, arg: 'Arg, prime: bool) : 'Result =
        primed <- prime

        try
            start arg
        finally
            primed <- false

    interface ICriticalNotifyCompletion with
        member this.OnCompleted continuation = this.Enqueue continuation
        member this.UnsafeOnCompleted continuation = this.Enqueue continuation

    static member Current =
        let existing = Trampoline.current

        if isNull (box existing) then
            let created = Trampoline(ValueNone)
            Trampoline.current <- created
            created
        else
            existing

    member _.IsRunning = running

    /// Marks the next resumption on this thread as one that IcedTasks itself causes: a drained bounce, or
    /// the completion of a builder task that resumes its awaiter inline.
    member _.Trust() = trusted <- true

    member _.ClearTrust() = trusted <- false

    member private _.TakeTrust() =
        let wasTrusted = trusted
        trusted <- false
        wasTrusted

    member private _.IsResumptionOwner = previous.IsSome

    member private _.EnterFrame() = frames <- frames + 1

    /// Leaves a MoveNext frame. When the resumption that created this trampoline leaves, drain what it
    /// bounced, then give the thread back to the trampoline that ran before.
    member private this.ExitFrame() =
        frames <- frames - 1

        if frames = 0 then
            frames <- 1

            try
                this.Drain()
            finally
                frames <- 0
                Trampoline.current <- previous.Value

    /// Called when a state machine starts, after it reads IsAwaited.
    static member Started() =
        let trampoline = Trampoline.current

        if not (isNull (box trampoline)) then
            trampoline.ClearTrust()

            if trampoline.IsResumptionOwner then
                trampoline.EnterFrame()

    /// Called when a state machine resumes after an await. Other code, for example
    /// TaskCompletionSource.SetResult, can resume an awaiting state machine inline while a trampoline runs
    /// further up the stack. That code can then block on the resumed task, and the drain that would complete
    /// it never runs. So such a resumption owns a fresh trampoline until its MoveNext returns.
    static member Resumed() =
        let trampoline = Trampoline.current

        if not (isNull (box trampoline)) then
            if
                not (trampoline.TakeTrust())
                && trampoline.IsRunning
            then
                Trampoline.current <- Trampoline(ValueSome trampoline)
            elif trampoline.IsResumptionOwner then
                trampoline.EnterFrame()

    /// Called when MoveNext returns, after the state machine suspends or completes. It must not read the
    /// state machine, because another thread can already run it, or a pool can already reuse it.
    /// It returns a value so that callers can ignore it: a call to a unit method at the end of MoveNext
    /// gets the IL tail. prefix, and the JIT then compiles MoveNext without tiering and dynamic PGO.
    static member Exited() : bool =
        let trampoline = Trampoline.current

        if
            not (isNull (box trampoline))
            && trampoline.IsResumptionOwner
        then
            trampoline.ExitFrame()

        true

    /// Bounce only when an owner drains this trampoline further up the stack, and only when the stack
    /// runs low. A bounce at any other depth could change code that works without one.
    member _.ShouldBounce =
        running
        && not Trampoline.HasSufficientStack

    /// The check before a nested start sets its result or exception. Setting it runs the awaiting
    /// parent inline. On .NET Framework, continuations run inline with no stack guard, so a chain of
    /// completions can overflow even when no trampoline runs. So on netstandard2.0 a completion with no
    /// trampoline running bounces too. The trampoline runs it at once on the same thread and owns the
    /// rest of the chain, where the stack check limits it. A depth count is not used, because it would
    /// also bounce where the stack is sufficient.
    member this.ShouldBounceCompletion =
#if NETSTANDARD2_0
        not running
        || not Trampoline.HasSufficientStack
#else
        this.ShouldBounce
#endif

    /// Reads and clears the flag that StartNested sets. Only a start that reads true may bounce.
    member _.IsAwaited() =
        let wasPrimed = primed
        primed <- false
        wasPrimed

    /// A bounced child is incomplete when its parent binds it. The parent then posts its resumption to the
    /// SynchronizationContext or TaskScheduler, and a blocked context never runs it. So the stack is only
    /// left without them.
    static member CanLeaveStack =
        isNull SynchronizationContext.Current
        && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)

    static member HasSufficientStack =
#if NETSTANDARD2_0
        try
            RuntimeHelpers.EnsureSufficientExecutionStack()
            true
        with :? InsufficientExecutionStackException ->
            false
#else
        RuntimeHelpers.TryEnsureSufficientExecutionStack()
#endif

    /// Runs `start` on a new thread with a fresh stack and waits for its synchronous part.
    /// The caller still sees a task that is complete or truly pending, so it can block on it.
    static member RunOnNewThread(start: unit -> 'Result) : 'Result =
        let result = ref ValueNone
        let context = ExecutionContext.Capture()

        let run () =
            result.Value <-
                try
                    ValueSome(Ok(start ()))
                with e ->
                    ValueSome(Error(ExceptionDispatchInfo.Capture e))

        let thread =
            Thread(
                (fun () ->
                    match context with
                    | null -> run ()
                    | context -> ExecutionContext.Run(context, runInContext, box run)
                ),
                16
                * 1024
                * 1024,
                IsBackground = true
            )

        thread.Start()
        thread.Join()

        match result.Value with
        | ValueSome(Ok value) -> value
        | ValueSome(Error error) ->
            error.Throw()
            Unchecked.defaultof<_>
        | ValueNone -> failwith "The start thread ended without a result"

    /// A start from user code. The caller can block on the result, so the start must not leave work in an
    /// enclosing trampoline: it owns the trampoline, or a fresh one, and drains it before it returns.
    static member StartImmediate(start: 'Arg -> 'Result, arg: 'Arg) : 'Result =
        if
            not Trampoline.HasSufficientStack
            && Trampoline.CanLeaveStack
        then
            Trampoline.RunOnNewThread(fun () -> Trampoline.StartImmediate(start, arg))
        else
            let trampoline = Trampoline.Current

            if not trampoline.IsRunning then
                trampoline.RunAsOwner(start, arg, false)
            else
                let isolated = Trampoline(ValueNone)
                Trampoline.current <- isolated

                try
                    isolated.RunAsOwner(start, arg, false)
                finally
                    Trampoline.current <- trampoline

    /// A start from a builder bind. The binder awaits the result, so the start may bounce.
    static member StartNested(start: 'Arg -> 'Result, arg: 'Arg) : 'Result =
        let trampoline = Trampoline.Current
        let prime = Trampoline.CanLeaveStack

        if trampoline.IsRunning then
            trampoline.Run(start, arg, prime)
        else
            trampoline.RunAsOwner(start, arg, prime)

/// Starts a cold value from a builder bind.
[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type INestedStart<'Arg, 'Result> =
    abstract StartNested: 'Arg -> 'Result

/// The start function of a cold builder. Calling it is a start from user code. A builder bind finds it
/// with a type test and calls StartNested. Because ColdTask and CancellableTask are function aliases,
/// the type is the only way to tell the two starts apart.
[<Sealed; System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type TrampolinedStart<'Arg, 'Result>(start: 'Arg -> 'Result) =
    inherit FSharpFunc<'Arg, 'Result>()

    override _.Invoke(arg) = Trampoline.StartImmediate(start, arg)

    interface INestedStart<'Arg, 'Result> with
        member _.StartNested(arg) = Trampoline.StartNested(start, arg)

module Trampoline =

    let inline wrap (start: 'Arg -> 'Result) : 'Arg -> 'Result =
        unbox<'Arg -> 'Result> (box (TrampolinedStart start))

    /// Starts a bound value. Only a TrampolinedStart is started as nested, so a plain function that
    /// blocks on a cold value inside cannot make that value bounce.
    let inline bind (f: 'Arg -> 'Result) (arg: 'Arg) : 'Result =
        match box f with
        | :? INestedStart<'Arg, 'Result> as nested -> nested.StartNested arg
        | _ -> f arg

module ExceptionCache =
    let store = ConditionalWeakTable<exn, ExceptionDispatchInfo>()

    let inline CaptureOrRetrieve (exn: exn) =
        match store.TryGetValue exn with
        | true, edi when edi.SourceException = exn -> edi
        | _ ->
            let edi = ExceptionDispatchInfo.Capture exn

            try
                store.Add(exn, edi)
            with _ ->
                ()

            edi

    let inline Throw (exn: exn) =
        let edi = CaptureOrRetrieve exn
        edi.Throw()
        Unchecked.defaultof<_>

    let inline GetResultOrThrow awaiter =
        try
            Awaiter.GetResult awaiter
        with exn ->
            Throw exn
