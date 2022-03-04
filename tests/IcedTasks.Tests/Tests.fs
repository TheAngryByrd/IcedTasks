namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks
open IcedTasks.ColdTaskBuilder
open IcedTasks.ColdTaskBuilderExtensions
open IcedTasks.CancellableTaskBuilder
open IcedTasks.CancellableTaskBuilderExtensions

type Expect =

    static member CancellationRequested (asyncf : Async<_>) =
        async {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e ->
                ()
            | :? OperationCanceledException as e ->
                ()
        }

    static member CancellationRequested (asyncf : Task<_>) =
        task {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e ->
                ()
            | :? OperationCanceledException as e ->
                ()
        }

    static member CancellationRequested (asyncf : ColdTask<_>) =
        coldTask {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e ->
                ()
            | :? OperationCanceledException as e ->
                ()
        }


    static member CancellationRequested (asyncf : CancellableTask<_>) =
        cancellableTask {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e ->
                ()
            | :? OperationCanceledException as e ->
                ()
        }

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
        testList
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
                    do! Expect.CancellationRequested(async {

                        let foo = cancellableTask {
                            return "lol"
                        }
                        use cts = new CancellationTokenSource()
                        cts.Cancel()
                        let! result = foo cts.Token  |> Async.AwaitTask

                        Expect.equal result "lol" ""
                    })
                }


                testCaseAsync "simple can canel" <| async {

                    let mutable someValue = null
                    do! Expect.CancellationRequested(async {
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
                    })


                    Expect.equal someValue null ""
                }
                testCaseAsync "pass along CancellableTask.getCancellationToken " <| async {
                    let fooTask = cancellableTask {
                        let! ct = CancellableTask.getCancellationToken
                        return ct
                    }
                    use cts = new CancellationTokenSource()
                    let! result = fooTask cts.Token |> Async.AwaitTask
                    Expect.equal result cts.Token ""
                }

                testCaseAsync "pass along deep CancellableTask.getCancellationToken " <| async {
                    do!
                        Expect.CancellationRequested( async {
                            let fooTask = cancellableTask {
                                return! cancellableTask {
                                    do! cancellableTask {
                                        let! ct = CancellableTask.getCancellationToken
                                        do! Task.Delay(10000,ct)
                                        }
                                }
                            }
                            use cts = new CancellationTokenSource()
                            cts.CancelAfter(100)
                            do! fooTask cts.Token |> Async.AwaitTask
                        }
                        )

                }

                testCaseAsync "pass along CancellationToken to async bind" <| async {

                    let fooTask = cancellableTask {
                        let! result = async {
                            let! ct = Async.CancellationToken
                            return ct
                        }
                        return result
                    }
                    use cts = new CancellationTokenSource()
                    let! passedct = fooTask cts.Token |> Async.AwaitTask
                    Expect.equal passedct cts.Token ""
                }
            ]
