namespace IcedTasks

open System
open System.Runtime.ExceptionServices
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

type Trampoline private () =

    [<ThreadStatic; DefaultValue>]
    static val mutable private current: Trampoline

    let mutable depth = 0

    [<Literal>]
    let MaxDepth = 50

    let mutable pending: Action voption = ValueNone
    let mutable running = false

    let mutable primed = false

    let start () =
        try
            running <- true

            while pending.IsSome do
                match pending with
                | ValueSome next ->
                    pending <- ValueNone
                    next.Invoke()
                | ValueNone -> ()
        finally
            running <- false

    let set action =
        pending <- ValueSome action

        if not running then
            start ()

    static let runInContext =
        ContextCallback(fun state ->
            match state with
            | :? (unit -> unit) as run -> run ()
            | _ -> ()
        )

    interface ICriticalNotifyCompletion with
        member _.OnCompleted continuation = set continuation
        member _.UnsafeOnCompleted continuation = set continuation

    member this.Ref: ICriticalNotifyCompletion ref = ref this

    static member Current =
        let existing = Trampoline.current

        if isNull (box existing) then
            let created = Trampoline()
            Trampoline.current <- created
            created
        else
            existing

    member _.IsRunning = running

    member _.ShouldBounce =
        // We must check pending here because of MergeSources.
        not running
        || pending.IsNone
           && (depth <- depth + 1
               depth % MaxDepth = 0)

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
    /// enclosing trampoline: it gets a fresh one, which drains before the start returns.
    static member StartImmediate(start: 'Arg -> 'Result, arg: 'Arg) : 'Result =
        if
            not Trampoline.HasSufficientStack
            && Trampoline.CanLeaveStack
        then
            Trampoline.RunOnNewThread(fun () -> start arg)
        else
            let previous = Trampoline.current

            if
                isNull (box previous)
                || not previous.IsRunning
            then
                start arg
            else
                Trampoline.current <- Trampoline()

                try
                    start arg
                finally
                    Trampoline.current <- previous

    /// A start from a builder bind. The binder awaits the result, so the start may bounce.
    static member StartNested(start: 'Arg -> 'Result, arg: 'Arg) : 'Result =
        let trampoline = Trampoline.Current

        if Trampoline.CanLeaveStack then
            trampoline.Prime()

        try
            start arg
        finally
            trampoline.Unprime()

    member private _.Prime() = primed <- true
    member private _.Unprime() = primed <- false

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
