namespace IcedTasks.Tests

open System
open System.Threading.Tasks
open IcedTasks

module Task =
    /// Runs Task.Yield() `max` times. Useful for places where we want the scheduler to asynchronously yield but really fast.
    /// We run it max times to ensure it really gets async yielded.
    /// Alternative would be Task.Delay but can be slow.
    let yieldMany max =
        Task.Run<unit>(fun _ ->
            task {
                for _ = 0 to max do
                    do! Task.Yield()
            }
        )

    let runInThreadPool (f: unit -> Task<'T>) : Task<'T> = Task.Run<'T>(fun _ -> f ())

module TestHelpers =
    open System.Threading

    let makeDisposable (callback) =
        { new System.IDisposable with
            member this.Dispose() = callback ()
        }

    let makeAsyncDisposable (callback) =
        { new System.IAsyncDisposable with
            member this.DisposeAsync() = callback ()
        }

    let setSyncContext newContext =
        let oldContext = SynchronizationContext.Current
        SynchronizationContext.SetSynchronizationContext newContext
        makeDisposable (fun () -> SynchronizationContext.SetSynchronizationContext oldContext)


module Expecto =
    open Expecto

    let environVarAsBoolOrDefault varName defaultValue =
        let truthyConsts = [
            "1"
            "Y"
            "YES"
            "T"
            "TRUE"
        ]

        try
            let envvar =
                Environment.GetEnvironmentVariable varName
                |> ValueOption.ofObj
                |> ValueOption.defaultValue ""
                |> _.ToUpper()

            truthyConsts
            |> List.exists ((=) envvar)
        with _ ->
            defaultValue

    let isInCI () = environVarAsBoolOrDefault "CI" false

    let fsCheckConfig =
        if isInCI () then
            // these tests can be slow on CI so reduce the number of tests
            {
                FsCheckConfig.defaultConfig with
                    maxTest = 10
            }
        else
            FsCheckConfig.defaultConfig


module Expect =
    open Expecto

    let inline isAssignableFrom<'t> (e: exn) =
        let t1 = e.GetType()
        let t2 = typeof<'t>

        t2.IsAssignableFrom t1

    let stringContainsNot (actual: string) (expectedSubstring: string) message =
        if actual.Contains(expectedSubstring) then
            failtestf "%s. Expected %s to NOT contain substring %s" message actual expectedSubstring

    /// Expects the passed function to throw `'texn`.
    [<RequiresExplicitTypeArguments>]
    let throwsTAsync<'texn when 'texn :> exn> f message =
        async {
            let! thrown =
                async {
                    try
                        do! f ()
                        return ValueNone
                    with e ->
                        return ValueSome e
                }


            match thrown with
            | ValueSome e when isAssignableFrom<'texn> e -> ()
            | ValueSome e ->
                failtestf
                    "%s. Expected f to throw an exn of type %s, but one of type %s was thrown."
                    message
                    (typeof<'texn>.FullName)
                    (e.GetType().FullName)

            | _ -> failtestf "%s. Expected f to throw." message
        }

    [<RequiresExplicitTypeArguments>]
    let throwsTask<'texn when 'texn :> exn> f message =
        throwsTAsync<'texn>
            (f
             >> Async.AwaitTask)
            message
        |> Async.StartImmediateAsTask

    [<RequiresExplicitTypeArguments>]
    let throwsValueTask<'texn when 'texn :> exn> (f: unit -> ValueTask<unit>) message =
        throwsTAsync<'texn>
            (f
             >> Async.AwaitValueTask)
            message
        |> Async.StartImmediateAsTask
        |> ValueTask<unit>

/// Checks that a cold or cancellable value runs its body again every time it is started,
/// also when the body suspends and when an earlier start is still suspended.
/// See https://github.com/TheAngryByrd/IcedTasks/issues/65
module MultiStart =
    open System.Threading
    open Expecto

    /// Starts the value made by `makeWork` twice, the second time after the first start completed.
    /// Its body should call the function given to `makeWork`, suspend and return what it returned.
    /// `start` starts the value once; each task it returns is awaited once.
    let sequential (makeWork: (unit -> int) -> 'Work) (start: 'Work -> Task<int>) =
        async {
            let entered = ref 0
            let work = makeWork (fun () -> Interlocked.Increment(&entered.contents))

            let! first =
                start work
                |> Async.AwaitTask

            let! second =
                start work
                |> Async.AwaitTask

            Expect.equal entered.Value 2 "Each start should run the body"
            Expect.equal (first, second) (1, 2) "Each start should return its own result"
        }

    /// Starts the value made by `makeWork` twice, the second time while the first one is suspended.
    /// Its body should call the function given to `makeWork`, await the given gate and return what
    /// that function returned. `start` starts the value once; each task it returns is awaited once.
    let overlapping (makeWork: (unit -> int) -> Task -> 'Work) (start: 'Work -> Task<int>) =
        async {
            let entered = ref 0

            let gate =
                TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)

            let work =
                makeWork (fun () -> Interlocked.Increment(&entered.contents)) (gate.Task :> Task)

            let first = start work

            // A second start that waits for the first one to resume blocks until the gate opens,
            // so open it after a while to fail the test instead of hanging it.
            use watchdog =
                new Timer(
                    (fun _ ->
                        gate.TrySetResult(())
                        |> ignore
                    ),
                    null,
                    TimeSpan.FromSeconds(5.),
                    Timeout.InfiniteTimeSpan
                )

            let second = start work
            let secondReturnedWhileFirstSuspended = not gate.Task.IsCompleted

            gate.TrySetResult(())
            |> ignore

            let! first =
                first
                |> Async.AwaitTask

            let! second =
                second
                |> Async.AwaitTask

            Expect.isTrue
                secondReturnedWhileFirstSuspended
                "Starting the value again should not wait for the suspended first start"

            Expect.equal entered.Value 2 "Each start should run the body"
            Expect.equal (first, second) (1, 2) "Each start should return its own result"
        }


type Expect =

    static member CancellationRequested(operation: Async<_>) =
        Expect.throwsTAsync<OperationCanceledException>
            (fun () -> operation)
            "Should have been cancelled"

    static member CancellationRequested(operation: ValueTask<unit>) =
        Expect.CancellationRequested(Async.AwaitValueTask operation)
        |> Async.AsValueTask

    static member CancellationRequested(operation: Task<_>) =
        Expect.CancellationRequested(Async.AwaitTask operation)
        |> Async.StartImmediateAsTask

    static member CancellationRequested(operation: ColdTask<_>) =
        Expect.CancellationRequested(Async.AwaitColdTask operation)
        |> Async.AsColdTask

    static member CancellationRequested(operation: CancellableTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableTask operation)
        |> Async.AsCancellableTask


    static member CancellationRequested(operation: CancellableValueTask<_>) =
        Expect.CancellationRequested(Async.AwaitCancellableValueTask operation)
        |> Async.AsCancellableValueTask


open TimeProviderExtensions
open System.Runtime.CompilerServices

[<Extension>]
type ManualTimeProviderExtensions =

    /// Advances the fake clock, but only after the code under test has registered the timers it is waiting on.
    ///
    /// Why the wait is needed:
    /// A <c>ManualTimeProvider</c> does not move time by itself. A test moves it with <c>Advance</c>, and
    /// <c>Advance</c> fires only the timers that exist at that moment. The code under test creates its timer
    /// when it reaches <c>timeProvider.Delay(...)</c> or when a cancellation token source starts its countdown.
    /// That often happens on another thread, a moment after the test started the task. If the test calls
    /// <c>Advance</c> before the timer exists, time moves but nothing fires. The timer is then created with
    /// a due time that is still in the future, no later <c>Advance</c> comes, and the test waits forever.
    ///
    /// What this method does:
    /// <c>ActiveTimers</c> is the number of timers that are registered and have not fired yet. This method
    /// yields until that number reaches <paramref name="minActiveTimers"/>, then calls <c>Advance</c>. The
    /// wait normally ends within microseconds. If the count is not reached within 30 seconds, the test fails
    /// with a message that shows the expected and actual counts instead of hanging.
    ///
    /// How to choose <paramref name="minActiveTimers"/>:
    /// Count the timers the code under test must have created before this advance. A <c>Delay</c> is one
    /// timer. A <c>CreateCancellationTokenSource(timeout)</c> is one timer. Timers that already fired or were
    /// disposed by cancellation do not count. Pass 0 when no timer is expected and the advance is only there
    /// to move the clock. Example: a task that awaits one <c>Delay</c> under a cancellation token source with
    /// a timeout has 2 active timers until one of them fires.
    [<Extension>]
    static member ForwardTimeAsync(this: ManualTimeProvider, time: TimeSpan, minActiveTimers: int) =
        let timeout = TimeSpan.FromSeconds 30.

        backgroundTask {
            let started = Diagnostics.Stopwatch.StartNew()

            while this.ActiveTimers < minActiveTimers do
                if started.Elapsed > timeout then
                    Expecto.Tests.failtestf
                        "Expected at least %d active timers before advancing time but found %d after %O"
                        minActiveTimers
                        this.ActiveTimers
                        started.Elapsed

                do! Task.Yield()

            this.Advance(time)
        }


module CustomAwaiter =

    type CustomAwaiter<'T>(onGetResult, onIsCompleted) =

        member this.GetResult() : 'T = onGetResult ()
        member this.IsCompleted: bool = onIsCompleted ()

        interface ICriticalNotifyCompletion with
            member this.UnsafeOnCompleted(continuation) = failwith "Not Implemented"
            member this.OnCompleted(continuation: Action) : unit = failwith "Not Implemented"


module AsyncEnumerable =
    open System.Collections.Generic
    open System.Threading

    type AsyncEnumerator<'T>(current, moveNext, dispose, cancellationToken: CancellationToken) =
        member this.CancellationToken = cancellationToken

        interface IAsyncEnumerator<'T> with
            member this.Current = current ()
            member this.MoveNextAsync() = moveNext ()
            member this.DisposeAsync() = dispose ()

    type AsyncEnumerable<'T>(e: IEnumerable<'T>, beforeMoveNext: Func<_, ValueTask<unit>>) =

        let mutable lastEnumerator = None
        member this.LastEnumerator = lastEnumerator

        member this.GetAsyncEnumerator(ct) =
            let enumerator = e.GetEnumerator()

            lastEnumerator <-
                Some
                <| AsyncEnumerator(
                    (fun () -> enumerator.Current),
                    (fun () ->
                        valueTask {
                            do! beforeMoveNext.Invoke(ct)
                            return enumerator.MoveNext()
                        }
                    ),
                    (fun () ->
                        enumerator.Dispose()
                        |> ValueTask
                    ),
                    ct
                )

            lastEnumerator.Value

        interface IAsyncEnumerable<'T> with
            member this.GetAsyncEnumerator(ct: CancellationToken) = this.GetAsyncEnumerator(ct)

    let forXtoY<'T> x y beforeMoveNext =
        AsyncEnumerable([ x..y ], Func<_, _>(beforeMoveNext))

