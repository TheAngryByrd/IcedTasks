namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
open System.Collections.Generic
open FSharp.Control

module CancellableValueTaskTests =
    open TimeProviderExtensions

    let builderTests =
        testList "CancellableValueTaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = cancellableValueTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitCancellableValueTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom CancellableValueTask"
                <| async {
                    let fooTask: CancellableValueTask = fun ct -> ValueTask.CompletedTask
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom CancellableValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableValueTask<_> = fun ct -> ValueTask.FromResult expected
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom CancellableTask"
                <| async {
                    let fooTask: CancellableTask = fun ct -> Task.CompletedTask
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom CancellableTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom Cancellable TaskLike"
                <| async {
                    let fooTask = fun (ct: CancellationToken) -> Task.Yield()
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = cancellableValueTask { return! Task.CompletedTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = cancellableValueTask { return! Task.FromResult expected }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom ColdTask"
                <| async {
                    let coldT: ColdTask = fun () -> Task.CompletedTask
                    let outerTask = cancellableValueTask { return! coldT }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom ColdTask<T>"
                <| async {
                    let expected = "lol"
                    let coldT = coldTask { return expected }
                    let outerTask = cancellableValueTask { return! coldT }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = cancellableValueTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind CancellableValueTask"
                <| async {
                    let fooTask: CancellableValueTask = fun ct -> ValueTask.CompletedTask
                    let outerTask = cancellableValueTask { do! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind CancellableValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableValueTask<_> = fun ct -> ValueTask.FromResult expected

                    let outerTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind CancellableTask"
                <| async {
                    let fooTask: CancellableTask = fun ct -> Task.CompletedTask
                    let outerTask = cancellableValueTask { do! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind CancellableTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected

                    let outerTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind Cancellable TaskLike"
                <| async {
                    let fooTask = fun (ct: CancellationToken) -> Task.Yield()

                    let outerTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = cancellableValueTask { do! Task.CompletedTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask =
                        cancellableValueTask {
                            let! result = Task.FromResult expected
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind ColdTask<T>"
                <| async {
                    let expected = "lol"

                    let coldT = coldTask { return expected }

                    let outerTask =
                        cancellableValueTask {
                            let! result = coldT
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind ColdTask"
                <| async {

                    let coldT: ColdTask = fun () -> Task.CompletedTask

                    let outerTask =
                        cancellableValueTask {
                            let! result = coldT
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()

                    let outerTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Type inference"
                <| async {
                    let expected = "lol"

                    let outerTask fooTask =
                        cancellableValueTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask (fun ct -> ValueTask.FromResult expected)
                        |> Async.AwaitCancellableValueTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual =
                        cancellableValueTask {
                            let result = data

                            if true then
                                ()

                            return result
                        }

                    Expect.equal actual data "Zero/Combine/Delay should work"
                }
            ]

            testList "TryWith" [
                testCaseAsync "try with"
                <| async {
                    let data = 42

                    let! actual =
                        cancellableValueTask {
                            let data = data

                            try
                                ()
                            with _ ->
                                ()

                            return data
                        }

                    Expect.equal actual data "TryWith should work"
                }
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let! actual =
                        cancellableValueTask {
                            let data = data

                            try
                                ()
                            finally
                                ()

                            return data
                        }

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
                        cancellableValueTask {
                            use d = TestHelpers.makeDisposable doDispose
                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        cancellableValueTask {
                            use! d =
                                TestHelpers.makeDisposable doDispose
                                |> async.Return

                            return data
                        }

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
                        cancellableValueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)

                            return data
                        }

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
                        Expect.throwsValueTask<Exception>
                            (fun () ->
                                cancellableValueTask {
                                    use d = TestHelpers.makeAsyncDisposable (doDispose)
                                    return ()
                                }
                                |> fun c -> c CancellationToken.None
                            )
                            ""
                        |> Async.AwaitValueTask
                }

                testCaseAsync "use IAsyncDisposable cancelled"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            do! Task.Delay(15)
                            wasDisposed <- true
                        }
                        |> ValueTask

                    let timeProvider = ManualTimeProvider()

                    let actor data =
                        cancellableValueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            do! fun ct -> timeProvider.Delay(TimeSpan.FromMilliseconds(200), ct)

                        }

                    use cts =
                        timeProvider.CreateCancellationTokenSource(TimeSpan.FromMilliseconds(100))

                    let inProgress = actor data cts.Token

                    do!
                        timeProvider.ForwardTimeAsync(TimeSpan.FromMilliseconds(100))
                        |> Async.AwaitTask

                    do!
                        Expect.CancellationRequested inProgress
                        |> Async.AwaitValueTask

                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "use! IAsyncDisposable sync "
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        cancellableValueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
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
                        cancellableValueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            Expect.isFalse wasDisposed ""

                            return data
                        }

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
                        cancellableValueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            Expect.isFalse wasDisposed ""
                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        cancellableValueTask {
                            use d = null
                            return data
                        }

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
                                cancellableValueTask {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }

                            Expect.equal actual loops "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                cancellableValueTask {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }

                            Expect.equal actual loops "Should be ok"
                        }
                    )
            ]


            testList "For" [

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

                            let! actual =
                                cancellableValueTask {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }

                            Expect.equal actual index "Should be ok"
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

                            let! actual =
                                cancellableValueTask {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }

                            Expect.equal actual index "Should be ok"
                        }
                    )

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind in {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                cancellableValueTask {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }

                            Expect.equal actual index "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                cancellableValueTask {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }

                            Expect.equal actual index "Should be ok"
                        }
                    )

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"IAsyncEnumerable for in {loops}"
                        <| async {
                            let mutable index = 0

                            let asyncSeq: IAsyncEnumerable<_> =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

                            let! actual =
                                cancellableValueTask {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitCancellableValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )
                // https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/179
                testCaseAsync "IAsyncEnumerable cancellation"
                <| async {

                    do!
                        Expect.CancellationRequested(
                            cancellableValueTask {

                                let mutable index = 0
                                let loops = 10

                                let asyncSeq: IAsyncEnumerable<_> =
                                    AsyncEnumerable.forXtoY
                                        0
                                        loops
                                        (cancellableValueTask { do! Task.Yield() })

                                use cts = new CancellationTokenSource()

                                let actual =
                                    cancellableValueTask {
                                        for (i: int) in asyncSeq do
                                            do! Task.Yield()

                                            if index >= 5 then
                                                cts.Cancel()

                                            index <- index + 1
                                    }

                                do! actual cts.Token
                            }
                        )
                }


                testCaseAsync "IAsyncEnumerator receives CancellationToken"
                <| async {
                    do!
                        cancellableValueTask {

                            let mutable index = 0
                            let loops = 10

                            let asyncSeq =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

                            use cts = new CancellationTokenSource()

                            let actual =
                                cancellableValueTask {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- index + 1
                                }

                            do! actual cts.Token

                            Expect.equal
                                asyncSeq.LastEnumerator.Value.CancellationToken
                                cts.Token
                                ""
                        }
                }

#if TEST_NETSTANDARD2_1 || TEST_NET6_0_OR_GREATER
                testCaseAsync "TaskSeq receives CancellationToken"
                <| async {

                    do!
                        cancellableValueTask {

                            let loops = 1

                            let mutable CancellationToken = CancellationToken.None

                            let asyncSeq =
                                taskSeq {
                                    for i in 0..loops do
                                        do!
                                            cancellableValueTask {
                                                let! ct =
                                                    CancellableValueTask.getCancellationToken ()

                                                CancellationToken <- ct
                                            }

                                        yield ()
                                }

                            use cts = new CancellationTokenSource()

                            let actual =
                                cancellableValueTask {
                                    for i in asyncSeq do
                                        do! Task.Yield()
                                }

                            do! actual cts.Token

                            Expect.equal CancellationToken cts.Token ""
                        }
                }
#endif

            ]

            testList "MergeSources" [
                testCaseAsync "and! cancellableTask x cancellableTask"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = cancellableValueTask { return 1 }
                            and! b = cancellableValueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! cancellableTask x task"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = cancellableValueTask { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! task x cancellableTask"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = task { return 1 }
                            and! b = cancellableValueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! task x task"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = task { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableT"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = valueTask { return 1 }
                            and! b = valueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableUnit"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = valueTask { return 2 }
                            and! _ = Task.Yield()
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! awaitableUnit x awaitableT "
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! _ = Task.Yield()
                            and! a = valueTask { return 2 }
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! 6 random"
                <| async {
                    let! actual =
                        cancellableValueTask {
                            let! a = cancellableValueTask { return 1 }
                            and! b = coldTask { return 2 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            and! c = fun () -> ValueTask.FromResult(3)
                            return a + b + c
                        }

                    Expect.equal actual 6 ""
                }
            ]
            testSequencedGroup "MergeSourcesParallel - cancellableValueTask"
            <| testList "MergeSourcesParallel" [
                testPropertyWithConfig Expecto.fsCheckConfig "parallelism"
                <| fun () ->
                    asyncEx {
                        let sequencedList = ResizeArray<_>()
                        let parallelList = ResizeArray<_>()

                        let fakeWork id yieldTimes (l: ResizeArray<_>) =
                            cancellableValueTask {
                                lock l (fun () -> l.Add(id))
                                do! Task.yieldMany yieldTimes
                                let dt = DateTimeOffset.UtcNow
                                lock l (fun () -> l.Add(id))
                                return dt
                            }

                        // Have earlier tasks take longer to complete
                        // so we can see if they are sequenced or not
                        let fakeWork1 = fakeWork 1 10000
                        let fakeWork2 = fakeWork 2 750
                        let fakeWork3 = fakeWork 3 500
                        let fakeWork4 = fakeWork 4 250
                        let fakeWork5 = fakeWork 5 100
                        let fakeWork6 = fakeWork 6 1

                        let! sequenced =
                            cancellableValueTask {
                                let! a = fakeWork1 sequencedList
                                let! b = fakeWork2 sequencedList
                                let! c = fakeWork3 sequencedList
                                let! d = fakeWork4 sequencedList
                                let! e = fakeWork5 sequencedList
                                let! f = fakeWork6 sequencedList

                                return [
                                    a
                                    b
                                    c
                                    d
                                    e
                                    f
                                ]
                            }

                        let executeParallel =
                            cancellableValueTask {
                                parallelList.Clear()
                                let! a = fakeWork1 parallelList
                                and! b = fakeWork2 parallelList
                                and! c = fakeWork3 parallelList
                                and! d = fakeWork4 parallelList
                                and! e = fakeWork5 parallelList
                                and! f = fakeWork6 parallelList

                                return [
                                    a
                                    b
                                    c
                                    d
                                    e
                                    f
                                ]
                            }

                        let sequencedEntrances =
                            sequencedList
                            |> Seq.toList

                        let parallelEntrances () =
                            parallelList
                            |> Seq.toList

                        let sequencedAlwaysOrdered =
                            sequencedEntrances = [
                                1
                                1
                                2
                                2
                                3
                                3
                                4
                                4
                                5
                                5
                                6
                                6
                            ]

                        let! parallelNotSequenced =
                            cancellableValueTask {
                                let mutable result = false

                                while not result do
                                    let! _ = executeParallel

                                    result <-
                                        parallelEntrances ()
                                        <> sequencedEntrances

                                return result
                            }

                        return
                            sequencedAlwaysOrdered
                            && parallelNotSequenced
                    }
                    |> Async.RunSynchronously
            ]


            testList "Cancellation Semantics" [

                testCaseAsync "Simple Cancellation"
                <| async {
                    do!
                        Expect.CancellationRequested(
                            cancellableValueTask {

                                let foo = cancellableValueTask { return "lol" }
                                use cts = new CancellationTokenSource()
                                cts.Cancel()
                                let! result = foo cts.Token
                                Expect.equal result "lol" ""
                            }
                        )
                }

                testCaseAsync "CancellableValueTasks are lazily evaluated"
                <| async {

                    let mutable someValue = null

                    do!
                        Expect.CancellationRequested(
                            cancellableValueTask {
                                let fooColdTask = cancellableValueTask { someValue <- "lol" }
                                do! Async.Sleep(100)
                                Expect.equal someValue null ""
                                use cts = new CancellationTokenSource()
                                cts.Cancel()
                                let fooAsync = fooColdTask cts.Token
                                do! Async.Sleep(100)
                                Expect.equal someValue null ""

                                do! fooAsync

                                Expect.equal someValue "lol" ""
                            }
                        )

                    Expect.equal someValue null ""
                }

                testCaseAsync
                    "Can extract context's CancellationToken via CancellableValueTask.getCancellationToken"
                <| async {
                    let fooTask =
                        cancellableValueTask {
                            let! ct = CancellableValueTask.getCancellationToken ()
                            return ct
                        }

                    use cts = new CancellationTokenSource()

                    let! result =
                        fooTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal result cts.Token ""
                }

                testCaseAsync
                    "Can extract context's CancellationToken via CancellableValueTask.getCancellationToken in a deeply nested CE"
                <| async {
                    let timeProvider = ManualTimeProvider()

                    do!
                        Expect.CancellationRequested(
                            cancellableValueTask {
                                let fooTask =
                                    cancellableValueTask {
                                        return!
                                            cancellableValueTask {
                                                do!
                                                    cancellableValueTask {
                                                        let! ct =
                                                            CancellableValueTask
                                                                .getCancellationToken ()

                                                        do!
                                                            timeProvider.Delay(
                                                                TimeSpan.FromMilliseconds(1000),
                                                                ct
                                                            )
                                                    }
                                            }
                                    }

                                use cts =
                                    timeProvider.CreateCancellationTokenSource(
                                        TimeSpan.FromMilliseconds(100)
                                    )

                                let runningTask = fooTask cts.Token
                                do! timeProvider.ForwardTimeAsync(TimeSpan.FromMilliseconds(50))
                                Expect.isFalse runningTask.IsCanceled ""
                                do! timeProvider.ForwardTimeAsync(TimeSpan.FromMilliseconds(50))
                                do! runningTask
                            }
                        )

                }

                testCaseAsync "pass along CancellationToken to async bind"
                <| async {

                    let fooTask =
                        cancellableValueTask {
                            let! result =
                                async {
                                    let! ct = Async.CancellationToken
                                    return ct
                                }

                            return result
                        }

                    use cts = new CancellationTokenSource()

                    let! passedct =
                        fooTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal passedct cts.Token ""
                }

                testCase
                    "CancellationToken flows from Async<unit> to CancellableValueTask<T> via Async.AwaitCancellableValueTask"
                <| fun () ->
                    let innerTask =
                        cancellableValueTask {
                            return! CancellableValueTask.getCancellationToken ()
                        }

                    let outerAsync =
                        async {
                            return!
                                innerTask
                                |> Async.AwaitCancellableValueTask
                        }

                    use cts = new CancellationTokenSource()
                    let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                    Expect.equal actual cts.Token ""

                testCase
                    "CancellationToken flows from Async<unit> to CancellableValueTask via Async.AwaitCancellableValueTask"
                <| fun () ->
                    let mutable actual = CancellationToken.None

                    let innerTask: CancellableValueTask =
                        fun ct ->
                            valueTask { actual <- ct }
                            |> ValueTask.toUnit

                    let outerAsync =
                        async {
                            return!
                                innerTask
                                |> Async.AwaitCancellableValueTask
                        }

                    use cts = new CancellationTokenSource()
                    Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                    Expect.equal actual cts.Token ""
            ]
        ]

    let asyncBuilderTests =
        testList "AsyncBuilder" [

            testCase "AsyncBuilder can Bind CancellableValueTask<T>"
            <| fun () ->
                let innerTask =
                    cancellableValueTask { return! CancellableValueTask.getCancellationToken () }

                let outerAsync =
                    async {
                        let! result = innerTask
                        return result
                    }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableValueTask<T>"
            <| fun () ->
                let innerTask =
                    cancellableValueTask { return! CancellableValueTask.getCancellationToken () }

                let outerAsync = async { return! innerTask }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can Bind CancellableValueTask"
            <| fun () ->
                let mutable actual = CancellationToken.None

                let innerTask: CancellableValueTask =
                    fun ct ->
                        valueTask { actual <- ct }
                        |> ValueTask.toUnit

                let outerAsync = async { do! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""

            testCase "AsyncBuilder can ReturnFrom CancellableValueTask"
            <| fun () ->
                let mutable actual = CancellationToken.None

                let innerTask: CancellableValueTask =
                    fun ct ->
                        valueTask { actual <- ct }
                        |> ValueTask.toUnit

                let outerAsync = async { return! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""
        ]


    let asyncExBuilderTests =
        testList "AsyncExBuilder" [

            testCase "AsyncExBuilder can Bind CancellableValueTask<T>"
            <| fun () ->
                let innerTask =
                    cancellableValueTask { return! CancellableValueTask.getCancellationToken () }

                let outerAsync =
                    asyncEx {
                        let! result = innerTask
                        return result
                    }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableValueTask<T>"
            <| fun () ->
                let innerTask =
                    cancellableValueTask { return! CancellableValueTask.getCancellationToken () }

                let outerAsync = asyncEx { return! innerTask }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can Bind CancellableValueTask"
            <| fun () ->
                let mutable actual = CancellationToken.None

                let innerTask: CancellableValueTask =
                    fun ct ->
                        valueTask { actual <- ct }
                        |> ValueTask.toUnit

                let outerAsync = asyncEx { do! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""

            testCase "AsyncBuilder can ReturnFrom CancellableValueTask"
            <| fun () ->
                let mutable actual = CancellationToken.None

                let innerTask: CancellableValueTask =
                    fun ct ->
                        valueTask { actual <- ct }
                        |> ValueTask.toUnit

                let outerAsync = asyncEx { return! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""
        ]

    let functionTests =
        testList "functions" [
            testList "singleton" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = CancellableValueTask.singleton "lol"

                    let! someTask = innerCall

                    Expect.equal "lol" someTask ""
                }
            ]
            testList "bind" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.bind (fun x ->
                            cancellableValueTask { return x + "fooo" }
                        )

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "map" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.map (fun x -> x + "fooo")

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "apply" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableValueTask { return "lol" }
                    let applier = cancellableValueTask { return fun x -> x + "fooo" }

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.apply applier

                    Expect.equal "lolfooo" someTask ""
                }
            ]

            testList "zip" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableValueTask { return "fooo" }
                    let innerCall2 = cancellableValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.zip innerCall2

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "parZip" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableValueTask { return "fooo" }
                    let innerCall2 = cancellableValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.parallelZip innerCall2

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "ofUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> ValueTask.CompletedTask

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.ofUnit

                    Expect.equal () someTask ""
                }
            ]

            testList "toUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> ValueTask.FromResult "lol"

                    let! someTask =
                        innerCall
                        |> CancellableValueTask.toUnit

                    Expect.equal () someTask ""
                }
            ]
        ]


    let recursionTests =
        testList "Recursion" [
            testCaseAsync "Non-tail recursion"
            <| async {
                let rec loop n =
                    cancellableValueTask {
                        try
                            try
                                // if n % 1000 = 0 then printfn $"in loop at {n}"

                                if n = 42 then
                                    failwith "boom"

                                if n <= 0 then return 0 else return! loop (n - 1)
                            finally
                                () // if n % 1000 = 0 then printfn $"finally at {n}"
                        with exn when n = 10_000 ->
                            //printfn $"caught {exn.Message} at {n}"
                            return 55
                    }

                let! result = loop 100_000
                Expect.equal result 55 ""
            }
        ]

    [<Tests>]
    let cancellationTaskTests =
        testList "IcedTasks.CancellableValueTask" [
            builderTests
            asyncBuilderTests
            asyncExBuilderTests
            functionTests
            recursionTests
        ]
