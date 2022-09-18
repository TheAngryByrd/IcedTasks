namespace IcedTasks.Tests

open System
open IcedTasks
open Expecto

module ParallelAsyncTests =

    [<Tests>]
    let tests =
        testList "ParallelAsync" [
            testList "Return" [
                testCaseAsync "return"
                <| async {
                    let data = 42
                    let! actual = parallelAsync { return data }
                    Expect.equal actual data "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "return!"
                <| async {
                    let data = 42
                    let! actual = parallelAsync { return! async.Return data }
                    Expect.equal actual data "Should be able to Return! value"
                }
            ]

            testList "Binds" [
                testCaseAsync "let!"
                <| async {
                    let data = 42

                    let! actual = parallelAsync {

                        let! someValue = async.Return data
                        return someValue
                    }

                    Expect.equal actual data "Should be able to Return! value"
                }
                testCaseAsync "do!"
                <| async { do! parallelAsync { do! async.Return() } }

            ]
            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual = parallelAsync {
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

                    let! actual = parallelAsync {
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

                    let! actual = parallelAsync {
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

                    let! actual = parallelAsync {
                        use d = TestHelpers.makeDisposable ()
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "use!"
                <| async {
                    let data = 42

                    let! actual = parallelAsync {
                        use! d =
                            TestHelpers.makeDisposable ()
                            |> async.Return

                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual = parallelAsync {
                        use d = null
                        return data
                    }

                    Expect.equal actual data "Should be able to use use"
                }
            ]

            testList "While" [
                testCaseAsync "while"
                <| async {
                    let loops = 10
                    let mutable index = 0

                    let! actual = parallelAsync {
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

                    let! actual = parallelAsync {
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

                    let! actual = parallelAsync {
                        for i = 1 to loops do
                            index <- i + i

                        return index
                    }

                    Expect.equal actual index "Should be ok"
                }
            ]

            testList "MergeSources" [
                testCaseAsync "and!"
                <| async {
                    let data = 42

                    let! actual = parallelAsync {
                        let! r1 = async.Return data
                        and! r2 = async.Return data
                        and! r3 = async.Return data

                        return
                            r1
                            + r2
                            + r3
                    }

                    Expect.equal actual 126 "and! works"
                }
            ]


        ]
