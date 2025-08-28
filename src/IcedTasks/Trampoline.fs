namespace IcedTasks

open System
open System.Collections.Generic
open System.Runtime.ExceptionServices
open System.Threading
open System.Runtime.CompilerServices

[<AutoOpen>]
module Assert =
    let failIfNot condition msg =
        if not condition then
            failwith $" assertion failed {msg}"

type Trampoline private () =

    let ownerThreadId = Thread.CurrentThread.ManagedThreadId

    static let holder = new ThreadLocal<_>(fun () -> Trampoline())

    let mutable pending: Action voption = ValueNone
    let mutable running = false

    let start (action: Action) =
        try
            running <- true
            action.Invoke()

            while pending.IsSome do
                let next = pending.Value
                pending <- ValueNone
                next.Invoke()
        finally
            running <- false

    let set action =
        failIfNot (Thread.CurrentThread.ManagedThreadId = ownerThreadId) "thread"
        failIfNot pending.IsNone "trampoline taken, pending is not None"

        if running then
            pending <- ValueSome action
        else
            start action

    interface ICriticalNotifyCompletion with
        member _.OnCompleted(continuation) = set continuation
        member _.UnsafeOnCompleted(continuation) = set continuation

    member this.Ref: ICriticalNotifyCompletion ref = ref this

    static member Current = holder.Value

module BindContext =
    [<Literal>]
    let bindLimit = 50

    let bindCount = new ThreadLocal<int>()
    let isBind = new ThreadLocal<bool>()

    let inline incrementBindCount () =
        bindCount.Value <-
            bindCount.Value
            + 1

        bindCount.Value % bindLimit = 0

    /// Signal to the task that it is evaluated as a bound value in a computation expression.
    /// It will use current trampoline to avoid stack overflows in recursive binds.
    let inline SetIsBind f x =
        isBind.Value <- true
        f x

    let inline CheckWhenIsBind () =
        try
            isBind.Value
            && incrementBindCount ()
        finally
            isBind.Value <- false

    let inline Check () = incrementBindCount ()

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

[<Struct>]
type DynamicState =
    | InitialYield
    | Running
    | SetResult
    | SetException of ExceptionDispatchInfo

[<Struct>]
type DynamicContinuation =
    | Stop
    | Immediate
    | Bounce
    | Await of ICriticalNotifyCompletion
