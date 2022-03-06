namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks

type Expect =

    static member CancellationRequested(asyncf: Async<_>) =
        async {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }

    static member CancellationRequested(asyncf: Task<_>) =
        task {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }

    static member CancellationRequested(asyncf: ColdTask<_>) =
        coldTask {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }


    static member CancellationRequested(asyncf: CancellableTask<_>) =
        cancellableTask {
            try
                do! asyncf
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }

module SayTests =
    open System.Threading

    [<Tests>]
    let tests =
        testList
            "IcedTasks.ColdTaskBuilder"
            [ testCaseAsync "ColdTask simple result"
              <| async {
                  let foo = coldTask { return "lol" }
                  let! result = foo |> Async.AwaitColdTask

                  Expect.equal result "lol" ""
              }
              testCaseAsync "Task run immediately"
              <| async {
                  let mutable someValue = null
                  let foo = task { someValue <- "lol" }

                  do! Async.Sleep(100)

                  Expect.equal someValue "lol" ""
              }
              testCaseAsync "ColdTask are lazily evaluated"
              <| async {
                  let mutable someValue = null
                  let fooColdTask = coldTask { someValue <- "lol" }
                  do! Async.Sleep(100)
                  Expect.equal someValue null ""
                  let fooAsync = fooColdTask |> Async.AwaitColdTask
                  do! Async.Sleep(100)
                  Expect.equal someValue null ""

                  do! fooAsync

                  Expect.equal someValue "lol" ""
              }

              testCaseAsync "Can Bind Async<T>"
              <| async {
                  let innerCall = async { return "lol" }

                  let foo =
                      coldTask {
                          let! result = innerCall
                          return "lmao" + result
                      }

                  let! result = foo |> Async.AwaitColdTask

                  Expect.equal result "lmaolol" ""
              }
              testCaseAsync "Can ReturnFrom Async<T>"
              <| async {
                  let innerCall = async { return "lol" }

                  let foo = coldTask { return! innerCall }

                  let! result = foo |> Async.AwaitColdTask

                  Expect.equal result "lol" ""
              }


              testCaseAsync "Can Bind Task<T>"
              <| async {

                  let foo =
                      coldTask {
                          let! result = task { return "lol" }
                          return "lmao" + result
                      }

                  let! result = foo |> Async.AwaitColdTask

                  Expect.equal result "lmaolol" ""
              }
              testCaseAsync "Can ReturnFrom Task<T>"
              <| async {

                  let foo = coldTask { return! task { return "lol" } }

                  let! result = foo |> Async.AwaitColdTask

                  Expect.equal result "lol" ""
              }
              testCaseAsync "Can Bind Task"
              <| async {
                  let foo = coldTask { do! Task.FromResult() }
                  do! foo |> Async.AwaitColdTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Can ReturnFrom Task"
              <| async {
                  let foo = coldTask { return! Task.FromResult() }
                  do! foo |> Async.AwaitColdTask
              // Compiling is a sufficient Expect
              }

              testCaseAsync "wont run immediately with binding innerTask"
              <| async {
                  let mutable someValue = null
                  let fooColdTask = coldTask { do! task { someValue <- "lol" } }
                  do! Async.Sleep(100)
                  Expect.equal someValue null ""
                  let fooAsync = fooColdTask |> Async.AwaitColdTask
                  do! Async.Sleep(100)
                  Expect.equal someValue null ""

                  do! fooAsync

                  Expect.equal someValue "lol" ""
              }
              testCaseAsync "Can Bind ColdTask<T>"
              <| async {

                  let foo =
                      coldTask {
                          let! result = coldTask { return "lol" }
                          return "lmao" + result
                      }

                  let! result = foo () |> Async.AwaitTask

                  Expect.equal result "lmaolol" ""
              }

              testCaseAsync "Can ReturnFrom with Coldtask<T>"
              <| async {
                  // ct.ThrowIfCancellationRequested
                  let foo = coldTask { return! coldTask { return "lol" } }
                  let! result = foo () |> Async.AwaitTask

                  Expect.equal result "lol" ""
              }

              testCaseAsync "Can Bind Coldtask"
              <| async {

                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = coldTask { do! innerCold }
                  do! foo |> Async.AwaitColdTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Can ReturnFrom Coldtask"
              <| async {
                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = coldTask { return! innerCold }
                  do! foo |> Async.AwaitColdTask
              // Compiling is a sufficient Expect
              }


              testCaseAsync "Async Bind Coldtask<T>"
              <| async {
                  let innerCall = coldTask { return "lol" }

                  let foo =
                      async {
                          let! result = innerCall
                          return "lmao" + result
                      }

                  let! result = foo

                  Expect.equal result "lmaolol" ""
              }
              testCaseAsync "Async ReturnFrom Coldtask<T>"
              <| async {
                  let innerCall = coldTask { return "lol" }

                  let foo = async { return! innerCall }

                  let! result = foo

                  Expect.equal result "lol" ""
              }

              testCaseAsync "Async Bind Coldtask"
              <| async {

                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = async { do! innerCold }
                  do! foo
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Async ReturnFrom Coldtask"
              <| async {
                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = async { return! innerCold }
                  do! foo
              // Compiling is a sufficient Expect
              }

              testCaseAsync "Task Bind Coldtask<T>"
              <| async {
                  let innerCall = coldTask { return "lol" }

                  let foo =
                      task {
                          let! result = innerCall
                          return "lmao" + result
                      }

                  let! result = foo |> Async.AwaitTask

                  Expect.equal result "lmaolol" ""
              }
              testCaseAsync "Task ReturnFrom Coldtask<T>"
              <| async {
                  let innerCall = coldTask { return "lol" }

                  let foo = task { return! innerCall }

                  let! result = foo |> Async.AwaitTask

                  Expect.equal result "lol" ""
              }

              testCaseAsync "Task Bind Coldtask"
              <| async {

                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = task { do! innerCold }
                  do! foo |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Task ReturnFrom Coldtask"
              <| async {
                  let innerCold: ColdTask = fun () -> Task.FromResult()
                  let foo = task { return! innerCold }
                  do! foo |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Multi start task"
              <| async {
                  let values = ResizeArray<_>()
                  let someTask = task { values.Add("foo") }
                  do! someTask |> Async.AwaitTask
                  do! someTask |> Async.AwaitTask
                  Expect.hasLength values 1 ""
              }
              testCaseAsync "Multi start async"
              <| async {
                  let values = ResizeArray<_>()
                  let someTask = async { values.Add("foo") }
                  do! someTask
                  do! someTask
                  Expect.hasLength values 2 ""
              }
              testCaseAsync "Multi start coldTask"
              <| async {
                  let values = ResizeArray<_>()
                  let someTask = coldTask { values.Add("foo") }
                  do! someTask |> Async.AwaitColdTask
                  do! someTask |> Async.AwaitColdTask
                  Expect.hasLength values 2 ""
              }

              ]

    [<Tests>]
    let tests2 =
        testList
            "IcedTasks.CancellableTaskBuilder"
            [ testCaseAsync "Simple Return"
              <| async {
                  let foo = cancellableTask { return "lol" }
                  let! result = foo |> Async.AwaitCancellableTask

                  Expect.equal result "lol" ""
              }

              testCaseAsync "Simple Cancellation"
              <| async {
                  do!
                      Expect.CancellationRequested(
                          async {

                              let foo = cancellableTask { return "lol" }
                              use cts = new CancellationTokenSource()
                              cts.Cancel()
                              let! result = foo cts.Token |> Async.AwaitTask

                              Expect.equal result "lol" ""
                          }
                      )
              }


              testCaseAsync "CancellableTasks are lazily evaluated"
              <| async {

                  let mutable someValue = null

                  do!
                      Expect.CancellationRequested(
                          async {
                              let fooColdTask = cancellableTask { someValue <- "lol" }
                              do! Async.Sleep(100)
                              Expect.equal someValue null ""
                              use cts = new CancellationTokenSource()
                              cts.Cancel()
                              let fooAsync = fooColdTask cts.Token |> Async.AwaitTask
                              do! Async.Sleep(100)
                              Expect.equal someValue null ""

                              do! fooAsync

                              Expect.equal someValue "lol" ""
                          }
                      )

                  Expect.equal someValue null ""
              }
              testCaseAsync "Can extract context's CancellationToken via CancellableTask.getCancellationToken"
              <| async {
                  let fooTask =
                      cancellableTask {
                          let! ct = CancellableTask.getCancellationToken
                          return ct
                      }

                  use cts = new CancellationTokenSource()
                  let! result = fooTask cts.Token |> Async.AwaitTask
                  Expect.equal result cts.Token ""
              }

              testCaseAsync
                  "Can extract context's CancellationToken via CancellableTask.getCancellationToken in a deeply nested CE"
              <| async {
                  do!
                      Expect.CancellationRequested(
                          async {
                              let fooTask =
                                  cancellableTask {
                                      return!
                                          cancellableTask {
                                              do!
                                                  cancellableTask {
                                                      let! ct = CancellableTask.getCancellationToken
                                                      do! Task.Delay(1000, ct)
                                                      failwith "Didn't cancel fast enough"
                                                  }
                                          }
                                  }

                              use cts = new CancellationTokenSource()
                              cts.CancelAfter(100)
                              do! fooTask cts.Token |> Async.AwaitTask
                              failwith "Didn't cancel fast enough"
                          }
                      )

              }

              testCaseAsync "pass along CancellationToken to async bind"
              <| async {

                  let fooTask =
                      cancellableTask {
                          let! result =
                              async {
                                  let! ct = Async.CancellationToken
                                  return ct
                              }

                          return result
                      }

                  use cts = new CancellationTokenSource()
                  let! passedct = fooTask cts.Token |> Async.AwaitTask
                  Expect.equal passedct cts.Token ""
              }


              testCaseAsync "Can Bind CancellableTask"
              <| async {
                  let fooTask: CancellableTask = fun ct -> Task.FromResult()
                  let outerTask = cancellableTask { do! fooTask }
                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Can ReturnFrom CancellableTask"
              <| async {
                  let fooTask: CancellableTask = fun ct -> Task.FromResult()
                  let outerTask = cancellableTask { return! fooTask }
                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }

              testCaseAsync "Can Bind CancellableTask<T>"
              <| async {
                  let expected = "lol"
                  let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected

                  let outerTask =
                      cancellableTask {
                          let! result = fooTask
                          return result
                      }

                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }

              testCaseAsync "Can ReturnFrom CancellableTask<T>"
              <| async {
                  let expected = "lol"
                  let fooTask: CancellableTask<_> = fun ct -> Task.FromResult expected
                  let outerTask = cancellableTask { return! fooTask }
                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }


              testCaseAsync "Can Bind Task"
              <| async {
                  let outerTask = cancellableTask { do! Task.FromResult() }
                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Can ReturnFrom Task"
              <| async {
                  let outerTask = cancellableTask { return! Task.FromResult() }
                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }

              testCaseAsync "Can Bind Task<T>"
              <| async {
                  let expected = "lol"

                  let outerTask =
                      cancellableTask {
                          let! result = Task.FromResult expected
                          return result
                      }

                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }
              testCaseAsync "Can ReturnFrom Task<T>"
              <| async {
                  let expected = "lol"
                  let outerTask = cancellableTask { return! Task.FromResult expected }
                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }

              testCaseAsync "Can Bind ColdTask<T>"
              <| async {
                  let expected = "lol"

                  let coldT = coldTask { return expected }

                  let outerTask =
                      cancellableTask {
                          let! result = coldT
                          return result
                      }

                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }
              testCaseAsync "Can ReturnFrom ColdTask<T>"
              <| async {
                  let expected = "lol"
                  let coldT = coldTask { return expected }
                  let outerTask = cancellableTask { return! coldT }
                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }


              testCaseAsync "Can Bind ColdTask"
              <| async {

                  let coldT: ColdTask = fun () -> Task.FromResult()

                  let outerTask =
                      cancellableTask {
                          let! result = coldT
                          return result
                      }

                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }
              testCaseAsync "Can ReturnFrom ColdTask"
              <| async {
                  let coldT: ColdTask = fun () -> Task.FromResult()
                  let outerTask = cancellableTask { return! coldT }
                  use cts = new CancellationTokenSource()
                  do! outerTask cts.Token |> Async.AwaitTask
              // Compiling is a sufficient Expect
              }

              testCaseAsync "Can Bind Async<T>"
              <| async {
                  let expected = "lol"
                  let fooTask = async.Return expected

                  let outerTask =
                      cancellableTask {
                          let! result = fooTask
                          return result
                      }

                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }
              testCaseAsync "Can ReturnFrom Async<T>"
              <| async {
                  let expected = "lol"
                  let fooTask = async.Return expected
                  let outerTask = cancellableTask { return! fooTask }
                  use cts = new CancellationTokenSource()
                  let! actual = outerTask cts.Token |> Async.AwaitTask
                  Expect.equal actual expected ""
              }

              testCase "CancellationToken flows from Async<unit> to CancellableTask<T> via Async.AwaitCancellableTask"
              <| fun () ->
                  let innerTask = cancellableTask { return! CancellableTask.getCancellationToken }
                  let outerAsync = async { return! innerTask |> Async.AwaitCancellableTask }

                  use cts = new CancellationTokenSource()
                  let actual = Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                  Expect.equal actual cts.Token ""

              testCase "CancellationToken flows from Async<unit> to CancellableTask via Async.AwaitCancellableTask"
              <| fun () ->
                  let mutable actual = CancellationToken.None
                  let innerTask: CancellableTask = fun ct -> task { actual <- ct } :> Task
                  let outerAsync = async { return! innerTask |> Async.AwaitCancellableTask }

                  use cts = new CancellationTokenSource()
                  Async.RunSynchronously(outerAsync, cancellationToken = cts.Token)
                  Expect.equal actual cts.Token ""


              testCase "AsyncBuilder can Bind CancellableTask<T>"
              <| fun () ->
                  let innerTask = cancellableTask { return! CancellableTask.getCancellationToken }

                  let outerAsync =
                      async {
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
                  Expect.equal actual cts.Token "" ]
