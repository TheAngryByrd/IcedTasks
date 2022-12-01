namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks

module ColdTaskHelpers =
    let map (mapper: 'a -> 'b) (item: ColdTask<'a>) : ColdTask<'b> = coldTask {
        let! i = item
        return mapper i
    }

module ColdTaskTests =
    open System.Threading

    let builderTests =
        testList "ColdTaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = coldTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitColdTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom ColdTask"
                <| async {
                    let fooTask: ColdTask = fun () -> Task.CompletedTask
                    let outerTask = coldTask { return! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom ColdTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ColdTask<_> = fun () -> Task.FromResult expected
                    let outerTask = coldTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = coldTask { return! Task.CompletedTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = coldTask { return! Task.FromResult expected }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }


                testCaseAsync "Can ReturnFrom ValueTask"
                <| async {
                    let fooTask = ValueTask.CompletedTask

                    let outerTask = coldTask { return! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask

                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = ValueTask.FromResult expected

                    let outerTask = coldTask { return! fooTask }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can ReturnFrom Cold ValueTask"
                <| async {
                    let fooTask = fun () -> ValueTask.CompletedTask

                    let outerTask = coldTask { return! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask

                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can ReturnFrom Cold ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = fun () -> ValueTask.FromResult expected

                    let outerTask = coldTask { return! fooTask }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = coldTask { return! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()
                    let outerTask = coldTask { return! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = coldTask { return! fooTask }
                    use cts = new CancellationTokenSource()

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind ColdTask"
                <| async {
                    let fooTask: ColdTask = fun () -> Task.CompletedTask
                    let outerTask = coldTask { do! fooTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ColdTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ColdTask<_> = fun () -> Task.FromResult expected

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = coldTask { do! Task.CompletedTask }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask = coldTask {
                        let! result = Task.FromResult expected
                        return result
                    }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind ValueTask"
                <| async {
                    let fooTask = ValueTask.CompletedTask

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    do!
                        outerTask ()
                        |> Async.AwaitTask

                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = ValueTask.FromResult expected

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind ColdValueTask"
                <| async {
                    let fooTask = fun () -> ValueTask.CompletedTask

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    do!
                        outerTask ()
                        |> Async.AwaitTask

                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ColdValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = fun () -> ValueTask.FromResult expected

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind cold TaskLike"
                <| async {
                    let fooTask = fun () -> Task.Yield()

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    do!
                        outerTask ()
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask = coldTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask ()
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
                        use d = TestHelpers.makeDisposable ()
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42

                    let! actual = coldTask {
                        use! d =
                            TestHelpers.makeDisposable ()
                            |> async.Return

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }


                testCaseAsync "use IAsyncDisposable"
                <| async {
                    let data = 42

                    let! actual = coldTask {
                        use d = TestHelpers.makeAsyncDisposable ()

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! IAsyncDisposable"
                <| async {
                    let data = 42

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
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

                    let! actual = coldTask {
                        for i = 1 to loops do
                            index <- i + i

                        return index
                    }

                    Expect.equal actual index "Should be ok"
                }
            ]

            // testList "MergeSources" [
            //     testCaseAsync "and! "
            //     <| async {
            //         let foo = coldTask {
            //             let! r1 = Task.FromResult(1)
            //             and! r2 = Task.FromResult(2)
            //             return r1 + r2
            //         }

            //         let! actual =
            //             foo
            //             |> Async.AwaitColdTask

            //         Expect.equal actual 1 ""
            //     }
            // ]

            testList "Cold Semantics" [

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

                    let fooAsync =
                        fooColdTask
                        |> Async.AwaitColdTask

                    do! Async.Sleep(100)
                    Expect.equal someValue null ""

                    do! fooAsync

                    Expect.equal someValue "lol" ""
                }

                testCaseAsync "wont run immediately with binding innerTask"
                <| async {
                    let mutable someValue = null
                    let fooColdTask = coldTask { do! task { someValue <- "lol" } }
                    do! Async.Sleep(100)
                    Expect.equal someValue null ""

                    let fooAsync =
                        fooColdTask
                        |> Async.AwaitColdTask

                    do! Async.Sleep(100)
                    Expect.equal someValue null ""

                    do! fooAsync

                    Expect.equal someValue "lol" ""
                }

                testCaseAsync "Multi start task"
                <| async {
                    let values = ResizeArray<_>()
                    let someTask = task { values.Add("foo") }

                    do!
                        someTask
                        |> Async.AwaitTask

                    do!
                        someTask
                        |> Async.AwaitTask

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

                    do!
                        someTask
                        |> Async.AwaitColdTask

                    do!
                        someTask
                        |> Async.AwaitColdTask

                    Expect.hasLength values 2 ""
                }
            ]
        ]


    let asyncBuilderTests =
        testList "AsyncBuilder" [

            testCase "AsyncBuilder can Bind ColdTask<T>"
            <| fun () ->
                let innerTask = coldTask { return! coldTask { return "lol" } }

                let outerAsync = async {
                    let! result = innerTask
                    return result
                }

                let actual = Async.RunSynchronously(outerAsync)
                Expect.equal actual "lol" ""


            testCase "AsyncBuilder can ReturnFrom ColdTask<T>"
            <| fun () ->
                let innerTask = coldTask { return! coldTask { return "lol" } }

                let outerAsync = async { return! innerTask }

                let actual = Async.RunSynchronously(outerAsync)
                Expect.equal actual "lol" ""

            testCase "AsyncBuilder can Bind ColdTask"
            <| fun () ->
                let innerTask: ColdTask = fun () -> Task.CompletedTask

                let outerAsync = async {
                    let! result = innerTask
                    return result
                }

                let actual = Async.RunSynchronously(outerAsync)
                Expect.equal actual () ""

            testCase "AsyncBuilder can ReturnFrom ColdTask"
            <| fun () ->
                let innerTask: ColdTask = fun () -> Task.CompletedTask

                let outerAsync = async { return! innerTask }

                let actual = Async.RunSynchronously(outerAsync)
                Expect.equal actual () ""
        ]


    let taskBuilderTests =
        testList "TaskBuilder" [

            testCase "TaskBuilder can Bind ColdTask<T>"
            <| fun () ->
                let innerTask = coldTask { return! coldTask { return "lol" } }

                let outerAsync = task {
                    let! result = innerTask
                    return result
                }

                let actual = outerAsync.GetAwaiter().GetResult()
                Expect.equal actual "lol" ""


            testCase "TaskBuilder can ReturnFrom ColdTask<T>"
            <| fun () ->
                let innerTask = coldTask { return! coldTask { return "lol" } }

                let outerAsync = task { return! innerTask }

                let actual = outerAsync.GetAwaiter().GetResult()
                Expect.equal actual "lol" ""

            testCase "TaskBuilder can Bind ColdTask"
            <| fun () ->
                let innerTask: ColdTask = fun () -> Task.CompletedTask

                let outerAsync = task {
                    let! result = innerTask
                    return result
                }

                let actual = outerAsync.GetAwaiter().GetResult()
                Expect.equal actual () ""

            testCase "TaskBuilder can ReturnFrom ColdTask"
            <| fun () ->
                let innerTask: ColdTask = fun () -> Task.CompletedTask

                let outerAsync = task { return! innerTask }

                let actual = outerAsync.GetAwaiter().GetResult()
                Expect.equal actual () ""
        ]

    let functionTests =
        testList "functions" [
            testList "singleton" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ColdTask.singleton "lol"

                    let! someTask = innerCall

                    Expect.equal "lol" someTask ""
                }
            ]
            testList "bind" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = coldTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ColdTask.bind (fun x -> coldTask { return x + "fooo" })

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "map" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = coldTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ColdTask.map (fun x -> x + "fooo")

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "apply" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = coldTask { return "lol" }
                    let applier = coldTask { return fun x -> x + "fooo" }

                    let! someTask =
                        innerCall
                        |> ColdTask.apply applier

                    Expect.equal "lolfooo" someTask ""
                }
            ]

            testList "zip" [
                testCaseAsync "Simple"
                <| async {
                    let leftCall = coldTask { return "lol" }
                    let rightCall = coldTask { return "fooo" }

                    let! someTask = ColdTask.zip leftCall rightCall

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "parallelZip" [
                testCaseAsync "Simple"
                <| async {
                    let leftCall = coldTask { return "lol" }
                    let rightCall = coldTask { return "fooo" }

                    let! someTask = ColdTask.parallelZip leftCall rightCall

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]

            testList "ofUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> Task.CompletedTask

                    let! someTask =
                        innerCall
                        |> ColdTask.ofUnit

                    Expect.equal () someTask ""
                }
            ]


            testList "toUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = fun ct -> Task.FromResult "lol"

                    let! someTask =
                        innerCall
                        |> ColdTask.toUnit

                    Expect.equal () someTask ""
                }
            ]

        ]


    [<Tests>]
    let coldTaskTests =
        testList "IcedTasks.ColdTask" [
            builderTests
            asyncBuilderTests
            taskBuilderTests
            functionTests
        ]