#if TEST_NETSTANDARD2_1 || TEST_NET6_0_OR_GREATER

[<AutoOpen>]
module AsyncEnumerableExtensions =
    open FSharp.Control
    open Microsoft.FSharp.Core.CompilerServices

    type TaskSeqBuilder with

        member inline _.Bind
            ([<InlineIfLambda>] task: CancellableTask<'T>, continuation: ('T -> ResumableTSC<'U>))
            =
            ResumableTSC<'U>(fun sm ->
                let mutable awaiter =
                    task sm.Data.cancellationToken
                    |> Awaitable.GetTaskAwaiter

                let mutable __stack_fin = true

                if not (Awaiter.IsCompleted awaiter) then
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_fin <- __stack_yield_fin

                if __stack_fin then
                    let result = Awaiter.GetResult awaiter
                    (continuation result).Invoke(&sm)
                else
                    sm.Data.awaiter <- awaiter
                    sm.Data.current <- ValueNone
                    false
            )

        member inline _.Bind
            (
                [<InlineIfLambda>] task: CancellableValueTask<'T>,
                continuation: ('T -> ResumableTSC<'U>)
            ) =
            ResumableTSC<'U>(fun sm ->
                let mutable awaiter =
                    task sm.Data.cancellationToken
                    |> Awaitable.GetAwaiter

                let mutable __stack_fin = true

                if not (Awaiter.IsCompleted awaiter) then
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_fin <- __stack_yield_fin

                if __stack_fin then
                    let result = Awaiter.GetResult awaiter
                    (continuation result).Invoke(&sm)
                else
                    sm.Data.awaiter <- awaiter
                    sm.Data.current <- ValueNone
                    false
            )

#endif
