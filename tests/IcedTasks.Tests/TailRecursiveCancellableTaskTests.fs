namespace IcedTasks.Tests

#if TEST_NET6_0_OR_GREATER

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Expecto
open FSharp.Control
open IcedTasks

module TailRecursiveCancellableTaskTests =

    // Takes the dynamic path in every build configuration, as the dynamic test builders of the other
    // cold builders do. See CancellableTaskTests.fs.
    type TailRecursiveCancellableTaskDynamicBuilder() =
        inherit TailRecursiveCancellableTaskBuilder()

        member _.Run(code) =
            TailRecursiveCancellableTaskBuilder.RunDynamic(code)

    let dTailRecursiveCancellableTask = TailRecursiveCancellableTaskDynamicBuilder()

    /// A small stack, so that a recursion that adds a stack frame for each iteration overflows early.
    let smallStackSize = 512 * 1024

    /// Runs `work` on a thread with a small stack and returns its result.
    let runOnSmallStack (work: unit -> 'T) =
        let completion = TaskCompletionSource<'T>()

        let thread =
            Thread(
                (fun () ->
                    try
                        completion.SetResult(work ())
                    with e ->
                        completion.SetException e
                ),
                smallStackSize
            )

        thread.IsBackground <- true
        thread.Start()
        completion.Task

    /// Each call completes synchronously, as a message queue that is not empty does.
    let receive (n: int) : CancellableTask<int> = fun _ -> Task.FromResult n

    /// Suspends, then logs `step{i}`. A tail call of this in a scope must complete before the scope ends.
    let step (order: List<string>) (i: int) : CancellableTask<unit> =
        tailRecursiveCancellableTask {
            do! Task.Yield()
            order.Add $"step{i}"
        }

    /// A chain of n + 1 steps that each suspend and log. Each step is a tail call of the previous one.
    let rec countdown (order: List<string>) (n: int) : CancellableTask<int> =
        tailRecursiveCancellableTask {
            do! Task.Yield()
            order.Add $"step{n}"

            if n = 0 then return 0 else return! countdown order (n - 1)
        }

    let builderTests =
        testList "TailRecursiveCancellableTaskBuilder" [
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom CancellableTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected
                    let outerTask = tailRecursiveCancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom Task<T> and CancellableTask"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask = fun ct -> Task.CompletedTask

                    let outerTask =
                        tailRecursiveCancellableTask { return! Task.FromResult expected }

                    let unitTask = tailRecursiveCancellableTask { return! fooTask }

                    let! actual =
                        outerTask
                        |> Async.AwaitCancellableTask

                    do!
                        unitTask
                        |> Async.AwaitCancellableTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind Task, ValueTask, Async, CancellableTask, and the token"
                <| async {
                    let fooCancellableTask = cancellableTask { return 4 }

                    let outerTask =
                        tailRecursiveCancellableTask {
                            let! a = Task.FromResult 1
                            let! b = ValueTask<int>(2)
                            let! c = async { return 3 }
                            let! d = fooCancellableTask
                            do! Task.CompletedTask
                            let! ct = CancellableTask.getCancellationToken ()
                            Expect.isTrue ct.CanBeCanceled "Should get the ambient token"

                            return
                                a
                                + b
                                + c
                                + d
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual 10 "Should bind each shape"
                }

                testCaseAsync "let! and return of a CancellableTask"
                <| async {
                    let fooCancellableTask = cancellableTask { return 4 }

                    let outerTask =
                        tailRecursiveCancellableTask {
                            let! d = fooCancellableTask
                            return d + 1
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitCancellableTask

                    Expect.equal actual 5 "BindReturn should map the result"
                }
            ]

            testList "Tail calls" [
                testCaseAsync "A recursive loop over synchronous awaits runs on a small stack"
                <| async {
                    let iterations = 1_000_000

                    let rec loop (n: int) (sum: int64) : CancellableTask<int64> =
                        tailRecursiveCancellableTask {
                            let! received = receive n

                            if received = 0 then
                                return sum
                            else
                                return!
                                    loop
                                        (n - 1)
                                        (sum
                                         + int64 received)
                        }

                    let! result =
                        runOnSmallStack (fun () ->
                            (loop iterations 0L CancellationToken.None).Result
                        )
                        |> Async.AwaitTask

                    let expected =
                        int64 iterations
                        * int64 (
                            iterations
                            + 1
                        )
                        / 2L

                    Expect.equal result expected "The loop should run each iteration"
                }

                testCaseAsync
                    "An exception deep in the chain propagates as it does from cancellableTask"
                <| async {
                    let rec withTailRecursive (n: int) : CancellableTask<int> =
                        tailRecursiveCancellableTask {
                            let! received = receive n

                            if received = 1000 then
                                failwith "boom"

                            return!
                                withTailRecursive (
                                    received
                                    + 1
                                )
                        }

                    let rec withCancellableTask (n: int) : CancellableTask<int> =
                        cancellableTask {
                            let! received = receive n

                            if received = 1000 then
                                failwith "boom"

                            return!
                                withCancellableTask (
                                    received
                                    + 1
                                )
                        }

                    let outcome (work: CancellableTask<int>) =
                        async {
                            try
                                let! _ = Async.AwaitCancellableTask work
                                return None
                            with e ->
                                return Some(e.GetType().Name, e.Message)
                        }

                    let! expected = outcome (withCancellableTask 0)
                    let! actual = outcome (withTailRecursive 0)

                    Expect.isSome
                        expected
                        "cancellableTask should propagate the exception of iteration 1000"

                    Expect.equal actual expected "Both builders should propagate the same exception"
                }

                testCaseAsync
                    "The body runs synchronously through tail calls until the first suspension"
                <| async {
                    let mutable steps = 0

                    let gate =
                        TaskCompletionSource<unit>(
                            TaskCreationOptions.RunContinuationsAsynchronously
                        )

                    let rec loop (n: int) : CancellableTask<unit> =
                        tailRecursiveCancellableTask {
                            steps <- n

                            if n < 5 then
                                return! loop (n + 1)
                            else
                                do! gate.Task
                                steps <- 100
                        }

                    let running = loop 0 CancellationToken.None
                    Expect.equal steps 5 "Five tail calls should run before the first suspension"
                    gate.SetResult(())

                    do!
                        running
                        |> Async.AwaitTask

                    Expect.equal steps 100 "The body should resume after the suspension"
                }
            ]

            testList "Parity with cancellableTask" [
                testCaseAsync "The same body gives the same result and the same order of effects"
                <| async {
                    // The two bodies below are the same text. Only the builder differs.
                    let run (log: List<string>) =
                        let rec withCancellableTask (n: int) : CancellableTask<int> =
                            cancellableTask {
                                use _ =
                                    TestHelpers.makeDisposable (fun () -> log.Add $"dispose {n}")

                                let! received = receive n

                                try
                                    if received % 3 = 0 then
                                        failwith "three"
                                with e ->
                                    log.Add $"caught {e.Message} {n}"

                                try
                                    for i in 1..2 do
                                        log.Add $"for {n} {i}"

                                    let mutable j = 0

                                    while j < 2 do
                                        log.Add $"while {n} {j}"
                                        j <- j + 1
                                finally
                                    log.Add $"finally {n}"

                                if received = 0 then
                                    return 42
                                else
                                    return!
                                        withCancellableTask (
                                            received
                                            - 1
                                        )
                            }

                        let rec withTailRecursive (n: int) : CancellableTask<int> =
                            tailRecursiveCancellableTask {
                                use _ =
                                    TestHelpers.makeDisposable (fun () -> log.Add $"dispose {n}")

                                let! received = receive n

                                try
                                    if received % 3 = 0 then
                                        failwith "three"
                                with e ->
                                    log.Add $"caught {e.Message} {n}"

                                try
                                    for i in 1..2 do
                                        log.Add $"for {n} {i}"

                                    let mutable j = 0

                                    while j < 2 do
                                        log.Add $"while {n} {j}"
                                        j <- j + 1
                                finally
                                    log.Add $"finally {n}"

                                if received = 0 then
                                    return 42
                                else
                                    return!
                                        withTailRecursive (
                                            received
                                            - 1
                                        )
                            }

                        withCancellableTask, withTailRecursive

                    let expectedLog = List<string>()
                    let actualLog = List<string>()
                    let withCancellableTask, _ = run expectedLog
                    let _, withTailRecursive = run actualLog

                    let! expected =
                        withCancellableTask 5
                        |> Async.AwaitCancellableTask

                    let! actual =
                        withTailRecursive 5
                        |> Async.AwaitCancellableTask

                    Expect.equal actual expected "Both builders should return the same result"
                    Expect.isNonEmpty expectedLog "The sample should log effects"

                    Expect.sequenceEqual
                        actualLog
                        expectedLog
                        "Both builders should run the effects in the same order"
                }
            ]

            testList "Combine" [
                testCaseAsync "A tail call runs to completion before the statement after it"
                <| async {
                    let order = List<string>()

                    let outer (first: bool) =
                        tailRecursiveCancellableTask {
                            if first then
                                return! step order 1

                            order.Add "end"
                        }

                    do!
                        outer true
                        |> Async.AwaitCancellableTask

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "end"
                        ]
                        "The tail call should complete first"
                }
            ]

            testList "While" [
                testCaseAsync "A tail call in a while body runs in each iteration"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            let mutable i = 0

                            while i < 3 do
                                i <- i + 1
                                return! step order i

                            order.Add "end"
                        }

                    do!
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step2"
                            "step3"
                            "end"
                        ]
                        "Each iteration should run its tail call"
                }
            ]

            testList "TryWith" [
                testCaseAsync
                    "try/with catches an exception that a tail-called callee throws after an await"
                <| async {
                    let failsLater: CancellableTask<string> =
                        tailRecursiveCancellableTask {
                            do! Task.Yield()
                            failwith "late"
                            return "unreachable"
                        }

                    let guarded =
                        tailRecursiveCancellableTask {
                            try
                                return! failsLater
                            with e ->
                                return
                                    "caught "
                                    + e.Message
                        }

                    let! actual =
                        guarded
                        |> Async.AwaitCancellableTask

                    Expect.equal
                        actual
                        "caught late"
                        "The handler should see the callee's exception"
                }

                testCaseAsync "A tail call in the handler is a tail call"
                <| async {
                    let rec loop (n: int) : CancellableTask<int> =
                        tailRecursiveCancellableTask {
                            try
                                if n > 0 then
                                    failwith "retry"

                                return 42
                            with _ ->
                                return! loop (n - 1)
                        }

                    let! result =
                        runOnSmallStack (fun () -> (loop 200_000 CancellationToken.None).Result)
                        |> Async.AwaitTask

                    Expect.equal result 42 "The handler should tail call on a small stack"
                }
            ]

            testList "TryFinally" [
                testCaseAsync "finally runs after the tail-called chain completes"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            try
                                return! countdown order 2
                            finally
                                order.Add "finally"
                        }

                    let! result =
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.equal result 0 "The chain should return its result"

                    Expect.sequenceEqual
                        order
                        [
                            "step2"
                            "step1"
                            "step0"
                            "finally"
                        ]
                        "finally should run last"
                }
            ]

            testList "Using" [
                testCaseAsync "use disposes after the tail-called chain completes"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            use _ = TestHelpers.makeDisposable (fun () -> order.Add "dispose")
                            return! countdown order 1
                        }

                    let! result =
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.equal result 0 "The chain should return its result"

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step0"
                            "dispose"
                        ]
                        "dispose should run last"
                }

                testCaseAsync
                    "use with IAsyncDisposable disposes after the tail-called chain completes"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            use _ =
                                TestHelpers.makeAsyncDisposable (fun () ->
                                    order.Add "dispose"
                                    ValueTask()
                                )

                            return! countdown order 1
                        }

                    let! result =
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.equal result 0 "The chain should return its result"

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step0"
                            "dispose"
                        ]
                        "DisposeAsync should run last"
                }
            ]

            testList "For" [
                testCaseAsync "A tail call in a for body runs in each iteration"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            for i in 1..3 do
                                return! step order i

                            order.Add "end"
                        }

                    do!
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step2"
                            "step3"
                            "end"
                        ]
                        "Each iteration should run its tail call"
                }

                testCaseAsync
                    "A tail call in a for body over IAsyncEnumerable runs in each iteration"
                <| async {
                    let order = List<string>()

                    let outer =
                        tailRecursiveCancellableTask {
                            for i in
                                taskSeq {
                                    1
                                    2
                                    3
                                } do
                                return! step order i

                            order.Add "end"
                        }

                    do!
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step2"
                            "step3"
                            "end"
                        ]
                        "Each iteration should run its tail call"
                }
            ]

            testList "Cancellation Semantics" [
                testCaseAsync "Cancellation ends a recursive loop before its next step runs"
                <| async {
                    let mutable seen = 0

                    let gate =
                        TaskCompletionSource<unit>(
                            TaskCreationOptions.RunContinuationsAsynchronously
                        )

                    // Only the first step awaits, so only the check at the start of each step
                    // can see the cancellation.
                    let rec loop (n: int) : CancellableTask<int> =
                        tailRecursiveCancellableTask {
                            if n = 0 then
                                do! gate.Task

                            seen <- n

                            if n < 10 then return! loop (n + 1) else return n
                        }

                    use cts = new CancellationTokenSource()
                    let running = loop 0 cts.Token
                    cts.Cancel()
                    gate.SetResult(())

                    do!
                        Expect.CancellationRequested(
                            running
                            |> Async.AwaitTask
                            |> Async.Ignore
                        )

                    Expect.equal seen 0 "No step should run after the cancellation"
                }
            ]

            testList "Multi Start" [
                testCaseAsync "Multi start tailRecursiveCancellableTask that suspends"
                <| MultiStart.sequential
                    (fun enter ->
                        tailRecursiveCancellableTask {
                            let result = enter ()
                            do! Task.Yield()
                            return result
                        }
                    )
                    (fun work -> work CancellationToken.None)

                testCaseAsync "Multi start tailRecursiveCancellableTask while suspended"
                <| MultiStart.overlapping
                    (fun enter gate ->
                        tailRecursiveCancellableTask {
                            let result = enter ()
                            do! gate
                            return result
                        }
                    )
                    (fun work -> work CancellationToken.None)
            ]

            testList "Dynamic path" [
                testCaseAsync "A recursive loop runs on a small stack"
                <| async {
                    let rec loop (n: int) (sum: int64) : CancellableTask<int64> =
                        dTailRecursiveCancellableTask {
                            let! received = receive n

                            if received = 0 then
                                return sum
                            else
                                return!
                                    loop
                                        (n - 1)
                                        (sum
                                         + int64 received)
                        }

                    let! result =
                        runOnSmallStack (fun () -> (loop 100_000 0L CancellationToken.None).Result)
                        |> Async.AwaitTask

                    Expect.equal result 5_000_050_000L "The loop should run each iteration"
                }

                testCaseAsync "finally runs after the tail-called chain completes"
                <| async {
                    let order = List<string>()

                    let rec countdown (n: int) : CancellableTask<int> =
                        dTailRecursiveCancellableTask {
                            do! Task.Yield()
                            order.Add $"step{n}"

                            if n = 0 then return 0 else return! countdown (n - 1)
                        }

                    let outer =
                        dTailRecursiveCancellableTask {
                            try
                                return! countdown 1
                            finally
                                order.Add "finally"
                        }

                    let! result =
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.equal result 0 "The chain should return its result"

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step0"
                            "finally"
                        ]
                        "finally should run last"
                }

                // The dynamic path runs one flush delegate for each while loop, so it has to reset
                // its pending flag in each iteration. The static path inlines a fresh flush.
                testCaseAsync "A tail call in a while body runs in each iteration"
                <| async {
                    let order = List<string>()

                    let outer =
                        dTailRecursiveCancellableTask {
                            let mutable i = 0

                            while i < 3 do
                                i <- i + 1
                                return! step order i

                            order.Add "end"
                        }

                    do!
                        outer
                        |> Async.AwaitCancellableTask

                    Expect.sequenceEqual
                        order
                        [
                            "step1"
                            "step2"
                            "step3"
                            "end"
                        ]
                        "Each iteration should run its tail call"
                }

                testCaseAsync "A cancelled token gives a cancelled task before the body runs"
                <| async {
                    let mutable ran = false

                    let foo =
                        dTailRecursiveCancellableTask {
                            ran <- true
                            return "lol"
                        }

                    use cts = new CancellationTokenSource()
                    cts.Cancel()

                    do!
                        Expect.CancellationRequested(
                            foo cts.Token
                            |> Async.AwaitTask
                            |> Async.Ignore
                        )

                    Expect.isFalse ran "The body should not run"
                }
            ]
        ]

    let disposalOutcomeTests =
        testList "DisposeAsync outcome" [
            AsyncDisposalOutcome.tests
                "tailRecursiveCancellableTask"
                AsyncDisposalOutcome.Static
                [
                    AsyncDisposalOutcome.BodyCompletes
                    AsyncDisposalOutcome.BodyThrows
                    AsyncDisposalOutcome.BodyCancels
                    AsyncDisposalOutcome.BodyAwaitsCanceledTask
                ]
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    tailRecursiveCancellableTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts

                        if AsyncDisposalOutcome.awaitsCanceledTask body then
                            do! AsyncDisposalOutcome.canceledTask ()

                        do! Task.CompletedTask
                    }
                    |> fun t -> t cts.Token :> Task
                )
                (fun body cts items ->
                    tailRecursiveCancellableTask {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts

                            if AsyncDisposalOutcome.awaitsCanceledTask body then
                                do! AsyncDisposalOutcome.canceledTask ()

                            do! Task.CompletedTask
                    }
                    |> fun t -> t cts.Token :> Task
                )
            AsyncDisposalOutcome.tests
                "tailRecursiveCancellableTask"
                AsyncDisposalOutcome.Dynamic
                [
                    AsyncDisposalOutcome.BodyCompletes
                    AsyncDisposalOutcome.BodyThrows
                    AsyncDisposalOutcome.BodyCancels
                    AsyncDisposalOutcome.BodyAwaitsCanceledTask
                ]
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    dTailRecursiveCancellableTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts

                        if AsyncDisposalOutcome.awaitsCanceledTask body then
                            do! AsyncDisposalOutcome.canceledTask ()

                        do! Task.CompletedTask
                    }
                    |> fun t -> t cts.Token :> Task
                )
                (fun body cts items ->
                    dTailRecursiveCancellableTask {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts

                            if AsyncDisposalOutcome.awaitsCanceledTask body then
                                do! AsyncDisposalOutcome.canceledTask ()

                            do! Task.CompletedTask
                    }
                    |> fun t -> t cts.Token :> Task
                )
        ]

    [<Tests>]
    let tests =
        testList "IcedTasks.TailRecursiveCancellableTask" [
            builderTests
            disposalOutcomeTests
        ]

#endif
