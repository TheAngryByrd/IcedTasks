namespace IcedTasks

open System
open System.Runtime.ExceptionServices
open System.Threading
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

    static let holder = new ThreadLocal<_>(fun () -> Trampoline())

    let mutable depth = 0

    [<Literal>]
    let MaxDepth = 50

    let mutable pending: Action voption = ValueNone
    let mutable running = false

    let mutable primed = true

    let start () =
        try
            running <- true

            while pending.IsSome do
                let next = pending.Value
                pending <- ValueNone
                next.Invoke()
        finally
            running <- false

    let set action =
        pending <- ValueSome action

        if not running then
            start ()

    interface ICriticalNotifyCompletion with
        member _.OnCompleted continuation = set continuation
        member _.UnsafeOnCompleted continuation = set continuation

    member this.Ref: ICriticalNotifyCompletion ref = ref this

    static member Current = holder.Value

    member _.ShouldBounce =
        // We must check pending here because of MergeSources.
        not running
        || pending.IsNone
           && (depth <- depth + 1
               depth % MaxDepth = 0)

    // To prevent sync over async deadlocks, for example when a GetResult() is called on inner task inside a coldTask CE
    // We need to communicate to the starting cold task that it is in fact being awaited and can use the thread's trampoline
    // Any starting cold task will call this method to check if it is being awaited.
    //
    // The mechanism of a potential deadlock is as follows:
    // 1) A task continuation is executed on the trampoline
    // 2) instead of awaiting an inner task, it synchronously calls GetResult() on it (a very bad pratice)
    // 3) The inner task posts it's own continuation on the same trampoline, because we are on the same thread.
    // 4) Because the initial continuation is still running (blocked by GetResult), the trampoline is busy and never executes the inner task's continuation.
    member _.IsAwaited() =
        let wasPrimed = primed
        primed <- false
        wasPrimed

    member _.Prime() = primed <- true

module Trampoline =
    // Called inside Source builder methods to communicate to the starting task that it is let! bound (awaited),
    // therefore it can use the current trampoline.
    //
    // Because the bound task has simply a unit -> Task or CancellableToken -> Task signature, we cannot pass any such additional info directly.
    // TODO: similar mechanism can be used to communicate that the task is bound as a tail-call (ReturnFromFinal).
    let inline Allow f x =
        Trampoline.Current.Prime()
        f x

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
