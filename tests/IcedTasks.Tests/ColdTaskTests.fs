namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks

module ColdTaskTests =
    open System.Threading

    [<Tests>]
    let coldTaskTests =
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
