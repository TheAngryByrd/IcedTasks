namespace IcedTasks.Tests

#nowarn "3511"

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
open IcedTasks.Polyfill.Task
open System.Collections.Generic

module TaskDynamicTests =
    open System.Runtime.CompilerServices

    type TaskDynamicBuilder() =
        inherit TaskBuilder()

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member _.Run(code) = base.Run(code)


    let dTask = TaskDynamicBuilder()

    let builderTests =
        testList "TaskBuilderDynamic" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = dTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = dTask { return! fooTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = dTask { return! fooTask }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = dTask { return! Task.CompletedTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = dTask { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = dTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = dTask { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = dTask { do! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected

                    let outerTask =
                        dTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = dTask { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask =
                        dTask {
                            let! result = Task.FromResult expected
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask =
                        dTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind Type inference"
                <| async {
                    let expected = "lol"

                    let outerTask fooTask =
                        dTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask (Task.FromResult expected)
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual =
                        dTask {
                            let result = data

                            if true then
                                ()

                            return result
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Zero/Combine/Delay should work"
                }
            ]

            testList "TryWith" [
                testCaseAsync "try with"
                <| async {
                    let data = 42

                    let! actual =
                        dTask {
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
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let! actual =
                        dTask {
                            let data = data

                            try
                                ()
                            finally
                                ()

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "TryFinally should work"
                }
            ]

            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42

                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        dTask {
                            use d = TestHelpers.makeDisposable (doDispose)
                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        dTask {
                            use! d =
                                TestHelpers.makeDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "use IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        dTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        dTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable propagate exception"
                <| async {
                    let doDispose () =
                        task {
                            do! Task.Yield()
                            failwith "boom"
                        }
                        |> ValueTask

                    do!
                        Expect.throwsTask<Exception>
                            (fun () ->
                                dTask {
                                    use d = TestHelpers.makeAsyncDisposable (doDispose)
                                    return ()
                                }
                            )
                            ""
                        |> Async.AwaitTask
                }


                testCaseAsync "use IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        dTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        dTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        dTask {
                            use d = null
                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                }
            ]


            testList "While" [
                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                dTask {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                dTask {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )

                testCaseAsync "while bind to 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let! actual =
                        dTask {
                            while index < loops do
                                do! Task.CompletedTask
                                index <- index + 1

                            return index
                        }
                        |> Async.AwaitTask

                    Expect.equal actual loops "Should be ok"
                }
            ]

            testList "For" [

                testCaseAsync "for gives each element to the body in order"
                <| async {
                    let seen = ResizeArray<int>()

                    let! actual =
                        dTask {
                            for i in
                                [
                                    3
                                    1
                                    2
                                ] do
                                seen.Add i

                            return seen.Count
                        }
                        |> Async.AwaitTask

                    Expect.equal actual 3 "The body must run once for each element"

                    Expect.equal
                        (List.ofSeq seen)
                        [
                            3
                            1
                            2
                        ]
                        "The body must get the elements in order"
                }

                testCaseAsync "IAsyncEnumerable for gives each element to the body in order"
                <| async {
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

                    let! actual =
                        dTask {
                            for (i: int) in items do
                                seen.Add i

                            return seen.Count
                        }
                        |> Async.AwaitTask

                    Expect.equal actual 3 "The body must run once for each element"

                    Expect.equal
                        (List.ofSeq seen)
                        [
                            3
                            1
                            2
                        ]
                        "The body must get the elements in order"
                }

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for in {loops}"
                        <| async {
                            let mutable index = 0
                            let mutable runs = 0

                            let! actual =
                                dTask {
                                    for i in seq { 1..loops } do
                                        runs <- runs + 1
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual (loops * 2) "The last element must reach the body"
                            Expect.equal runs loops "The body must run once for each element"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for to {loops}"
                        <| async {
                            let mutable index = 0
                            let mutable runs = 0

                            let! actual =
                                dTask {
                                    for i = 1 to loops do
                                        runs <- runs + 1
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual (loops * 2) "The last element must reach the body"
                            Expect.equal runs loops "The body must run once for each element"
                        }
                    )

                yield!
                    [
                        10
                        10000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind in {loops}"
                        <| async {
                            let mutable index = 0
                            let mutable runs = 0

                            let! actual =
                                dTask {
                                    for i in seq { 1..loops } do
                                        do! Task.Yield()
                                        runs <- runs + 1
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual (loops * 2) "The last element must reach the body"
                            Expect.equal runs loops "The body must run once for each element"
                        }
                    )


                yield!
                    [
                        10
                        10000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind to {loops}"
                        <| async {
                            let mutable index = 0
                            let mutable runs = 0

                            let! actual =
                                dTask {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        runs <- runs + 1
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual (loops * 2) "The last element must reach the body"
                            Expect.equal runs loops "The body must run once for each element"
                        }
                    )

                testCaseAsync "for bind to 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let! actual =
                        dTask {
                            for i = 1 to loops do
                                do! Task.CompletedTask
                                index <- i + i

                            return index
                        }
                        |> Async.AwaitTask

                    Expect.equal actual (loops * 2) "Every iteration must run"
                }


                yield!
                    [
                        10
                        10000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"IAsyncEnumerable for in {loops}"
                        <| async {
                            let mutable index = 0
                            let mutable runs = 0

                            let asyncSeq: IAsyncEnumerable<_> =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

                            let! actual =
                                dTask {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        runs <- runs + 1
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual (loops * 2) "The last element must reach the body"
                            Expect.equal runs (loops + 1) "The body must run once for each element"
                        }
                    )

                testCaseAsync "IAsyncEnumerable for in 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let asyncSeq: IAsyncEnumerable<_> =
                        AsyncEnumerable.forXtoY
                            0
                            loops
                            (cancellableValueTask { do! Task.CompletedTask })

                    let! actual =
                        dTask {
                            for (i: int) in asyncSeq do
                                do! Task.CompletedTask
                                index <- i + i

                            return index
                        }
                        |> Async.AwaitTask

                    Expect.equal actual (loops * 2) "Every iteration must run"
                }

            ]

            testList "MergeSources" [
                testCaseAsync "and! task x task"
                <| asyncEx {
                    let! actual =
                        dTask {
                            let! a = task { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableT"
                <| asyncEx {
                    let! actual =
                        dTask {
                            let! a = valueTask { return 1 }
                            and! b = valueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableUnit"
                <| asyncEx {
                    let! actual =
                        dTask {
                            let! a = valueTask { return 2 }
                            and! _ = Task.Yield()
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! awaitableUnit x awaitableT "
                <| asyncEx {
                    let! actual =
                        dTask {
                            let! _ = Task.Yield()
                            and! a = valueTask { return 2 }
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! 5 random"
                <| asyncEx {
                    let! actual =
                        dTask {
                            let! a = ValueTask.FromResult 1
                            and! b = Task.FromResult 2
                            and! c = coldTask { return 3 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            return a + b + c
                        }

                    Expect.equal actual 6 ""
                }

            ]
        ]


    let disposalOutcomeTests =
        testList "DisposeAsync outcome" [
            AsyncDisposalOutcome.tests
                "task"
                AsyncDisposalOutcome.Dynamic
                [
                    AsyncDisposalOutcome.BodyCompletes
                    AsyncDisposalOutcome.BodyThrows
                    AsyncDisposalOutcome.BodyAwaitsCanceledTask
                ]
                AsyncDisposalOutcome.taskExpectation
                (fun body cts resource ->
                    dTask {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts

                        if AsyncDisposalOutcome.awaitsCanceledTask body then
                            do! AsyncDisposalOutcome.canceledTask ()
                    }
                    |> fun t -> t :> Task
                )
                (fun body cts items ->
                    dTask {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts

                            if AsyncDisposalOutcome.awaitsCanceledTask body then
                                do! AsyncDisposalOutcome.canceledTask ()
                    }
                    |> fun t -> t :> Task
                )
        ]

    [<Tests>]
    let tests =
        testList "IcedTasks.Polyfill.Task" [
            builderTests
            disposalOutcomeTests
        ]
