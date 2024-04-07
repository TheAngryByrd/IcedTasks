namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks

module ValueTaskTests =
    open System.Collections.Generic

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
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitValueTask

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
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitValueTask

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

                    let outerTask =
                        valueTask {
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

                    let outerTask =
                        valueTask {
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

                    let outerTask =
                        valueTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

                testCaseAsync "Can Bind Type inference"
                <| async {
                    let expected = "lol"

                    let outerTask fooTask =
                        valueTask {
                            let! result = fooTask
                            return result
                        }

                    let! actual =
                        outerTask (ValueTask.FromResult expected)
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

                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        valueTask {
                            use d = TestHelpers.makeDisposable (doDispose)
                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        valueTask {
                            use! d =
                                TestHelpers.makeDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

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
                        valueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)

                            return data
                        }
                        |> Async.AwaitValueTask

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
                        valueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

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
                        Expect.throwsValueTask<Exception>
                            (fun () ->
                                valueTask {
                                    use d = TestHelpers.makeAsyncDisposable (doDispose)
                                    return ()
                                }
                            )
                            ""
                        |> Async.AwaitValueTask
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
                        valueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitValueTask

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
                        valueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
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
                                valueTask {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

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
                                valueTask {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

            ]

            testList "MergeSources" [
                testCaseAsync "and! task x task"
                <| asyncEx {
                    let! actual =
                        valueTask {
                            let! a = task { return 1 }
                            and! b = task { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableT"
                <| asyncEx {
                    let! actual =
                        valueTask {
                            let! a = valueTask { return 1 }
                            and! b = valueTask { return 2 }
                            return a + b
                        }

                    Expect.equal actual 3 ""
                }

                testCaseAsync "and! awaitableT x awaitableUnit"
                <| asyncEx {
                    let! actual =
                        valueTask {
                            let! a = valueTask { return 2 }
                            and! _ = Task.Yield()
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! awaitableUnit x awaitableT "
                <| asyncEx {
                    let! actual =
                        valueTask {
                            let! _ = Task.Yield()
                            and! a = valueTask { return 2 }
                            return a
                        }

                    Expect.equal actual 2 ""
                }

                testCaseAsync "and! 5 random"
                <| asyncEx {
                    let! actual =
                        valueTask {
                            let! a = ValueTask.FromResult 1
                            and! b = Task.FromResult 2
                            and! c = coldTask { return 3 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            return a + b + c
                        }

                    Expect.equal actual 6 ""
                }
            ]

            testList "MergeSourcesParallel" [
                testPropertyWithConfig Expecto.fsCheckConfig "parallelism"
                <| fun () ->
                    asyncEx {
                        let! ct = Async.CancellationToken
                        let sequencedList = ResizeArray<_>()
                        let parallelList = ResizeArray<_>()


                        let fakeWork id yieldTimes (l: ResizeArray<_>) =
                            valueTask {
                                lock l (fun () -> l.Add(id))
                                do! Task.yieldMany yieldTimes
                                let dt = DateTimeOffset.UtcNow
                                lock l (fun () -> l.Add(id))
                                return dt
                            }

                        // Have earlier tasks take longer to complete
                        // so we can see if they are sequenced or not
                        let fakeWork1 = fakeWork 1 10000
                        let fakeWork2 = fakeWork 2 750
                        let fakeWork3 = fakeWork 3 500
                        let fakeWork4 = fakeWork 4 250
                        let fakeWork5 = fakeWork 5 100
                        let fakeWork6 = fakeWork 6 1

                        let! sequenced =
                            valueTask {
                                let! a = fakeWork1 sequencedList
                                let! b = fakeWork2 sequencedList
                                let! c = fakeWork3 sequencedList
                                let! d = fakeWork4 sequencedList
                                let! e = fakeWork5 sequencedList
                                let! f = fakeWork6 sequencedList

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
                            valueTask {
                                let! a = fakeWork1 parallelList
                                and! b = fakeWork2 parallelList
                                and! c = fakeWork3 parallelList
                                and! d = fakeWork4 parallelList
                                and! e = fakeWork5 parallelList
                                and! f = fakeWork6 parallelList

                                return [
                                    a
                                    b
                                    c
                                    d
                                    e
                                    f
                                ]
                            }

                        let sequencedEntrances =
                            sequencedList
                            |> Seq.toList

                        let parallelEntrances =
                            parallelList
                            |> Seq.toList

                        let sequencedAlwaysOrdered =
                            sequencedEntrances = [
                                1
                                1
                                2
                                2
                                3
                                3
                                4
                                4
                                5
                                5
                                6
                                6
                            ]

                        let parallelNotSequenced =
                            parallelEntrances
                            <> sequencedEntrances

                        return
                            sequencedAlwaysOrdered
                            && parallelNotSequenced
                    }
                    |> Async.RunSynchronously
            ]
        ]

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
    let tests =
        testList "IcedTasks.ValueTask" [
            builderTests
            functionTests
        ]
