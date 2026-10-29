namespace IcedTasks

open System
open System.Runtime.ExceptionServices
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Microsoft.FSharp.Core.CompilerServices
open IcedTasks.TaskLike
open IcedTasks.Nullness

/// The queue of bounced state machines on one thread. This type holds only the state of one
/// trampoline. The Trampoline module holds the rules for when to bounce and when to drain.
[<Sealed>]
[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type Trampoline internal (owner: Owner) =

    [<ThreadStatic; DefaultValue>]
    static val mutable private onThread: Trampoline

    let queue = Queue<Action>()

    // A trampoline that a resumption owns counts the MoveNext frames on it.
    let mutable frames =
        match owner with
        | ResumptionOwned _ -> 1
        | StartOwned -> 0

    /// The trampoline of this thread, if it has one.
    static member internal OnThread =
        match box Trampoline.onThread with
        | null -> ValueNone
        | _ -> ValueSome Trampoline.onThread

    /// Makes `trampoline` the trampoline of this thread.
    static member internal Install(trampoline: Trampoline) = Trampoline.onThread <- trampoline

    /// True while an owner further up the stack drains this trampoline. A trampoline that a resumption
    /// owns runs from the start. A start sets it when it becomes the owner.
    member val internal Running =
        (match owner with
         | ResumptionOwned _ -> true
         | StartOwned -> false) with get, set

    /// Set by a nested start, and read and cleared by the state machine that it starts.
    member val internal Primed = false with get, set

    /// Set when IcedTasks itself causes the next resumption on this thread.
    member val internal Trusted = false with get, set

    member internal this.Drain() =
        try
            while queue.Count > 0 do
                let continuation = queue.Dequeue()
                this.Trusted <- true
                continuation.Invoke()
                this.Trusted <- false
        finally
            this.Trusted <- false
            this.Running <- false

    member private this.Enqueue(continuation: Action) =
        queue.Enqueue continuation

        if not this.Running then
            this.Running <- true
            this.Drain()

    interface ICriticalNotifyCompletion with
        member this.OnCompleted continuation = this.Enqueue continuation
        member this.UnsafeOnCompleted continuation = this.Enqueue continuation

    /// Enters a MoveNext frame. Only a trampoline that a resumption owns counts its frames.
    member internal _.EnterFrame() =
        match owner with
        | ResumptionOwned _ -> frames <- frames + 1
        | StartOwned -> ()

    /// Leaves a MoveNext frame. When the resumption that created this trampoline leaves, drain what it
    /// bounced, then give the thread back to the trampoline that ran before.
    member internal this.ExitFrame() =
        match owner with
        | StartOwned -> ()
        | ResumptionOwned previous ->
            frames <- frames - 1

            if frames = 0 then
                frames <- 1

                try
                    this.Drain()
                finally
                    frames <- 0
                    Trampoline.Install previous

/// The owner of a trampoline. The owner drains it.
and [<Struct; NoEquality; NoComparison>] internal Owner =
    /// The outermost start on the thread. It drains the trampoline before it returns.
    | StartOwned
    /// A resumption from code that is not IcedTasks. It drains the trampoline when its last MoveNext
    /// frame exits. Then it gives the thread back to the previous trampoline.
    | ResumptionOwned of previous: Trampoline

/// The rules for when a cold start bounces, and the hooks that the state machines call.
[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
module Trampoline =

    /// The trampoline of this thread. If the thread has no trampoline, this makes one.
    let current () =
        match Trampoline.OnThread with
        | ValueSome existing -> existing
        | ValueNone ->
            let created = Trampoline(StartOwned)
            Trampoline.Install created
            created

    let internal hasSufficientStack () =
#if NETSTANDARD2_0
        try
            RuntimeHelpers.EnsureSufficientExecutionStack()
            true
        with :? InsufficientExecutionStackException ->
            false
#else
        RuntimeHelpers.TryEnsureSufficientExecutionStack()
#endif

    /// A bounced child is incomplete when its parent binds it. The parent then posts its resumption to the
    /// SynchronizationContext or TaskScheduler, and a blocked context never runs it. So the stack is only
    /// left without them.
    let internal canLeaveStack () =
        isNull SynchronizationContext.Current
        && obj.ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default)

    /// Bounce only when an owner drains this trampoline further up the stack, and only when the stack
    /// runs low. A bounce at any other depth could change code that works without one.
    let shouldBounce (trampoline: Trampoline) =
        trampoline.Running
        && not (hasSufficientStack ())

    /// The check before a nested start sets its result or exception. Setting it runs the awaiting
    /// parent inline. On .NET Framework, continuations run inline with no stack guard, so a chain of
    /// completions can overflow even when no trampoline runs. So on netstandard2.0 a completion with no
    /// trampoline running bounces too. The trampoline runs it at once on the same thread and owns the
    /// rest of the chain, where the stack check limits it. A depth count is not used, because it would
    /// also bounce where the stack is sufficient.
    let shouldBounceCompletion (trampoline: Trampoline) =
#if NETSTANDARD2_0
        not trampoline.Running
        || not (hasSufficientStack ())
#else
        shouldBounce trampoline
#endif

    /// Marks the next resumption on this thread as one that IcedTasks itself causes: the completion of a
    /// builder task that resumes its awaiter inline.
    let trust () = (current ()).Trusted <- true

    let clearTrust () = (current ()).Trusted <- false

    /// Called when a state machine starts, as its first action. It returns true when the start may
    /// bounce: only a nested start primes the trampoline, and no user code runs between the prime and
    /// this read.
    let started () =
        let trampoline = current ()
        trampoline.Trusted <- false
        trampoline.EnterFrame()
        let primed = trampoline.Primed
        trampoline.Primed <- false
        primed

    /// Called when a state machine resumes after an await. Other code, for example
    /// TaskCompletionSource.SetResult, can resume an awaiting state machine inline while a trampoline runs
    /// further up the stack. That code can then block on the resumed task, and the drain that would complete
    /// it never runs. So such a resumption owns a fresh trampoline until its MoveNext returns.
    let resumed () =
        match Trampoline.OnThread with
        | ValueNone -> ()
        | ValueSome trampoline ->
            let trusted = trampoline.Trusted
            trampoline.Trusted <- false

            if
                not trusted
                && trampoline.Running
            then
                Trampoline.Install(Trampoline(ResumptionOwned trampoline))
            else
                trampoline.EnterFrame()

    /// Called when MoveNext returns, after the state machine suspends or completes. It must not read the
    /// state machine, because another thread can already run it, or a pool can already reuse it.
    /// It returns a value so that callers can ignore it: a call to a unit function at the end of MoveNext
    /// gets the IL tail. prefix, and the JIT then compiles MoveNext without tiering and dynamic PGO.
    let exited () : bool =
        match Trampoline.OnThread with
        | ValueNone -> ()
        | ValueSome trampoline -> trampoline.ExitFrame()

        true

    let private runInContext =
        ContextCallback(fun state ->
            match state with
            | :? (unit -> unit) as run -> run ()
            | _ -> ()
        )

    /// Runs `start` on a new thread with a fresh stack and waits for its synchronous part.
    /// The caller still sees a task that is complete or truly pending, so it can block on it.
    let internal runOnNewThread (start: unit -> 'Result) : 'Result =
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

/// The start function of a cold builder. Calling it is a start from user code. A builder bind finds it
/// with a type test and calls StartNested. Because ColdTask and CancellableTask are function aliases,
/// the type is the only way to tell the two starts apart. A builder's Run derives from it with an object
/// expression that holds the state machine, so one object is both the function and the start.
[<AbstractClass;
  System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type TrampolinedStart<'Arg, 'Result>() =
    inherit FSharpFunc<'Arg, 'Result>()

    /// Starts the state machine, without the trampoline around it.
    abstract Start: 'Arg -> 'Result

    member private this.Run(trampoline: Trampoline, arg: 'Arg, prime: bool) =
        trampoline.Primed <- prime

        try
            this.Start arg
        finally
            trampoline.Primed <- false

    /// Runs the start as the owner of the trampoline, then drains what it bounced. The owner is the
    /// outermost start on the thread, so a bounce unwinds to a shallow stack before it runs again.
    member private this.RunAsOwner(trampoline: Trampoline, arg: 'Arg, prime: bool) =
        trampoline.Running <- true

        try
            this.Run(trampoline, arg, prime)
        finally
            trampoline.Drain()

    /// A start from user code. The caller can block on the result, so the start must not leave work in an
    /// enclosing trampoline: it owns the trampoline, or a fresh one, and drains it before it returns.
    override this.Invoke(arg) =
        if
            not (Trampoline.hasSufficientStack ())
            && Trampoline.canLeaveStack ()
        then
            Trampoline.runOnNewThread (fun () -> this.Invoke arg)
        else
            let trampoline = Trampoline.current ()

            if not trampoline.Running then
                this.RunAsOwner(trampoline, arg, false)
            else
                let isolated = Trampoline(StartOwned)
                Trampoline.Install isolated

                try
                    this.RunAsOwner(isolated, arg, false)
                finally
                    Trampoline.Install trampoline

    /// A start from a builder bind. The binder awaits the result, so the start may bounce.
    member this.StartNested(arg: 'Arg) : 'Result =
        let trampoline = Trampoline.current ()
        let prime = Trampoline.canLeaveStack ()

        if trampoline.Running then
            this.Run(trampoline, arg, prime)
        else
            this.RunAsOwner(trampoline, arg, prime)

/// A TrampolinedStart around a function, for the starts that do not hold a state machine.
[<Sealed; System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type FuncStart<'Arg, 'Result>(start: 'Arg -> 'Result) =
    inherit TrampolinedStart<'Arg, 'Result>()

    override _.Start(arg) = start arg

[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
module TrampolinedStart =

    /// Puts a TrampolinedStart around a function.
    let inline ofFunc (start: 'Arg -> 'Result) : 'Arg -> 'Result =
        unbox<'Arg -> 'Result> (box (FuncStart start))

    /// Gives a TrampolinedStart the type of the function that it is.
    let inline asFunc (start: TrampolinedStart<'Arg, 'Result>) : 'Arg -> 'Result =
        unbox<'Arg -> 'Result> (box start)

    /// Starts a bound value. Only a TrampolinedStart is started as nested, so a plain function that
    /// blocks on a cold value inside cannot make that value bounce.
    let inline bind (f: 'Arg -> 'Result) (arg: 'Arg) : 'Result =
        match box f with
        | :? TrampolinedStart<'Arg, 'Result> as start -> start.StartNested arg
        | _ -> f arg

/// The resumption info of a cold builder's dynamic path. It runs the code, calls the trampoline hooks,
/// and bounces where the static path bounces. A builder gives only the calls to its method builder.
[<AbstractClass;
  System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type TrampolinedResumptionInfo<'Data>(initial: ResumptionFunc<'Data>) =
    inherit ResumptionDynamicInfo<'Data>(initial)

    let mutable started = false
    let mutable bounceAllowed = false
    let mutable completing = false
    let mutable error: ExceptionNull = null

    /// Calls AwaitOnCompleted on the method builder.
    abstract Await:
        sm: byref<ResumableStateMachine<'Data>> * awaiter: ICriticalNotifyCompletion -> unit

    /// Calls SetResult on the method builder.
    abstract SetResult: sm: byref<ResumableStateMachine<'Data>> -> unit

    /// Calls SetException on the method builder.
    abstract SetException: sm: byref<ResumableStateMachine<'Data>> * error: exn -> unit

    member private this.Complete(sm: byref<ResumableStateMachine<'Data>>) =
        Trampoline.trust ()

        match error with
        | null -> this.SetResult(&sm)
        | error -> this.SetException(&sm, error)

        Trampoline.clearTrust ()

    /// Runs the code to its next await or to its end. An await stores its awaiter in ResumptionData.
    member private this.Step(sm: byref<ResumableStateMachine<'Data>>) =
        let mutable finished = false

        try
            this.ResumptionData <- null
            finished <- this.ResumptionFunc.Invoke(&sm)
        with exn ->
            error <- exn

        if
            finished
            || not (isNull error)
        then
            completing <- true

            if
                bounceAllowed
                && Trampoline.shouldBounceCompletion (Trampoline.current ())
            then
                this.Await(&sm, Trampoline.current ())
            else
                this.Complete(&sm)
        else
            match this.ResumptionData with
            | :? ICriticalNotifyCompletion as awaiter -> this.Await(&sm, awaiter)
            | _ -> ()

    override this.MoveNext(sm) =
        let bounce =
            if started then
                Trampoline.resumed ()
                false
            else
                started <- true
                bounceAllowed <- Trampoline.started ()

                bounceAllowed
                && Trampoline.shouldBounce (Trampoline.current ())

        try
            if bounce then this.Await(&sm, Trampoline.current ())
            elif completing then this.Complete(&sm)
            else this.Step(&sm)
        finally
            Trampoline.exited ()
            |> ignore

module ExceptionCache =
    let private store = ConditionalWeakTable<exn, ExceptionDispatchInfo>()

    let private capture =
        ConditionalWeakTable<exn, ExceptionDispatchInfo>.CreateValueCallback(fun error -> ExceptionDispatchInfo.Capture error)

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
