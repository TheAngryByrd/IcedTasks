namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks


module ValueTaskTests =
    open System.Threading

    let builderTests =
        testList "ValueTaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = valueTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitValueTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = valueTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = valueTask { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = valueTask { return! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = valueTask { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = valueTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = valueTask { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = valueTask { do! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected

                    let outerTask = valueTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = valueTask { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask = valueTask {
                        let! result = Task.FromResult expected
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask = valueTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            let result = data

                            if true then
                                ()

                            return result
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Zero/Combine/Delay should work"
                }
            ]

            testList "TryWith" [
                testCaseAsync "try with"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            let data = data

                            try
                                ()
                            with _ ->
                                ()

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "TryWith should work"
                }
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            let data = data

                            try
                                ()
                            finally
                                ()

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "TryFinally should work"
                }
            ]

            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            use d = TestHelpers.makeDisposable ()
                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            use! d =
                                TestHelpers.makeDisposable ()
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }


                testCaseAsync "use IAsyncDisposable"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            use d = TestHelpers.makeAsyncDisposable ()

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use! IAsyncDisposable"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable ()
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        valueTask {
                            use d = null
                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }
            ]


            testList "While" [
                testCaseAsync "while to 10"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual =
                        valueTask {
                            while index < loops do
                                index <- index + 1

                            return index
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual loops "Should be ok"
                }
                testCaseAsync "while to 1000000"
                <| async {
                    let loops = 1000000
                    let mutable index = 0

                    let! actual =
                        valueTask {
                            while index < loops do
                                index <- index + 1

                            return index
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual loops "Should be ok"
                }
            ]

            testList "For" [
                testCaseAsync "for in"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual =
                        valueTask {
                            for i in [ 1..10 ] do
                                index <- i + i

                            return index
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual index "Should be ok"
                }


                testCaseAsync "for to"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual =
                        valueTask {
                            for i = 1 to loops do
                                index <- i + i

                            return index
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual index "Should be ok"
                }
            ]
        ]


    // let asyncBuilderTests =
    //     testList "AsyncBuilder" [

    //         testCase "AsyncBuilder can Bind ValueTask<T>"
    //         <| fun () ->
    //             let innerTask = valueTask { return! valueTask { return "lol" } }

    //             let outerAsync = async {
    //                 let! result = innerTask |> Async.AwaitValueTask
    //                 return result
    //             }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual "lol" ""


    //         testCase "AsyncBuilder can ReturnFrom ValueTask<T>"
    //         <| fun () ->
    //             let innerTask = valueTask { return! valueTask { return "lol" } }

    //             let outerAsync = async { return! innerTask }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual "lol" ""

    //         testCase "AsyncBuilder can Bind ValueTask"
    //         <| fun () ->
    //             let innerTask: ValueTask = fun () -> Task.CompletedTask

    //             let outerAsync = async {
    //                 let! result = innerTask
    //                 return result
    //             }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual () ""

    //         testCase "AsyncBuilder can ReturnFrom ValueTask"
    //         <| fun () ->
    //             let innerTask: ValueTask = fun () -> Task.CompletedTask

    //             let outerAsync = async { return! innerTask }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual () ""
    //     ]


    let functionTests =
        testList "functions" [
            testList "singleton" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.singleton "lol"

                    let! someTask =
                        innerCall
                        |> Async.AwaitValueTask

                    Expect.equal "lol" someTask ""
                }
            ]
            testList "bind" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = valueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ValueTask.bind (fun x -> valueTask { return x + "fooo" })
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "map" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = valueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ValueTask.map (fun x -> x + "fooo")
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "apply" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = valueTask { return "lol" }
                    let applier = valueTask { return fun x -> x + "fooo" }

                    let! someTask =
                        innerCall
                        |> ValueTask.apply applier
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]

            testList "zip" [
                testCaseAsync "Simple"
                <| async {
                    let leftCall = valueTask { return "lol" }
                    let rightCall = valueTask { return "fooo" }

                    let! someTask =
                        ValueTask.zip leftCall rightCall
                        |> Async.AwaitValueTask

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]


            testList "ofUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.CompletedTask

                    let! someTask =
                        innerCall
                        |> ValueTask.ofUnit
                        |> Async.AwaitValueTask

                    Expect.equal () someTask ""
                }
            ]


            testList "toUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.FromResult "lol"

                    let! someTask =
                        innerCall
                        |> ValueTask.toUnit
                        |> Async.AwaitValueTask

                    Expect.equal () someTask ""
                }
            ]

        ]


    [<Tests>]
    let valueTaskTests =
        testList "IcedTasks.ValueTask" [
            builderTests
            // asyncBuilderTests
            functionTests
        ]
