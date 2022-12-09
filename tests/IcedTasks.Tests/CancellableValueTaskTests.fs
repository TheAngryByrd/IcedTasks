namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks

module CancellableValueTaskTests =

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

                    let outerTask = cancellableValueTask {
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

                    let outerTask = cancellableValueTask {
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

                    let outerTask = cancellableValueTask {
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

                    let outerTask = cancellableValueTask {
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

                    let outerTask = cancellableValueTask {
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

                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask = cancellableValueTask {
                        let! result = fooTask
                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask cts.Token
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
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

                    let! actual = cancellableValueTask {
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

                    let! actual = cancellableValueTask {
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
                testCaseAsync "use"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
                        use d = TestHelpers.makeDisposable ()
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use!"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
                        use! d =
                            TestHelpers.makeDisposable ()
                            |> async.Return

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }

                testCaseAsync "use async"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
                        use d = TestHelpers.makeAsyncDisposable ()

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! async"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
                        use! d =
                            TestHelpers.makeAsyncDisposable ()
                            |> async.Return

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual = cancellableValueTask {
                        use d = null
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
            ]

            testList "While" [
                testCaseAsync "while to 10"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual = cancellableValueTask {
                        while index < loops do
                            index <- index + 1

                        return index
                    }

                    Expect.equal actual loops "Should be ok"
                }
                testCaseAsync "while to 1000000"
                <| async {
                    let loops = 1000000
                    let mutable index = 0

                    let! actual = cancellableValueTask {
                        while index < loops do
                            index <- index + 1

                        return index
                    }

                    Expect.equal actual loops "Should be ok"
                }
            ]

            testList "For" [
                testCaseAsync "for in"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual = cancellableValueTask {
                        for i in [ 1..10 ] do
                            index <- i + i

                        return index
                    }

                    Expect.equal actual index "Should be ok"
                }


                testCaseAsync "for to"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual = cancellableValueTask {
                        for i = 1 to loops do
                            index <- i + i

                        return index
                    }

                    Expect.equal actual index "Should be ok"
                }
            ]

            testList "MergeSources" [
                testCaseAsync "and! 6"
                <| async {
                    let! actual = cancellableValueTask {
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
                    let fooTask = cancellableValueTask {
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
                    do!
                        Expect.CancellationRequested(
                            cancellableValueTask {
                                let fooTask = cancellableValueTask {
                                    return! cancellableValueTask {
                                        do! cancellableValueTask {
                                            let! ct = CancellableValueTask.getCancellationToken ()
                                            do! Task.Delay(1000, ct)
                                        }
                                    }
                                }

                                use cts = new CancellationTokenSource()
                                cts.CancelAfter(100)
                                do! fooTask cts.Token
                            }
                        )

                }

                testCaseAsync "pass along CancellationToken to async bind"
                <| async {

                    let fooTask = cancellableValueTask {
                        let! result = async {
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
                    let innerTask = cancellableValueTask {
                        return! CancellableValueTask.getCancellationToken ()
                    }

                    let outerAsync = async {
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

                    let outerAsync = async {
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
                let innerTask = cancellableValueTask {
                    return! CancellableValueTask.getCancellationToken ()
                }

                let outerAsync = async {
                    let! result = innerTask
                    return result
                }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableValueTask<T>"
            <| fun () ->
                let innerTask = cancellableValueTask {
                    return! CancellableValueTask.getCancellationToken ()
                }

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
                        |> CancellableValueTask.bind (fun x -> cancellableValueTask {
                            return x + "fooo"
                        }
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

    [<Tests>]
    let cancellationTaskTests =
        testList "IcedTasks.CancellableValueTask" [
            builderTests
            asyncBuilderTests
            functionTests
        ]
