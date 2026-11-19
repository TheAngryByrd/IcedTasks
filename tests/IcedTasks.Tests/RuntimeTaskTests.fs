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
                                    for _ in [ 1..loops ] do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should run the body once for each item"
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
                                    for _ = 1 to loops do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should run the body once for each item"
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
                                    for _ in [ 1..loops ] do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should run the body once for each item"
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
                                    for _ = 1 to loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should run the body once for each item"
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
                                    1
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

                            let! actual =
                                task {
                                    for (_: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should run the body once for each item"
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
        ]
#endif
