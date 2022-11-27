namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks

module CancellableTaskTests =

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
                    let fooTask: CancellableTask = fun ct -> Task.FromResult()
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
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = cancellableTask { return! Task.FromResult() }
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
                testCaseAsync "Can ReturnFrom ColdTask"
                <| async {
                    let coldT: ColdTask = fun () -> Task.FromResult()
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
                    let fooTask: CancellableTask = fun ct -> Task.FromResult()
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
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = cancellableTask { do! Task.FromResult() }
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

                    let coldT: ColdTask = fun () -> Task.FromResult()

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
                testCaseAsync "use"
                <| async {
                    let data = 42

                    let! actual = cancellableTask {
                        use d = TestHelpers.makeDisposable ()
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use!"
                <| async {
                    let data = 42

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
                        use d = TestHelpers.makeAsyncDisposable ()

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! async"
                <| async {
                    let data = 42

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
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

                    let! actual = cancellableTask {
                        for i = 1 to loops do
                            index <- i + i

                        return index
                    }

                    Expect.equal actual index "Should be ok"
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
                    do!
                        Expect.CancellationRequested(
                            cancellableTask {
                                let fooTask = cancellableTask {
                                    return! cancellableTask {
                                        do! cancellableTask {
                                            let! ct = CancellableTask.getCancellationToken ()
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

                    let fooTask = cancellableTask {
                        let! result = async {
                            let! ct = Async.CancellationToken
                            return ct
                        }

                        return result
                    }

                    use cts = new CancellationTokenSource()

                    let! passedct =
                        fooTask cts.Token
                        |> Async.AwaitTask

                    Expect.equal passedct cts.Token ""
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
                        |> CancellableTask.parZip innerCall2

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

        ]

    [<Tests>]
    let cancellationTaskTests =
        testList "IcedTasks.CancellableTask" [
            builderTests
            asyncBuilderTests
            functionTests
        ]
