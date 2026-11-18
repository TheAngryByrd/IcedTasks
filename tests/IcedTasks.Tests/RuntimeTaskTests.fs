namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks

#if NET11_0_OR_GREATER
open IcedTasks.Polyfill.RuntimeTask

module RuntimeTaskTests =
    open System.Collections.Generic
    open System.Reflection
    open System.Runtime.CompilerServices
    open System.Threading


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
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = task { return "lol" }

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
                    let outerTask = task { return! fooTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = task { return! fooTask }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = task { return! Task.CompletedTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = task { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = task { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = task { return! fooTask }


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
                    let outerTask = task { do! fooTask }

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
                        task {
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
                    let outerTask = task { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask =
                        task {
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
                        task {
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
                        task {
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
                        task {
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
            ]

            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42

                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        task {
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
                        task {
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
                        task {
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
                        task {
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
                                task {
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
                        task {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            do! Task.Yield()
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
                        task {
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
                        task {
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
                                task {
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
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

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
                                task {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

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
                                task {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

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
                                task {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

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
                                task {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

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
                                task {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

            ]

            testList "MergeSources" [

                testCaseAsync "and! task x task"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = task { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableT"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = valueTask { return 1 }
                            and! b = valueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableUnit"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = valueTask { return 2 }
                            and! _ = Task.Yield()
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! awaitableUnit x awaitableT "
                <| asyncEx {
                    let! actual =
                        task {
                            let! _ = Task.Yield()
                            and! a = valueTask { return 2 }
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! 5 random"
                <| asyncEx {
                    let! actual =
                        task {
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

            testSequencedGroup "MergeSourcesParallel - task"
            <| testList "MergeSourcesParallel" [
                testPropertyWithConfig Expecto.fsCheckConfig "parallelism"
                <| fun () ->
                    asyncEx {
                        let! ct = Async.CancellationToken
                        let sequencedList = ResizeArray<_>()
                        let parallelList = ResizeArray<_>()


                        let fakeWork id yieldTimes (l: ResizeArray<_>) =
                            Task.Run<DateTimeOffset>(fun () ->
                                task {
                                    lock l (fun () -> l.Add(id))
                                    do! Task.yieldMany yieldTimes
                                    let dt = DateTimeOffset.UtcNow
                                    lock l (fun () -> l.Add(id))
                                    return dt
                                }
                            )

                        // Have earlier tasks take longer to complete
                        // so we can see if they are sequenced or not
                        let fakeWork1 = fakeWork 1 10000
                        let fakeWork2 = fakeWork 2 750
                        let fakeWork3 = fakeWork 3 500
                        let fakeWork4 = fakeWork 4 250
                        let fakeWork5 = fakeWork 5 100
                        let fakeWork6 = fakeWork 6 1

                        let! sequenced =
                            task {
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

                        let executeParallel () =
                            task {
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
                            task {
                                let mutable result = false

                                while not result do
                                    let! _ = executeParallel ()

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

            testCaseAsync "and! starts both operands before it awaits them"
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

    [<Tests>]
    let tests =
        testList "IcedTasks.Polyfill.RuntimeTask" [
            builderTests
            runtimeAsyncTests
            valueTaskTests
            syncContextTests
        ]
#endif
