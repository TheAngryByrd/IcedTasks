namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks

module CancellableTaskHelpers =
    let map (mapper: 'a -> 'b) (item: CancellableTask<'a>) : CancellableTask<'b> = cancellableTask {
        let! i = item
        return mapper i
    }

module CancellableTaskTests =


    [<Tests>]
    let cancellationTaskTests =
        testList "IcedTasks.CancellableTaskBuilder" [
            testCaseAsync "Simple Return"
            <| async {
                let foo = cancellableTask { return "lol" }

                let! result =
                    foo
                    |> Async.AwaitCancellableTask

                Expect.equal result "lol" ""
            }

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
                    let! ct = CancellableTask.getCancellationToken
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
                                        let! ct = CancellableTask.getCancellationToken
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
            testCaseAsync "Can ReturnFrom CancellableTask"
            <| async {
                let fooTask: CancellableTask = fun ct -> Task.FromResult()
                let outerTask = cancellableTask { return! fooTask }
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

            testCaseAsync "Can ReturnFrom CancellableTask<T>"
            <| async {
                let expected = "lol"
                let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected
                let outerTask = cancellableTask { return! fooTask }
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
            testCaseAsync "Can ReturnFrom Task"
            <| async {
                let outerTask = cancellableTask { return! Task.FromResult() }
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
            testCaseAsync "Can ReturnFrom Task<T>"
            <| async {
                let expected = "lol"
                let outerTask = cancellableTask { return! Task.FromResult expected }
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
            testCaseAsync "Can ReturnFrom ColdTask<T>"
            <| async {
                let expected = "lol"
                let coldT = coldTask { return expected }
                let outerTask = cancellableTask { return! coldT }
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
            testCaseAsync "Can ReturnFrom ColdTask"
            <| async {
                let coldT: ColdTask = fun () -> Task.FromResult()
                let outerTask = cancellableTask { return! coldT }
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

            testCase
                "CancellationToken flows from Async<unit> to CancellableTask<T> via Async.AwaitCancellableTask"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken }

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


            testCase "AsyncBuilder can Bind CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken }

                let outerAsync = async {
                    let! result = innerTask
                    return result
                }

                use cts = new CancellationTokenSource()
                let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                Expect.equal actual cts.Token ""


            testCase "AsyncBuilder can ReturnFrom CancellableTask<T>"
            <| fun () ->
                let innerTask = cancellableTask { return! CancellableTask.getCancellationToken }
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

            testCaseAsync "Generic cancellableTask parameter"
            <| async {
                let innerCall = cancellableTask { return "lol" }

                let! someTask =
                    innerCall
                    |> CancellableTaskHelpers.map (fun x -> x + "fooo")

                Expect.equal "lolfooo" someTask ""
            }
        ]
