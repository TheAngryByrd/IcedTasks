namespace IcedTasks.Tests

open System
open Expecto
open System.Threading.Tasks
open IcedTasks
open IcedTasks.Polyfill.Task

module TaskTests =
    open System.Collections.Generic

    let builderTests =
        testList "TaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = task { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]

            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = task { return! fooTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = task { return! fooTask }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = task { return! Task.CompletedTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitTask

                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = task { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = task { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = task { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind ValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = task { do! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected

                    let outerTask =
                        task {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = task { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask =
                        task {
                            let! result = Task.FromResult expected
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask =
                        task {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Type inference"
                <| async {
                    let expected = "lol"

                    let outerTask fooTask =
                        task {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask (Task.FromResult expected)
                        |> Async.AwaitTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual =
                        task {
                            let result = data

                            if true then
                                ()

                            return result
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Zero/Combine/Delay should work"
                }
            ]

            testList "TryWith" [
                testCaseAsync "try with"
                <| async {
                    let data = 42

                    let! actual =
                        task {
                            let data = data

                            try
                                ()
                            with _ ->
                                ()

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "TryWith should work"
                }
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let! actual =
                        task {
                            let data = data

                            try
                                ()
                            finally
                                ()

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "TryFinally should work"
                }
            ]

            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42

                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        task {
                            use d = TestHelpers.makeDisposable (doDispose)
                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        task {
                            use! d =
                                TestHelpers.makeDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        task {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        task {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable propagate exception"
                <| async {
                    let doDispose () =
                        task {
                            do! Task.Yield()
                            failwith "boom"
                        }
                        |> ValueTask

                    do!
                        Expect.throwsTask<Exception>
                            (fun () ->
                                task {
                                    use d = TestHelpers.makeAsyncDisposable (doDispose)
                                    return ()
                                }
                            )
                            ""
                        |> Async.AwaitTask
                }


                testCaseAsync "use IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        task {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            do! Task.Yield()
                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        task {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        task {
                            use d = null
                            return data
                        }
                        |> Async.AwaitTask

                    Expect.equal actual data "Should be able to use use"
                }
            ]


            testList "While" [
                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )
            ]

            testList "For" [

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for in {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind in {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                task {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )
                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"IAsyncEnumerable for in {loops}"
                        <| async {
                            let mutable index = 0

                            let asyncSeq: IAsyncEnumerable<_> =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (fun _ -> valueTaskUnit { do! Task.Yield() })

                            let! actual =
                                task {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

            ]

            testSequencedGroup "MergeSources"
            <| testList "MergeSources" [

                testCaseAsync "and! task x task"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = task { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableT"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = valueTask { return 1 }
                            and! b = valueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableUnit"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = valueTask { return 2 }
                            and! _ = Task.Yield()
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! awaitableUnit x awaitableT "
                <| asyncEx {
                    let! actual =
                        task {
                            let! _ = Task.Yield()
                            and! a = valueTask { return 2 }
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! 5 random"
                <| asyncEx {
                    let! actual =
                        task {
                            let! a = ValueTask.FromResult 1
                            and! b = Task.FromResult 2
                            and! c = coldTask { return 3 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            return a + b + c
                        }

                    Expect.equal actual 6 ""
                }

                testProperty "parallelism"
                <| fun () ->
                    asyncEx {
                        let doOtherStuff () =
                            task {
                                do! Task.Yield()
                                do! Task.Delay(15)
                                let dt = DateTimeOffset.UtcNow
                                return dt
                            }

                        let! sequenced =
                            task {
                                let! a = doOtherStuff ()
                                let! b = doOtherStuff ()
                                let! c = doOtherStuff ()
                                let! d = doOtherStuff ()
                                let! e = doOtherStuff ()
                                let! f = doOtherStuff ()

                                return [
                                    a
                                    b
                                    c
                                    d
                                    e
                                    f
                                ]
                            }

                        let! paralleled =
                            task {
                                let! a = doOtherStuff ()
                                and! b = doOtherStuff ()
                                and! c = doOtherStuff ()
                                and! d = doOtherStuff ()
                                and! e = doOtherStuff ()
                                and! f = doOtherStuff ()

                                return [
                                    a
                                    b
                                    c
                                    d
                                    e
                                    f
                                ]
                            }

                        let maxSeq =
                            sequenced
                            |> List.maxBy (fun x -> x.Ticks)

                        let minSeq =
                            sequenced
                            |> List.minBy (fun x -> x.Ticks)

                        let maxPar =
                            paralleled
                            |> List.maxBy (fun x -> x.Ticks)

                        let minPar =
                            paralleled
                            |> List.minBy (fun x -> x.Ticks)

                        let diffSeq =
                            maxSeq
                            - minSeq

                        let diffPar =
                            maxPar
                            - minPar

                        return diffPar < diffSeq
                    }
                    |> Async.RunSynchronously
            ]
        ]


    [<Tests>]
    let tests = testList "IcedTasks.Polyfill.Task" [ builderTests ]
