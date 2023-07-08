namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
#if NET7_0_OR_GREATER
open IcedTasks.ValueTaskExtensions
#endif
module CancellableTaskTests =
    open System.Collections.Concurrent
    open TimeProviderExtensions

    let builderTests =
        testList "CancellableTaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = cancellableTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitCancellableTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom CancellableTask"
                <| async {
                    let fooTask: CancellableTask = fun ct -> Task.CompletedTask
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom CancellableTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

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
                    let outerTask = cancellableTask { return! Task.CompletedTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = cancellableTask { return! Task.FromResult expected }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom ColdTask"
                <| async {
                    let coldT: ColdTask = fun () -> Task.CompletedTask
                    let outerTask = cancellableTask { return! coldT }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom ColdTask<T>"
                <| async {
                    let expected = "lol"
                    let coldT = coldTask { return expected }
                    let outerTask = cancellableTask { return! coldT }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = cancellableTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind CancellableTask"
                <| async {
                    let fooTask: CancellableTask = fun ct -> Task.CompletedTask
                    let outerTask = cancellableTask { do! fooTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind CancellableTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected

                    let outerTask = cancellableTask {
                        let! result = fooTask
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
#if NET7_0_OR_GREATER
                testCaseAsync "Can Bind Cancellable TaskLike"
                <| async {
                    let fooTask = fun (ct: CancellationToken) -> Task.Yield()

                    let outerTask = cancellableValueTask {
                        let! result = fooTask
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
#endif
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = cancellableTask { do! Task.CompletedTask }
                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask = cancellableTask {
                        let! result = Task.FromResult expected
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind ColdTask<T>"
                <| async {
                    let expected = "lol"

                    let coldT = coldTask { return expected }

                    let outerTask = cancellableTask {
                        let! result = coldT
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind ColdTask"
                <| async {

                    let coldT: ColdTask = fun () -> Task.CompletedTask

                    let outerTask = cancellableTask {
                        let! result = coldT
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()

                    let outerTask = cancellableTask {
                        let! result = fooTask
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    do!
                        outerTask cts.Token
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask = cancellableTask {
                        let! result = fooTask
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
                        use d = TestHelpers.makeDisposable (doDispose)
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

                    let! actual = cancellableTask {
                        use! d =
                            TestHelpers.makeDisposable (doDispose)
                            |> async.Return

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


#if NET7_0_OR_GREATER
                testCaseAsync "use IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual = cancellableTask {
                        use d = TestHelpers.makeAsyncDisposable (doDispose)

                        return data
                    }

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

                    let! actual = cancellableTask {
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


                    let! actual = cancellableTask {
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


                    let! actual = cancellableTask {
                        use! d =
                            TestHelpers.makeAsyncDisposable (doDispose)
                            |> async.Return

                        Expect.isFalse wasDisposed ""

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
#endif
                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
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

                            let! actual = cancellableTask {
                                for i = 1 to loops do
                                    do! Task.Yield()
                                    index <- i + i

                                return index
                            }

                            Expect.equal actual index "Should be ok"
                        }
                    )
            ]


            testList "MergeSources" [
                testCaseAsync "and! 6"
                <| async {
                    let! actual = cancellableTask {
                        let! a = cancellableTask { return 1 }
                        and! b = coldTask { return 2 }
                        and! _ = Task.Yield()
                        and! _ = ValueTask.CompletedTask
                        and! c = fun () -> ValueTask.FromResult(3)
                        return a + b + c
                    }

                    Expect.equal actual 6 ""

                }
            ]

            testList "Cancellation Semantics" [

                testCaseAsync "Simple Cancellation"
                <| async {
                    do!
                        Expect.CancellationRequested(
                            cancellableTask {

                                let foo = cancellableTask { return "lol" }
                                use cts = new CancellationTokenSource()
                                cts.Cancel()
                                let! result = foo cts.Token
                                Expect.equal result "lol" ""
                            }
                        )
                }

                testCaseAsync "CancellableTasks are lazily evaluated"
                <| async {

                    let mutable someValue = null

                    do!
                        Expect.CancellationRequested(
                            cancellableTask {
                                let fooColdTask = cancellableTask { someValue <- "lol" }
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
                    "Can extract context's CancellationToken via CancellableTask.getCancellationToken"
                <| async {
                    let fooTask = cancellableTask {
                        let! ct = CancellableTask.getCancellationToken ()
                        return ct
                    }

                    use cts = new CancellationTokenSource()

                    let! result =
                        fooTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal result cts.Token ""
                }

                testCaseAsync
                    "Can extract context's CancellationToken via CancellableTask.getCancellationToken in a deeply nested CE"
                <| async {
                    let timeProvider = ManualTimeProvider()

                    do!
                        Expect.CancellationRequested(
                            cancellableTask {
                                let fooTask = cancellableTask {
                                    return! cancellableTask {
                                        do! cancellableTask {
                                            let! ct = CancellableTask.getCancellationToken ()

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

                    let fooTask = cancellableTask {
                        let! result = async {
                            let! ct = Async.CancellationToken
                            return ct
                        }

                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! passedCT =
                        fooTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal passedCT cts.Token ""
                }


                testCase
                    "CancellationToken flows from Async<unit> to CancellableTask<T> via Async.AwaitCancellableTask"
                <| fun () ->
                    let innerTask = cancellableTask {
                        return! CancellableTask.getCancellationToken ()
                    }

                    let outerAsync = async {
                        return!
                            innerTask
                            |> Async.AwaitCancellableTask
                    }

                    use cts = new CancellationTokenSource()
                    let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                    Expect.equal actual cts.Token ""

                testCase
                    "CancellationToken flows from Async<unit> to CancellableTask via Async.AwaitCancellableTask"
                <| fun () ->
                    let mutable actual = CancellationToken.None
                    let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task

                    let outerAsync = async {
                        return!
                            innerTask
                            |> Async.AwaitCancellableTask
                    }

                    use cts = new CancellationTokenSource()
                    Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                    Expect.equal actual cts.Token ""

            ]


        ]

    let asyncBuilderTests =
        testList "AsyncBuilder" [

            testCase "AsyncBuilder can Bind CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken () }

                let outerAsync = async {
                    let! result = innerTask
                    return result
                }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken () }
                let outerAsync = async { return! innerTask }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can Bind CancellableTask"
            <| fun () ->
                let mutable actual = CancellationToken.None
                let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task
                let outerAsync = async { do! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""

            testCase "AsyncBuilder can ReturnFrom CancellableTask"
            <| fun () ->
                let mutable actual = CancellationToken.None
                let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task
                let outerAsync = async { return! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""
        ]


    let asyncExBuilderTests =
        testList "AsyncExBuilder" [

            testCase "AsyncExBuilder can Bind CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken () }

                let outerAsync = asyncEx {
                    let! result = innerTask
                    return result
                }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken () }
                let outerAsync = asyncEx { return! innerTask }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can Bind CancellableTask"
            <| fun () ->
                let mutable actual = CancellationToken.None
                let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task
                let outerAsync = asyncEx { do! innerTask }

                use cts = new CancellationTokenSource()
                Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""

            testCase "AsyncBuilder can ReturnFrom CancellableTask"
            <| fun () ->
                let mutable actual = CancellationToken.None
                let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task
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
                    let innerCall = CancellableTask.singleton "lol"

                    let! someTask = innerCall

                    Expect.equal "lol" someTask ""
                }
            ]
            testList "bind" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableTask.bind (fun x -> cancellableTask { return x + "fooo" })

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "map" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableTask.map (fun x -> x + "fooo")

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "apply" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableTask { return "lol" }
                    let applier = cancellableTask { return fun x -> x + "fooo" }

                    let! someTask =
                        innerCall
                        |> CancellableTask.apply applier

                    Expect.equal "lolfooo" someTask ""
                }
            ]

            testList "zip" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableTask { return "fooo" }
                    let innerCall2 = cancellableTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableTask.zip innerCall2

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "parZip" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = cancellableTask { return "fooo" }
                    let innerCall2 = cancellableTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> CancellableTask.parallelZip innerCall2

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "ofUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> Task.CompletedTask

                    let! someTask =
                        innerCall
                        |> CancellableTask.ofUnit

                    Expect.equal () someTask ""
                }
            ]


            testList "toUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> Task.FromResult "lol"

                    let! someTask =
                        innerCall
                        |> CancellableTask.toUnit

                    Expect.equal () someTask ""
                }
            ]


            testList "whenAll" [
                testCaseAsync "Simple"
                <| async {
                    let items = [ 1..100 ]
                    let times = ConcurrentDictionary<int, DateTimeOffset>()
                    let timeProvider = ManualTimeProvider()

                    let tasks =
                        items
                        |> List.map (fun i -> cancellableTask {
                            do! fun ct -> timeProvider.Delay(TimeSpan.FromSeconds(15.), ct)

                            times.TryAdd(i, timeProvider.GetUtcNow())
                            |> ignore

                            return i + 1
                        })

                    let! ct = Async.CancellationToken
                    let result = CancellableTask.whenAll tasks ct

                    do!
                        timeProvider.ForwardTimeAsync(TimeSpan.FromSeconds(15.))
                        |> Async.AwaitTask

                    let! result =
                        result
                        |> Async.AwaitTask

                    Expect.equal
                        result
                        (items
                         |> List.map (fun i -> i + 1)
                         |> List.toArray)
                        ""

                    times
                    |> Seq.iter (fun (KeyValue(k, v)) ->
                        Expect.equal v (timeProvider.GetUtcNow()) ""
                    )
                }
            ]

            testList "whenAllThrottled" [
                testCaseAsync "Simple"
                <| async {
                    let pauseTime = 15
                    let pauseTimeTS = TimeSpan.FromSeconds pauseTime
                    let maxDegreeOfParallelism = 3
                    let items = [ 1..100 ]
                    let times = ConcurrentDictionary<int, DateTimeOffset>()
                    let timeProvider = ManualTimeProvider()

                    let tasks =
                        items
                        |> List.map (fun i -> cancellableTask {
                            do! fun ct -> timeProvider.Delay(pauseTimeTS, ct)

                            times.TryAdd(i, timeProvider.GetUtcNow())
                            |> ignore

                            return i + 1
                        })

                    let! ct = Async.CancellationToken
                    let result = CancellableTask.whenAllThrottled maxDegreeOfParallelism tasks ct

                    do!
                        timeProvider.ForwardTimeAsync(pauseTimeTS)
                        |> Async.AwaitTask

                    Expect.equal (Seq.length times) maxDegreeOfParallelism ""

                    do!
                        task {
                            let mutable i = maxDegreeOfParallelism

                            while Seq.length times < items.Length do

                                i <-
                                    i
                                    + maxDegreeOfParallelism

                                do!
                                    timeProvider.ForwardTimeAsync(pauseTimeTS)
                                    |> Async.AwaitTask
                                // times isn't guaranteed to be populated because these tasks still
                                // run in realtime kind of, so we need to check
                                // that is at least wasn't executing more than we'd expect
                                Expect.isLessThanOrEqual (Seq.length times) (min i items.Length) ""

                        }
                        |> Async.AwaitTask

                    Expect.equal (Seq.length times) items.Length ""

                    let! result =
                        result
                        |> Async.AwaitTask

                    Expect.equal
                        result
                        (items
                         |> List.map (fun i -> i + 1)
                         |> List.toArray)
                        ""
                }
            ]


            testList "sequential" [
                testCaseAsync "Simple"
                <| async {
                    let pauseTime = 15
                    let pauseTimeTS = TimeSpan.FromSeconds pauseTime
                    let maxDegreeOfParallelism = 1
                    let items = [ 1..100 ]
                    let times = ConcurrentDictionary<int, DateTimeOffset>()
                    let timeProvider = ManualTimeProvider()

                    let tasks =
                        items
                        |> List.map (fun i -> cancellableTask {
                            do! fun ct -> timeProvider.Delay(pauseTimeTS, ct)

                            times.TryAdd(i, timeProvider.GetUtcNow())
                            |> ignore

                            return i + 1
                        })

                    let! ct = Async.CancellationToken
                    let result = CancellableTask.sequential tasks ct

                    do!
                        timeProvider.ForwardTimeAsync(pauseTimeTS)
                        |> Async.AwaitTask

                    Expect.equal (Seq.length times) maxDegreeOfParallelism ""

                    do!
                        task {
                            let mutable i = maxDegreeOfParallelism

                            while Seq.length times < items.Length do
                                i <-
                                    i
                                    + maxDegreeOfParallelism

                                do!
                                    timeProvider.ForwardTimeAsync(pauseTimeTS)
                                    |> Async.AwaitTask

                                Expect.equal (Seq.length times) (min i items.Length) ""

                        }
                        |> Async.AwaitTask

                    Expect.equal (Seq.length times) items.Length ""

                    let! result =
                        result
                        |> Async.AwaitTask

                    Expect.equal
                        result
                        (items
                         |> List.map (fun i -> i + 1)
                         |> List.toArray)
                        ""
                }

            ]

        ]

    [<Tests>]
    let cancellationTaskTests =
        testList "IcedTasks.CancellableTask" [
            builderTests
            asyncBuilderTests
            asyncExBuilderTests
            functionTests
        ]
