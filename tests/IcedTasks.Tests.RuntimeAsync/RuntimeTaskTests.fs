namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks
open IcedTasks.Polyfill.RuntimeTask

module RuntimeTaskTests =
    open System.Collections.Generic
    open System.Reflection
    open System.Runtime.CompilerServices
    open System.Threading
    open System.Threading.Tasks.Sources


    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func0ThreadPool (x: int) : Task<int> =
        Task.Run<int>(fun () ->
            task {
                do! Task.Yield()
                return x + 1
            }
        )

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func1ThreadPool (x: int) : Task<int> =
        Task.Run<int>(fun () ->
            task {
                do! Task.Yield()
                failwith "boom"
                return! func0ThreadPool x
            }
        )

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func2ThreadPool (x: int) : Task<int> =
        Task.Run<int>(fun () ->
            task {
                let y = x + 1
                do! Task.Yield()
                return! func1ThreadPool y
            }
        )

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func3ThreadPool (x: int) : Task<int> =
        Task.Run<int>(fun () ->
            task {
                let y = x + 1
                do! Task.Yield()
                return! func2ThreadPool y
            }
        )

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func0Yield (x: int) : Task<int> =
        task {
            do! Task.Yield()
            return x + 1
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func1Yield (x: int) : Task<int> =
        task {
            do! Task.Yield()
            failwith "boom"
            return! func0Yield x
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func2Yield (x: int) : Task<int> =
        task {
            let y = x + 1
            do! Task.Yield()
            return! func1Yield y
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func3Yield (x: int) : Task<int> =
        task {
            let y = x + 1
            do! Task.Yield()
            return! func2Yield y
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func0Sync (x: int) : Task<int> =
        task {
            do! Task.Yield()
            return x + 1
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func1Sync (x: int) : Task<int> =
        task {
            failwith "boom"
            return! func0Sync x
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func2Sync (x: int) : Task<int> =
        task {
            let y = x + 1
            return! func1Sync y
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func3Sync (x: int) : Task<int> =
        task {
            let y = x + 1
            return! func2Sync y
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func1Use (x: int) : Task<int> =
        task {
            do! Task.Yield()
            failwith "boom"
            return x
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func2Use (x: int) : Task<int> =
        task {
            use _ =
                TestHelpers.makeAsyncDisposable (fun () ->
                    task { do! Task.Yield() }
                    |> ValueTask
                )

            do! Task.Yield()
            return! func1Use x
        }

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let func3Use (x: int) : Task<int> =
        task {
            do! Task.Yield()
            return! func2Use x
        }

    let expectStackTrace (start: unit -> Task<int>) =
        async {
            let data = 42
            let mutable exn = None

            let! actual =
                task {
                    try
                        let! _ = start ()
                        ()
                    with e ->
                        exn <- Some e

                    return data
                }
                |> Async.AwaitTask

            Expect.equal actual data "TryWith should work"
            Expect.isSome exn "Exception should have been caught"

            exn
            |> Option.iter (fun e ->
                let stackTrace = string e.StackTrace
                Expect.equal e.Message "boom" "Exception message should match"
                Expect.stringContains stackTrace "func3" ""
                Expect.stringContains stackTrace "func2" ""
                Expect.stringContains stackTrace "func1" ""
                Expect.stringContainsNot stackTrace "func0" ""
            )
        }


    let builderTests =
        testList "TaskBuilder" [
            testList "TryWith" [
                testCaseAsync "try with syntax"
                <| async {
                    let data = 42

                    let! actual =
                        task {
                            let data = data

                            try
                                ()
                            with _ ->
                                ()

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "TryWith should work"
                }

                testList "StackTracePreservation" [
                    testCaseAsync "threadpool"
                    <| expectStackTrace (fun () -> func3ThreadPool 3)

                    testCaseAsync "yield"
                    <| expectStackTrace (fun () -> func3Yield 3)

                    testCaseAsync "sync"
                    <| expectStackTrace (fun () -> func3Sync 3)

                    testCaseAsync "through use of an IAsyncDisposable"
                    <| async {
                        let! frames =
                            task {
                                try
                                    let! _ = func3Use 3
                                    return ""
                                with e ->
                                    return string e.StackTrace
                            }
                            |> Async.AwaitTask

                        Expect.stringContains
                            frames
                            "func1Use"
                            "The throw site should stay in the stack trace"

                        Expect.stringContains frames "func2Use" ""
                        Expect.stringContains frames "func3Use" ""
                    }
                ]
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let mutable wasFinalized = false

                    let! actual =
                        task {
                            let data = data

                            try
                                ()
                            finally
                                wasFinalized <- true

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "TryFinally should work"
                    Expect.isTrue wasFinalized "Finally block should have run"
                }

                testCaseAsync "try finally when the body throws after a suspension"
                <| async {
                    let mutable wasFinalized = false

                    let! message =
                        task {
                            try
                                do!
                                    task {
                                        try
                                            do! Task.Yield()
                                            failwith "body"
                                        finally
                                            wasFinalized <- true
                                    }

                                return None
                            with e ->
                                return Some e.Message
                        }
                        |> Async.AwaitTask

                    Expect.equal message (Some "body") "The body exception should reach the caller"
                    Expect.isTrue wasFinalized "Finally block should have run"
                }
            ]

            testList "Using" [
                testCaseAsync "use IAsyncDisposable propagate exception"
                <| async {
                    let doDispose () =
                        task {
                            do! Task.Yield()
                            failwith "boom"
                        }
                        |> ValueTask

                    let! message =
                        task {
                            try
                                do!
                                    task {
                                        use d = TestHelpers.makeAsyncDisposable (doDispose)
                                        return ()
                                    }

                                return None
                            with e ->
                                return Some e.Message
                        }
                        |> Async.AwaitTask

                    Expect.equal
                        message
                        (Some "boom")
                        "The DisposeAsync exception should reach the caller"
                }

            ]
        ]


    let isRuntimeAsync (method: MethodBase) =
        method.MethodImplementationFlags.HasFlag MethodImplAttributes.Async

    let expectRuntimeAsync (method: MethodBase) =
        Expect.isTrue
            (isRuntimeAsync method)
            $"Expected %s{string method.DeclaringType}.%s{method.Name} to have MethodImplAttributes.Async"

    let runtimeAsyncTests =
        testList "RuntimeAsync" [
            testCaseAsync "task body runs in a runtime-async method"
            <| async {
                let! method =
                    task {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "backgroundTask body runs in a runtime-async method"
            <| async {
                let! method =
                    backgroundTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "valueTask body runs in a runtime-async method"
            <| async {
                let! method =
                    valueTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }
                    |> Async.AwaitValueTask

                expectRuntimeAsync method
            }

            testCaseAsync "AsyncLocal value set in the body does not flow to the caller"
            <| async {
                let local = AsyncLocal<int>()
                local.Value <- 1

                let! inner =
                    task {
                        local.Value <- 2
                        do! Task.Yield()
                        return local.Value
                    }
                    |> Async.AwaitTask

                Expect.equal
                    inner
                    2
                    "The body should see its own AsyncLocal value after a suspension"

                Expect.equal local.Value 1 "The caller should keep its AsyncLocal value"
            }
        ]

    let valueTaskTests =
        testList "ValueTaskBuilder" [
            testCaseAsync "Simple Return"
            <| async {
                let! actual =
                    valueTask { return "lol" }
                    |> Async.AwaitValueTask

                Expect.equal actual "lol" "Should be able to Return value"
            }

            testCaseAsync "Can Bind ValueTask<T>, Task<T>, Async<T>, and an awaitable"
            <| async {
                let! actual =
                    valueTask {
                        let! a = ValueTask<int>(1)
                        let! b = Task.FromResult 2
                        let! c = async { return 3 }
                        do! Task.Yield()
                        do! ValueTask.CompletedTask
                        return a + b + c
                    }
                    |> Async.AwaitValueTask

                Expect.equal actual 6 "Should bind each source"
            }

            testCaseAsync "Can ReturnFrom ValueTask<T>"
            <| async {
                let! actual =
                    valueTask { return! ValueTask<string>("lol") }
                    |> Async.AwaitValueTask

                Expect.equal actual "lol" "Should be able to Return! value"
            }

            testCaseAsync "and! combines two Task operands"
            <| async {
                let left = TaskCompletionSource<int>()
                let right = TaskCompletionSource<int>()

                let result =
                    valueTask {
                        let! a = left.Task
                        and! b = right.Task
                        return a + b
                    }

                right.SetResult 2
                left.SetResult 1

                let! actual =
                    result
                    |> Async.AwaitValueTask

                Expect.equal actual 3 "Should combine both results"
            }

            testCaseAsync "use IAsyncDisposable async"
            <| async {
                let mutable disposed = false

                let! actual =
                    valueTask {
                        use _ =
                            { new IAsyncDisposable with
                                member _.DisposeAsync() =
                                    task {
                                        do! Task.Yield()
                                        disposed <- true
                                    }
                                    |> ValueTask
                            }

                        do! Task.Yield()
                        return 42
                    }
                    |> Async.AwaitValueTask

                Expect.equal actual 42 "Should return the body result"
                Expect.isTrue disposed "Should await DisposeAsync"
            }
        ]

    let syncContextTests =
        testList "SynchronizationContext" [
            testCase "task resumes on the caller SynchronizationContext"
            <| fun () ->
                let mutable posted = false

                let syncContext =
                    { new SynchronizationContext() with
                        member _.Post(d, state) =
                            posted <- true
                            d.Invoke(state)
                    }

                use _sync = TestHelpers.setSyncContext syncContext

                let t =
                    task {
                        do! Task.Yield()
                        return SynchronizationContext.Current
                    }

                Expect.equal t.Result syncContext "Should resume on the caller context"
                Expect.isTrue posted "Should post the continuation to the caller context"

            testCase "backgroundTask escapes the caller SynchronizationContext"
            <| fun () ->
                let mutable posted = false

                let syncContext =
                    { new SynchronizationContext() with
                        member _.Post(d, state) =
                            posted <- true
                            d.Invoke(state)
                    }

                use _sync = TestHelpers.setSyncContext syncContext

                let t =
                    backgroundTask {
                        let onThreadPool = Thread.CurrentThread.IsThreadPoolThread
                        do! Task.Yield()
                        return onThreadPool
                    }

                Expect.isTrue t.Result "Should start on a thread pool thread"
                Expect.isFalse posted "Should not post to the caller context"

            testCase "backgroundTask starts on the caller thread when it is on the thread pool"
            <| fun () ->
                let outer =
                    Task.Run(fun () ->
                        let callerThread = Thread.CurrentThread.ManagedThreadId

                        let inner = backgroundTask { return Thread.CurrentThread.ManagedThreadId }

                        struct (callerThread, inner.Result)
                    )

                let struct (callerThread, innerThread) = outer.Result

                Expect.equal
                    innerThread
                    callerThread
                    "Should start synchronously on the caller thread"
        ]

    type CompletedAwaiter(value: int) =
        let mutable registrations = 0
        let mutable reads = 0
        member _.Registrations = registrations
        member _.Reads = reads
        member _.IsCompleted = true

        member _.GetResult() =
            reads <- reads + 1
            value

        interface ICriticalNotifyCompletion with
            member _.OnCompleted(continuation) =
                registrations <-
                    registrations
                    + 1

                continuation.Invoke()

            member _.UnsafeOnCompleted(continuation) =
                registrations <-
                    registrations
                    + 1

                continuation.Invoke()

    let startOrderTests =
        testList "Start order" [
            testCaseTask "and! starts Async operands before it awaits them"
            <| fun () ->
                task {
                    let rightStarted =
                        TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)

                    let left =
                        async {
                            do!
                                rightStarted.Task
                                |> Async.AwaitTask

                            return 1
                        }

                    let right =
                        async {
                            rightStarted.SetResult()
                            return 2
                        }

                    let result =
                        task {
                            let! a = left
                            and! b = right
                            return a + b
                        }

                    let! actual = result.WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.equal
                        actual
                        3
                        "The right operand must start before the left one completes"
                }

            testCaseTask "and! starts cold task operands before it awaits them"
            <| fun () ->
                task {
                    let rightStarted =
                        TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)

                    let left () =
                        task {
                            do! rightStarted.Task
                            return 1
                        }

                    let right () =
                        task {
                            rightStarted.SetResult()
                            return 2
                        }

                    let result =
                        task {
                            let! a = left
                            and! b = right
                            return a + b
                        }

                    let! actual = result.WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.equal
                        actual
                        3
                        "The right operand must start before the left one completes"
                }

            testCaseTask "and! gives the left exception when both operands fail"
            <| fun () ->
                task {
                    let result =
                        task {
                            let! a = Task.FromException<int>(ArgumentException "left")
                            and! b = Task.FromException<int>(InvalidOperationException "right")
                            return a + b
                        }

                    let! message =
                        task {
                            try
                                let! _ = result
                                return None
                            with e ->
                                return Some e.Message
                        }

                    Expect.equal message (Some "left") "The left operand is awaited first"
                }

            testCase "a completed awaiter is read without a continuation registration"
            <| fun () ->
                let awaiter = CompletedAwaiter 2

                let result =
                    task {
                        let! value = awaiter
                        return value
                    }

                Expect.equal
                    (result.WaitAsync(TimeSpan.FromSeconds 10.).Result)
                    2
                    "Should return the awaiter result"

                Expect.equal
                    awaiter.Registrations
                    0
                    "The builder must not register a continuation on a completed awaiter"

                Expect.equal awaiter.Reads 1 "The builder must read the result once"
        ]

    let disposeOnCancelAndOrderTests =
        let canceledBody () =
            raise (OperationCanceledException "body")

        let expectCanceledAfterOneDispose (start: IAsyncDisposable -> Task) =
            task {
                let mutable disposeCalls = 0

                let resource =
                    TestHelpers.makeAsyncDisposable (fun () ->
                        Interlocked.Increment(&disposeCalls)
                        |> ignore

                        ValueTask()
                    )

                let result = start resource

                let! _ =
                    result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                Expect.equal
                    result.Status
                    TaskStatus.Canceled
                    "A canceled body gives a canceled task"

                Expect.equal
                    disposeCalls
                    1
                    "The builder must dispose once when the body is canceled"
            }

        testList "Canceled body and loop order" [
            testCaseTask "use disposes once when the body throws OperationCanceledException"
            <| fun () ->
                expectCanceledAfterOneDispose (fun resource ->
                    task {
                        use _ = resource
                        do! Task.Yield()
                        canceledBody ()
                    }
                    :> Task
                )

            testCaseTask
                "for over an IAsyncEnumerable disposes once when the body throws OperationCanceledException"
            <| fun () ->
                expectCanceledAfterOneDispose (fun resource ->
                    let items =
                        AsyncDisposalOutcome.singleItemAsyncEnumerable (fun () ->
                            resource.DisposeAsync()
                        )

                    task {
                        for _ in items do
                            do! Task.Yield()
                            canceledBody ()
                    }
                    :> Task
                )

            testCaseTask "for over a seq gives each element to the body in order"
            <| fun () ->
                task {
                    let seen = ResizeArray<int>()

                    do!
                        task {
                            for item in
                                [
                                    3
                                    1
                                    2
                                ] do
                                seen.Add item
                        }

                    Expect.equal
                        (List.ofSeq seen)
                        [
                            3
                            1
                            2
                        ]
                        "The body should see each element in order"
                }

            testCaseTask "for over an IAsyncEnumerable gives each element to the body in order"
            <| fun () ->
                task {
                    let seen = ResizeArray<int>()

                    let items: IAsyncEnumerable<int> =
                        AsyncEnumerable.AsyncEnumerable(
                            [
                                3
                                1
                                2
                            ],
                            Func<_, _>(cancellableValueTask { do! Task.Yield() })
                        )

                    do!
                        task {
                            for item in items do
                                do! Task.Yield()
                                seen.Add item
                        }

                    Expect.equal
                        (List.ofSeq seen)
                        [
                            3
                            1
                            2
                        ]
                        "The body should see each element in order"
                }
        ]

    let disposalOutcomeTests =
        let bodies = [
            AsyncDisposalOutcome.BodyCompletes
            AsyncDisposalOutcome.BodyThrows
        ]

        testList "DisposeAsync outcome" [
            AsyncDisposalOutcome.tests
                "runtime task"
                AsyncDisposalOutcome.Static
                bodies
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    task {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts
                    }
                    :> Task
                )
                (fun body cts items ->
                    task {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts
                    }
                    :> Task
                )
            AsyncDisposalOutcome.tests
                "runtime backgroundTask"
                AsyncDisposalOutcome.Static
                bodies
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    backgroundTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts
                    }
                    :> Task
                )
                (fun body cts items ->
                    backgroundTask {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts
                    }
                    :> Task
                )
            AsyncDisposalOutcome.tests
                "runtime valueTask"
                AsyncDisposalOutcome.Static
                bodies
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    valueTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts
                    }
                    |> fun vtask -> vtask.AsTask() :> Task
                )
                (fun body cts items ->
                    valueTask {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts
                    }
                    |> fun vtask -> vtask.AsTask() :> Task
                )
        ]

    let moreBuilderTests =
        testList "More builder cases" [
            testCase
                "task posts its continuation to the caller SynchronizationContext once after it awaits an incomplete Task"
            <| fun () ->
                let mutable posted = 0

                let syncContext =
                    { new SynchronizationContext() with
                        member _.Post(d, state) =
                            Interlocked.Increment(&posted)
                            |> ignore

                            d.Invoke(state)
                    }

                let release = TaskCompletionSource<int>()

                let t =
                    use _sync = TestHelpers.setSyncContext syncContext

                    task {
                        let! value = release.Task
                        return value
                    }

                Task.Run(fun () -> release.SetResult 1).WaitAsync(TimeSpan.FromSeconds 10.).Wait()

                let value = t.WaitAsync(TimeSpan.FromSeconds 10.).Result
                Expect.equal value 1 "Should return the awaited value"
                Expect.equal posted 1 "Should post the continuation to the caller context once"

            testCaseTask
                "backgroundTask gives a fault to its task when it escapes the caller context"
            <| fun () ->
                task {
                    let syncContext =
                        { new SynchronizationContext() with
                            member _.Post(d, state) = d.Invoke(state)
                        }

                    let result =
                        use _sync = TestHelpers.setSyncContext syncContext

                        backgroundTask {
                            do! Task.Yield()
                            failwith "background"
                            return 1
                        }

                    let! message =
                        task {
                            try
                                let! _ = result
                                return None
                            with e ->
                                return Some e.Message
                        }

                    Expect.equal
                        message
                        (Some "background")
                        "The fault should reach the returned task"
                }

            testCaseTask "backgroundTask escapes a task scheduler that is not the default"
            <| fun () ->
                task {
                    let scheduler = ConcurrentExclusiveSchedulerPair().ExclusiveScheduler

                    let! struct (before, after) =
                        Task.Factory
                            .StartNew(
                                (fun () ->
                                    backgroundTask {
                                        let before = TaskScheduler.Current
                                        do! Task.Yield()
                                        return struct (before, TaskScheduler.Current)
                                    }
                                ),
                                CancellationToken.None,
                                TaskCreationOptions.None,
                                scheduler
                            )
                            .Unwrap()

                    Expect.equal
                        before
                        TaskScheduler.Default
                        "The body should start on the default scheduler"

                    Expect.equal
                        after
                        TaskScheduler.Default
                        "The body should resume on the default scheduler"
                }

            testCaseTask "backgroundTask and valueTask infer Task<'T> and ValueTask<'T> sources"
            <| fun () ->
                task {
                    let fromTask t = backgroundTask { return! t }
                    let fromValueTask t = valueTask { return! t }
                    let! a = fromTask (Task.FromResult 1)
                    let! b = fromValueTask (ValueTask<int>(2))
                    Expect.equal (a + b) 3 "Should infer the source type from the builder"
                }

            testCaseTask "binds a non-generic cold task and an awaiter"
            <| fun () ->
                task {
                    let mutable ran = false

                    let cold () =
                        task {
                            do! Task.Yield()
                            ran <- true
                        }
                        :> Task

                    let! value =
                        task {
                            do! cold
                            let! x = (Task.FromResult 2).GetAwaiter()
                            return x
                        }

                    Expect.isTrue ran "Should run the cold task"
                    Expect.equal value 2 "Should read the awaiter result"
                }

            testCaseTask
                "and! starts a non-generic cold task operand before it awaits the left operand"
            <| fun () ->
                task {
                    let rightStarted =
                        TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)

                    let left () =
                        task {
                            do! rightStarted.Task
                            return 1
                        }

                    let right () =
                        rightStarted.SetResult()
                        Task.CompletedTask

                    let result =
                        task {
                            let! a = left
                            and! () = right
                            return a
                        }

                    let! actual = result.WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.equal
                        actual
                        1
                        "The right operand must start before the left one completes"
                }
        ]

    let coldAndCancellableTests =
        testList "Cold and cancellable builders" [
            testCaseAsync "coldTask body runs in a runtime-async method"
            <| async {
                let work =
                    coldTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }

                let! method =
                    work ()
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "backgroundColdTask body runs in a runtime-async method"
            <| async {
                let work =
                    backgroundColdTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }

                let! method =
                    work ()
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "cancellableTask body runs in a runtime-async method"
            <| async {
                let work =
                    cancellableTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }

                let! method =
                    work CancellationToken.None
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "backgroundCancellableTask body runs in a runtime-async method"
            <| async {
                let work =
                    backgroundCancellableTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }

                let! method =
                    work CancellationToken.None
                    |> Async.AwaitTask

                expectRuntimeAsync method
            }

            testCaseAsync "cancellableValueTask body runs in a runtime-async method"
            <| async {
                let work =
                    cancellableValueTask {
                        let method = nonNull (MethodBase.GetCurrentMethod())
                        do! Task.Yield()
                        return method
                    }

                let! method =
                    work CancellationToken.None
                    |> Async.AwaitValueTask

                expectRuntimeAsync method
            }

            testCaseTask
                "and! starts both cancellable operands with the token before it awaits them"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let rightStarted = TaskCompletionSource<CancellationToken>()

                    let left (ct: CancellationToken) =
                        task {
                            let! rightToken = rightStarted.Task
                            return ct = rightToken
                        }

                    let right (ct: CancellationToken) =
                        rightStarted.SetResult ct
                        Task.FromResult ct

                    let work =
                        cancellableTask {
                            let! sameToken = left
                            and! rightToken = right
                            return struct (sameToken, rightToken)
                        }

                    let! struct (sameToken, rightToken) =
                        (work cts.Token).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue sameToken "Both operands should get the same token"
                    Expect.equal rightToken cts.Token "The operands should get the caller token"
                }

            testCaseTask "and! gives the left exception when both cancellable operands fail"
            <| fun () ->
                task {
                    let left (_: CancellationToken) : Task<int> =
                        Task.FromException<int>(ArgumentException "left")

                    let right (_: CancellationToken) : Task<int> =
                        Task.FromException<int>(InvalidOperationException "right")

                    let work =
                        cancellableTask {
                            let! a = left
                            and! b = right
                            return a + b
                        }

                    let! message =
                        task {
                            try
                                let! _ = work CancellationToken.None
                                return None
                            with e ->
                                return Some e.Message
                        }

                    Expect.equal message (Some "left") "The left operand is awaited first"
                }

            testCase "backgroundColdTask escapes the caller SynchronizationContext"
            <| fun () ->
                let mutable posted = false

                let syncContext =
                    { new SynchronizationContext() with
                        member _.Post(d, state) =
                            posted <- true
                            d.Invoke(state)
                    }

                let work =
                    backgroundColdTask {
                        let onThreadPool = Thread.CurrentThread.IsThreadPoolThread
                        do! Task.Yield()
                        return onThreadPool
                    }

                let t =
                    use _sync = TestHelpers.setSyncContext syncContext
                    work ()

                Expect.isTrue
                    (t.WaitAsync(TimeSpan.FromSeconds 10.).Result)
                    "Should start on a thread pool thread"

                Expect.isFalse posted "Should not post to the caller context"

            testCase "backgroundCancellableTask escapes the caller SynchronizationContext"
            <| fun () ->
                let mutable posted = false

                let syncContext =
                    { new SynchronizationContext() with
                        member _.Post(d, state) =
                            posted <- true
                            d.Invoke(state)
                    }

                let work =
                    backgroundCancellableTask {
                        let onThreadPool = Thread.CurrentThread.IsThreadPoolThread
                        do! Task.Yield()
                        return onThreadPool
                    }

                let t =
                    use _sync = TestHelpers.setSyncContext syncContext
                    work CancellationToken.None

                Expect.isTrue
                    (t.WaitAsync(TimeSpan.FromSeconds 10.).Result)
                    "Should start on a thread pool thread"

                Expect.isFalse posted "Should not post to the caller context"

            testCaseTask
                "let! with more steps of a cancellable value checks the token before it calls the value"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let mutable called = false

                    let work =
                        cancellableTask {
                            cts.Cancel()

                            let! value =
                                fun (_: CancellationToken) ->
                                    called <- true
                                    Task.FromResult 1

                            do! Task.CompletedTask
                            return value
                        }

                    let result = work cts.Token

                    let! _ =
                        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse called "The builder should not call the cancellable value"
                }

            testCaseTask "let! with more steps of a task checks the token before it continues"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let mutable continued = false

                    let work =
                        cancellableTask {
                            cts.Cancel()
                            let! value = Task.FromResult 1
                            continued <- true
                            do! Task.CompletedTask
                            return value
                        }

                    let result = work cts.Token

                    let! _ =
                        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse continued "The builder should not continue after the let!"
                }

            testCaseTask
                "let! with return of a cancellable value checks the token before it calls the value"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let mutable called = false

                    let work =
                        cancellableTask {
                            cts.Cancel()

                            let! value =
                                fun (_: CancellationToken) ->
                                    called <- true
                                    Task.FromResult 1

                            return value
                        }

                    let result = work cts.Token

                    let! _ =
                        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse called "The builder should not call the cancellable value"
                }

            testCaseTask "let! with return of a task checks the token before it continues"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let mutable continued = false

                    let work =
                        cancellableTask {
                            cts.Cancel()
                            let! value = Task.FromResult 1
                            continued <- true
                            return value
                        }

                    let result = work cts.Token

                    let! _ =
                        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse continued "The builder should not continue after the let!"
                }

            testCaseTask
                "cancellableTask does not run the body when the token is cancelled at the call"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    cts.Cancel()
                    let mutable ran = false

                    let work =
                        cancellableTask {
                            ran <- true
                            return 1
                        }

                    let result = work cts.Token

                    let! _ =
                        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse ran "The body should not run"
                }
        ]

    type CountingAsyncEnumerable(count: int) =
        let mutable moves = 0
        let mutable disposals = 0
        member _.Moves = moves
        member _.Disposals = disposals

        interface IAsyncEnumerable<int> with
            member _.GetAsyncEnumerator(_) =
                { new IAsyncEnumerator<int> with
                    member _.MoveNextAsync() =
                        moves <- moves + 1

                        ValueTask<bool>(
                            moves
                            <= count
                        )

                    member _.Current = moves

                    member _.DisposeAsync() =
                        disposals <-
                            disposals
                            + 1

                        ValueTask()
                }

    type CountingValueTaskSource(value: int) =
        let mutable reads = 0
        member _.Reads = reads

        interface IValueTaskSource<int> with
            member _.GetStatus(_) = ValueTaskSourceStatus.Succeeded

            member _.GetResult(_) =
                reads <- reads + 1
                value

            member _.OnCompleted(_, _, _, _) =
                invalidOp "The source is already complete"

    let waitForCompletion (t: Task) =
        t.ContinueWith(fun (_: Task) -> ()).WaitAsync(TimeSpan.FromSeconds 10.)

    let messageOf (t: Task) =
        match t.Exception with
        | null -> None
        | e -> Some e.InnerException.Message

    let cancellableRulesTests =
        testList "Cancellable rules" [
            testCaseTask "for runs the body once for each item in task and cancellableTask"
            <| fun () ->
                task {
                    let count = 10000
                    let mutable seqVisits = 0
                    let mutable asyncVisits = 0
                    let mutable cancellableVisits = 0

                    do!
                        task {
                            for _ in [ 1..count ] do
                                seqVisits <-
                                    seqVisits
                                    + 1

                            for _ in CountingAsyncEnumerable count do
                                asyncVisits <-
                                    asyncVisits
                                    + 1
                        }

                    do!
                        (cancellableTask {
                            for _ in [ 1..count ] do
                                cancellableVisits <-
                                    cancellableVisits
                                    + 1

                            for _ in CountingAsyncEnumerable count do
                                cancellableVisits <-
                                    cancellableVisits
                                    + 1
                        })
                            CancellationToken.None

                    Expect.equal seqVisits count "task for over a seq"
                    Expect.equal asyncVisits count "task for over an IAsyncEnumerable"

                    Expect.equal
                        cancellableVisits
                        (2 * count)
                        "cancellableTask for over a seq and an IAsyncEnumerable"
                }

            testCaseTask "for over an IAsyncEnumerable checks the token before each move"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let items = CountingAsyncEnumerable 5
                    let mutable visits = 0

                    let work =
                        cancellableTask {
                            for _ in items do
                                visits <- visits + 1
                                cts.Cancel()
                        }

                    let result = work cts.Token
                    do! waitForCompletion result
                    Expect.isTrue result.IsCanceled "The loop should stop with a cancelled task"
                    Expect.equal visits 1 "The body should run once"
                    Expect.equal items.Moves 1 "The builder should not move again after the cancel"
                    Expect.equal items.Disposals 1 "The enumerator should be disposed once"
                }

            testCaseTask
                "cancellableValueTask for over an IAsyncEnumerable checks the token before each move"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let items = CountingAsyncEnumerable 5
                    let mutable visits = 0

                    let work =
                        cancellableValueTask {
                            for _ in items do
                                visits <- visits + 1
                                cts.Cancel()
                        }

                    let result = (work cts.Token).AsTask()
                    do! waitForCompletion result
                    Expect.isTrue result.IsCanceled "The loop should stop with a cancelled task"
                    Expect.equal visits 1 "The body should run once"
                }

            testCaseTask "and! checks the token before it awaits the right operand"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let left = TaskCompletionSource<int>()
                    let right = TaskCompletionSource<int>()

                    let work =
                        cancellableTask {
                            let! a = left.Task
                            and! b = right.Task
                            return a + b
                        }

                    let result = work cts.Token
                    cts.Cancel()
                    left.SetResult 1
                    do! waitForCompletion result

                    Expect.isTrue
                        result.IsCanceled
                        "The task should be cancelled without the right operand"
                }

            testCaseTask
                "and! with cancellable operands checks the token before it awaits the right operand"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let left = TaskCompletionSource<int>()
                    let right = TaskCompletionSource<int>()

                    let work =
                        cancellableTask {
                            let! a = fun (_: CancellationToken) -> left.Task
                            and! b = fun (_: CancellationToken) -> right.Task
                            return a + b
                        }

                    let result = work cts.Token
                    cts.Cancel()
                    left.SetResult 1
                    do! waitForCompletion result

                    Expect.isTrue
                        result.IsCanceled
                        "The task should be cancelled without the right operand"
                }

            testCaseTask
                "and! does not start the right operand when the left operand throws before it returns"
            <| fun () ->
                task {
                    let mutable rightCalls = 0

                    let work =
                        cancellableTask {
                            let! a =
                                fun (_: CancellationToken) ->
                                    raise (ArgumentException "left-sync")
                                    Task.FromResult 1

                            and! b =
                                fun (_: CancellationToken) ->
                                    rightCalls <-
                                        rightCalls
                                        + 1

                                    Task.FromResult 2

                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "left-sync")
                        "The left exception should win"

                    Expect.equal rightCalls 0 "The right operand should not start"
                }

            testCaseTask "and! fails at once when the right operand throws before it returns"
            <| fun () ->
                task {
                    let left = TaskCompletionSource<int>()

                    let work =
                        cancellableTask {
                            let! a = fun (_: CancellationToken) -> left.Task

                            and! b =
                                fun (_: CancellationToken) ->
                                    raise (ArgumentException "right-sync")
                                    Task.FromResult 2

                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "right-sync")
                        "The right exception should arrive without the left result"
                }

            testCaseTask "and! gives the start exception before an awaited fault"
            <| fun () ->
                task {
                    let work =
                        cancellableTask {
                            let! a =
                                fun (_: CancellationToken) ->
                                    Task.FromException<int>(InvalidOperationException "left-async")

                            and! b =
                                fun (_: CancellationToken) ->
                                    raise (ArgumentException "right-sync")
                                    Task.FromResult 2

                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "right-sync")
                        "A start exception comes before an await"
                }

            testCaseTask "and! does not read the right result before the left operand completes"
            <| fun () ->
                task {
                    let left = TaskCompletionSource<int>()
                    let source = CountingValueTaskSource 2

                    let work =
                        cancellableTask {
                            let! a = fun (_: CancellationToken) -> left.Task

                            and! b =
                                fun (_: CancellationToken) ->
                                    ValueTask<int>(source :> IValueTaskSource<int>, 0s)

                            return a + b
                        }

                    let result = work CancellationToken.None
                    let readsBeforeLeft = source.Reads
                    left.SetException(InvalidOperationException "left")
                    do! waitForCompletion result

                    Expect.equal
                        readsBeforeLeft
                        0
                        "The right result should not be read before the left operand"

                    Expect.equal source.Reads 0 "A left fault should skip the right result"
                }

            testCaseTask "and! starts cancellable operands from left to right"
            <| fun () ->
                task {
                    let order = ResizeArray<string>()

                    let start name (_: CancellationToken) =
                        order.Add name
                        Task.FromResult 1

                    let work =
                        cancellableTask {
                            let! a = start "left"
                            and! b = start "right"
                            return a + b
                        }

                    let! _ = (work CancellationToken.None).WaitAsync(TimeSpan.FromSeconds 10.)

                    Expect.equal
                        (List.ofSeq order)
                        [
                            "left"
                            "right"
                        ]
                        "The operands should start from left to right"
                }

            testCaseTask
                "and! starts a cancellable right operand before it awaits a task on the left"
            <| fun () ->
                task {
                    let left = TaskCompletionSource<int>()
                    let rightStarted = TaskCompletionSource()

                    let work =
                        cancellableTask {
                            let! a = left.Task

                            and! b =
                                fun (_: CancellationToken) ->
                                    rightStarted.SetResult()
                                    Task.FromResult 2

                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! rightStarted.Task.WaitAsync(TimeSpan.FromSeconds 10.)
                    left.SetResult 1
                    let! value = result.WaitAsync(TimeSpan.FromSeconds 10.)
                    Expect.equal value 3 "Should combine both results"
                }

            testCaseTask "return! of a task checks the token"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()

                    let work =
                        cancellableTask {
                            cts.Cancel()
                            return! Task.FromResult 1
                        }

                    let result = work cts.Token
                    do! waitForCompletion result
                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                }

            testCaseTask "return! of a cancellable value checks the token before it calls the value"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let mutable called = false

                    let work =
                        cancellableTask {
                            cts.Cancel()

                            return!
                                fun (_: CancellationToken) ->
                                    called <- true
                                    Task.FromResult 1
                        }

                    let result = work cts.Token
                    do! waitForCompletion result
                    Expect.isTrue result.IsCanceled "The task should be cancelled"
                    Expect.isFalse called "The builder should not call the cancellable value"
                }

            testCaseTask
                "a call with a cancelled token gives TaskCanceledException, as the state-machine builders do"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    cts.Cancel()
                    let mutable ran = 0

                    let results: Task list = [
                        (cancellableTask {
                            ran <- ran + 1
                            return 1
                        })
                            cts.Token
                        :> Task
                        (backgroundCancellableTask {
                            ran <- ran + 1
                            return 1
                        })
                            cts.Token
                        :> Task
                        ((cancellableValueTask {
                            ran <- ran + 1
                            return 1
                        })
                            cts.Token)
                            .AsTask()
                        :> Task
                    ]

                    for result in results do
                        do! waitForCompletion result
                        Expect.isTrue result.IsCanceled "The task should be cancelled"

                        let thrown =
                            try
                                result.GetAwaiter().GetResult()
                                None
                            with :? OperationCanceledException as e ->
                                Some(e.GetType(), e.CancellationToken = cts.Token)

                        Expect.equal
                            thrown
                            (Some(typeof<TaskCanceledException>, true))
                            "The awaited exception type and token"

                    Expect.equal ran 0 "No body should run"
                }

            testCaseTask
                "backgroundColdTask and backgroundCancellableTask escape a task scheduler that is not the default"
            <| fun () ->
                task {
                    let scheduler = ConcurrentExclusiveSchedulerPair().ExclusiveScheduler

                    let onScheduler (f: unit -> Task<TaskScheduler>) =
                        Task.Factory
                            .StartNew(
                                f,
                                CancellationToken.None,
                                TaskCreationOptions.None,
                                scheduler
                            )
                            .Unwrap()

                    let cold = backgroundColdTask { return TaskScheduler.Current }
                    let cancellable = backgroundCancellableTask { return TaskScheduler.Current }
                    let! coldScheduler = onScheduler cold

                    let! cancellableScheduler =
                        onScheduler (fun () -> cancellable CancellationToken.None)

                    Expect.equal
                        coldScheduler
                        TaskScheduler.Default
                        "backgroundColdTask should start on the default scheduler"

                    Expect.equal
                        cancellableScheduler
                        TaskScheduler.Default
                        "backgroundCancellableTask should start on the default scheduler"
                }

            testCaseTask
                "backgroundCancellableTask gives a fault on the thread pool path to its task"
            <| fun () ->
                task {
                    let syncContext =
                        { new SynchronizationContext() with
                            member _.Post(d, state) = d.Invoke(state)
                        }

                    let work =
                        backgroundCancellableTask {
                            do! Task.Yield()
                            raise (InvalidOperationException "worker")
                            return 1
                        }

                    let result =
                        use _sync = TestHelpers.setSyncContext syncContext
                        work CancellationToken.None

                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "worker")
                        "The fault should reach the returned task"
                }

            testCaseTask "coldTask binds a function that returns an awaiter"
            <| fun () ->
                task {
                    let work =
                        coldTask {
                            let! value = fun () -> (Task.FromResult 41).GetAwaiter()
                            return value + 1
                        }

                    let! value = work ()
                    Expect.equal value 42 "Should await the awaiter from the function"
                }

            testCase "the linked suites use the runtime-async builders"
            <| fun () ->
                let asm = typeof<CountingAsyncEnumerable>.Assembly

                let flags =
                    BindingFlags.Public
                    ||| BindingFlags.NonPublic
                    ||| BindingFlags.Static
                    ||| BindingFlags.Instance
                    ||| BindingFlags.DeclaredOnly

                let countIn (moduleName: string) =
                    asm.GetTypes()
                    |> Seq.filter (fun t ->
                        let name = string t.FullName
                        name.StartsWith($"IcedTasks.Tests.{moduleName}+")
                    )
                    |> Seq.collect (fun t -> t.GetMethods(flags))
                    |> Seq.filter isRuntimeAsync
                    |> Seq.length

                for moduleName in
                    [
                        "TaskTests"
                        "TaskBackgroundTests"
                        "ValueTaskTests"
                        "ColdTaskTests"
                        "CancellableTaskTests"
                        "CancellableValueTaskTests"
                    ] do
                    Expect.isGreaterThan
                        (countIn moduleName)
                        10
                        $"%s{moduleName} should compile its tests with the runtime-async builders"
        ]

    type CancellingAsyncEnumerable(cts: CancellationTokenSource, move: Task<bool>) =
        let mutable disposals = 0
        member _.Disposals = disposals

        interface IAsyncEnumerable<int> with
            member _.GetAsyncEnumerator(_) =
                { new IAsyncEnumerator<int> with
                    member _.MoveNextAsync() =
                        cts.Cancel()
                        ValueTask<bool>(move)

                    member _.Current = 1

                    member _.DisposeAsync() =
                        disposals <-
                            disposals
                            + 1

                        ValueTask()
                }

    let cancellableRules2Tests =
        testList "Cancellable rules 2" [
            testCaseTask
                "for over an IAsyncEnumerable checks the token after a move that cancels it"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let move = TaskCompletionSource<bool>()
                    let items = CancellingAsyncEnumerable(cts, move.Task)
                    let mutable visits = 0

                    let work =
                        cancellableTask {
                            for _ in items do
                                visits <- visits + 1
                        }

                    let result = work cts.Token
                    do! waitForCompletion result
                    Expect.isTrue result.IsCanceled "The loop should stop without the pending move"
                    Expect.equal visits 0 "The body should not run"
                    Expect.equal items.Disposals 1 "The enumerator should be disposed once"
                }

            testCaseTask
                "and! of a task and a cancellable value checks the token before it awaits the right operand"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let left = TaskCompletionSource<int>()
                    let right = TaskCompletionSource<int>()

                    let work =
                        cancellableTask {
                            let! a = left.Task
                            and! b = fun (_: CancellationToken) -> right.Task
                            return a + b
                        }

                    let result = work cts.Token
                    cts.Cancel()
                    left.SetResult 1
                    do! waitForCompletion result

                    Expect.isTrue
                        result.IsCanceled
                        "The task should be cancelled without the right operand"
                }

            testCaseTask
                "and! of a cancellable value and a task checks the token before it awaits the right operand"
            <| fun () ->
                task {
                    use cts = new CancellationTokenSource()
                    let left = TaskCompletionSource<int>()
                    let right = TaskCompletionSource<int>()

                    let work =
                        cancellableTask {
                            let! a = fun (_: CancellationToken) -> left.Task
                            and! b = right.Task
                            return a + b
                        }

                    let result = work cts.Token
                    cts.Cancel()
                    left.SetResult 1
                    do! waitForCompletion result

                    Expect.isTrue
                        result.IsCanceled
                        "The task should be cancelled without the right operand"
                }

            testCaseTask
                "and! of a cancellable value and a task gives the left exception when both fail"
            <| fun () ->
                task {
                    let work =
                        cancellableTask {
                            let! a =
                                fun (_: CancellationToken) ->
                                    Task.FromException<int>(ArgumentException "left")

                            and! b = Task.FromException<int>(InvalidOperationException "right")
                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "left")
                        "The left operand is awaited first"
                }

            testCaseTask
                "and! of a task and a cancellable value gives the left exception when both fail"
            <| fun () ->
                task {
                    let work =
                        cancellableTask {
                            let! a = Task.FromException<int>(ArgumentException "left")

                            and! b =
                                fun (_: CancellationToken) ->
                                    Task.FromException<int>(InvalidOperationException "right")

                            return a + b
                        }

                    let result = work CancellationToken.None
                    do! waitForCompletion result

                    Expect.equal
                        (messageOf result)
                        (Some "left")
                        "The left operand is awaited first"
                }
        ]

    [<Tests>]
    let tests =
        testList "IcedTasks.Polyfill.RuntimeTask" [
            builderTests
            runtimeAsyncTests
            valueTaskTests
            syncContextTests
            startOrderTests
            disposalOutcomeTests
            moreBuilderTests
            disposeOnCancelAndOrderTests
            coldAndCancellableTests
            cancellableRulesTests
            cancellableRules2Tests
        ]
