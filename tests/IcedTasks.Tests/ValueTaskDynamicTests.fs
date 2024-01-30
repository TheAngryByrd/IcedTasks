namespace IcedTasks.Tests

#nowarn "3511"

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
open System.Collections.Generic

module ValueTaskDynamicTests =
    open System.Runtime.CompilerServices

    type ValueTaskDynamicBuilder() =
        inherit ValueTaskBuilder()

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member _.Run(code) = base.Run(code)


    let dValueTask = ValueTaskDynamicBuilder()

    let builderTests =
        testList "ValueTaskBuilderDynamic" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = dValueTask { return "lol" }

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
                    let outerTask = dValueTask { return! fooTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitValueTask

                }
                testCaseAsync "Can ReturnFrom ValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = dValueTask { return! fooTask }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = dValueTask { return! Task.CompletedTask }
                    // Compiling is sufficient expect
                    do!
                        outerTask
                        |> Async.AwaitValueTask

                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = dValueTask { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = dValueTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = dValueTask { return! fooTask }


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
                    let outerTask = dValueTask { do! fooTask }

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
                        dValueTask {
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
                    let outerTask = dValueTask { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask =
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                                dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                        dValueTask {
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
                                dValueTask {
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
                                dValueTask {
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
                                dValueTask {
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
                                dValueTask {
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
                                dValueTask {
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
                                dValueTask {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

#if TEST_NETSTANDARD2_1 || TEST_NET6_0_OR_GREATER
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
                                FSharp.Control.TaskSeq.initAsync
                                    loops
                                    (fun i ->
                                        task {
                                            do! Task.Yield()
                                            return i
                                        }
                                    )

                            let! actual =
                                dValueTask {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )
#endif
            ]
            testList "MergeSources" [
                testCaseAsync "and! 5"
                <| async {
                    let! actual =
                        dValueTask {
                            let! a = ValueTask.FromResult 1
                            and! b = Task.FromResult 2
                            and! c = coldTask { return 3 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            return a + b + c
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual 6 ""

                }
            ]
        ]


    [<Tests>]
    let tests = testList "IcedTasks.ValueTask" [ builderTests ]
