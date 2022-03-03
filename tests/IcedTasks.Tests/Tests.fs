namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks
open IcedTasks.ColdTaskBuilder
open IcedTasks.ColdTaskBuilderExtensions
open IcedTasks.CancellableTaskBuilder
open IcedTasks.CancellableTaskBuilderExtensions
module SayTests =
    open System.Threading

    [<Tests>]
    let tests =
        testList
            "IcedTasks.ColdTaskBuilder"
            [
                testCaseAsync "simple result" <| async {
                    let foo = coldTask {
                        return "lol"
                    }
                    let! result = foo  |> Async.AwaitColdTask

                    Expect.equal result "lol" ""
                }
                testCaseAsync "run immediately" <| async {
                    let mutable someValue = null
                    let foo = task {
                        someValue <- "lol"
                    }

                    do! Async.Sleep(100)

                    Expect.equal someValue "lol" ""
                }
                testCaseAsync "wont run immediately" <| async {
                    let mutable someValue = null
                    let fooColdTask = coldTask {
                        someValue <- "lol"
                    }
                    do! Async.Sleep(100)
                    Expect.equal someValue null ""
                    let fooAsync = fooColdTask |> Async.AwaitColdTask
                    do! Async.Sleep(100)
                    Expect.equal someValue null ""

                    do! fooAsync

                    Expect.equal someValue "lol" ""
                }

                testCaseAsync "can bind with async" <| async {
                    let innerCall = async {
                        return "lol"
                    }
                    let foo = coldTask {
                        let! result = innerCall
                        return "lmao" + result
                    }
                    let! result = foo |> Async.AwaitColdTask

                    Expect.equal result "lmaolol" ""
                }


                testCaseAsync "can bind with task" <| async {

                    let foo = coldTask {
                        let! result = task {
                            return "lol"
                        }
                        return "lmao" + result
                    }
                    let! result = foo |> Async.AwaitColdTask

                    Expect.equal result "lmaolol" ""
                }

                testCaseAsync "wont run immediately with binding innerTask" <| async {
                    let mutable someValue = null
                    let fooColdTask = coldTask {
                        do! task {
                            someValue <- "lol"
                        }
                    }
                    do! Async.Sleep(100)
                    Expect.equal someValue null ""
                    let fooAsync = fooColdTask |> Async.AwaitColdTask
                    do! Async.Sleep(100)
                    Expect.equal someValue null ""

                    do! fooAsync

                    Expect.equal someValue "lol" ""
                }
                testCaseAsync "can bind with coldtask" <| async {

                    let foo = coldTask {
                        let! result = coldTask {
                            return "lol"
                        }
                        return "lmao" + result
                    }
                    let! result = foo () |> Async.AwaitTask

                    Expect.equal result "lmaolol" ""
                }

                testCaseAsync "can ReturnFrom with coldtask" <| async {
                    // ct.ThrowIfCancellationRequested
                    let foo = coldTask {
                        return! coldTask {
                            return "lol"
                        }
                    }
                    let! result = foo () |> Async.AwaitTask

                    Expect.equal result "lol" ""
                }
            ]


    [<Tests>]
    let tests2 =
        ftestList
            "IcedTasks.CancellableTaskBuilder"
            [
                testCaseAsync "simple result" <| async {
                    let foo = cancellableTask {
                        return "lol"
                    }
                    let! result = foo  |> Async.AwaitCancellableTask

                    Expect.equal result "lol" ""
                }


                testCaseAsync "simple result ct" <| async {
                    try
                        let foo = cancellableTask {
                            return "lol"
                        }
                        use cts = new CancellationTokenSource()
                        cts.Cancel()
                        let! result = foo cts.Token  |> Async.AwaitTask

                        Expect.equal result "lol" ""
                    with
                    | :? TaskCanceledException as e ->
                        // printfn "%A" e
                        ()
                    | :? OperationCanceledException as e ->
                        // printfn "%A" e
                        ()
                }


                testCaseAsync "simple can canel" <| async {

                    let mutable someValue = null
                    try
                        let fooColdTask = cancellableTask {
                            someValue <- "lol"
                        }
                        do! Async.Sleep(100)
                        Expect.equal someValue null ""
                        use cts = new CancellationTokenSource()
                        cts.Cancel()
                        let fooAsync = fooColdTask cts.Token |> Async.AwaitTask
                        do! Async.Sleep(100)
                        Expect.equal someValue null ""

                        do! fooAsync

                        Expect.equal someValue "lol" ""
                    with
                    | :? TaskCanceledException as e ->
                        // printfn "%A" e
                        ()
                    | :? OperationCanceledException as e ->
                        // printfn "%A" e
                        ()

                    Expect.equal someValue null ""
                }


                testCaseAsync "pass along CancellationToken to async bind" <| async {
                    let mutable passedct = CancellationToken.None

                    let fooTask = cancellableTask {
                        let! result = async {
                            let! ct = Async.CancellationToken
                            passedct <- ct
                            return "lol"
                        }
                        return result
                    }
                    use cts = new CancellationTokenSource()
                    let! _ = fooTask cts.Token |> Async.AwaitTask
                    Expect.equal passedct cts.Token ""
                }
            ]
