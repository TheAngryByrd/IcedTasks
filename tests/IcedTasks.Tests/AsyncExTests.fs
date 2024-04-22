namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
open System.Collections.Generic


module AsyncExTests =
    open FSharp.Control

    let builderTests =
        testList "AsyncExBuilder" [
            testList "Return" [
                testCaseAsync "Simple return"
                <| async {
                    let data = "foo"
                    let! result = asyncEx { return data }
                    Expect.equal result data "Should return the data"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom an AsyncEx"
                <| async {
                    let data = "foo"
                    let inner = asyncEx { return data }
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can ReturnFrom an Async"
                <| async {
                    let data = "foo"
                    let inner = async { return data }
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result data "Should return the data"
                }

                testCaseAsync "Can ReturnFrom an Task<T>"
                <| async {
                    let data = "foo"
                    let inner = task { return data }
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can ReturnFrom an Task"
                <| async {
                    let inner: Task = Task.CompletedTask
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
                testCaseAsync "Can ReturnFrom a ValueTask<T>"
                <| async {
                    let data = "foo"
                    let inner = valueTask { return data }
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can ReturnFrom a ValueTask"
                <| async {
                    let inner: ValueTask = ValueTask.CompletedTask
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
                testCaseAsync "Can ReturnFrom an TaskLike"
                <| async {
                    let inner = Task.Yield()
                    let outer = asyncEx { return! inner }
                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
            ]
            testList "Bind" [
                testCaseAsync "Can bind an AsyncEx"
                <| async {
                    let data = "foo"
                    let inner = asyncEx { return data }

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can bind an Async"
                <| async {
                    let data = "foo"
                    let inner = async { return data }

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can bind an Task<T>"
                <| async {
                    let data = "foo"
                    let inner = task { return data }

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can bind an Task"
                <| async {
                    let inner: Task = Task.CompletedTask

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
                testCaseAsync "Can bind a ValueTask<T>"
                <| async {
                    let data = "foo"
                    let inner = valueTask { return data }

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync "Can bind a ValueTask"
                <| async {
                    let inner: ValueTask = ValueTask.CompletedTask

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }

                testCaseAsync "Can bind an TaskLike"
                <| async {
                    let inner = Task.Yield()

                    let outer =
                        asyncEx {
                            let! result = inner
                            return result
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
                testCaseAsync "Can Bind Type inference"
                <| async {
                    let expected = "lol"

                    let outerTask fooTask =
                        asyncEx {
                            let! result = fooTask
                            return result
                        }

                    let! actual = outerTask (async.Return expected)

                    Expect.equal actual expected ""
                }
            ]
            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = "foo"
                    let inner = asyncEx { return data }

                    let outer =
                        asyncEx {
                            let! result = inner

                            if true then
                                ()

                            return result
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
            ]
            testList "TryWith" [
                testCaseAsync "simple try with"
                <| async {
                    let data = "foo"
                    let inner = asyncEx { return data }

                    let outer =
                        asyncEx {
                            try
                                let! result = inner
                                return result
                            with ex ->
                                return failwith "Should not throw"
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
                testCaseAsync
                    "Awaiting Failed Task<'T> should only contain one exception and not aggregation"
                <| async {
                    let data = "lol"

                    let inner =
                        asyncEx {
                            let! result =
                                task {
                                    do! Task.Yield()
                                    raise (ArgumentException "foo")
                                    return data
                                }

                            return result
                        }

                    let outer =
                        asyncEx {
                            try
                                let! result = inner
                                return ()
                            with
                            | :? ArgumentException ->
                                // Should be this exception and not AggregationException
                                return ()
                            | ex ->
                                return
                                    raise (Exception("Should not throw this type of exception", ex))
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }

                testCaseAsync
                    "Awaiting Failed Task should only contain one exception and not aggregation"
                <| async {
                    let data = "lol"

                    let inner =
                        asyncEx {
                            do!
                                task {
                                    do! Task.Yield()
                                    raise (ArgumentException "foo")
                                    return data
                                }
                                :> Task
                        }

                    let outer =
                        asyncEx {
                            try
                                do! inner
                                return ()
                            with
                            | :? ArgumentException ->
                                // Should be this exception and not AggregationException
                                return ()
                            | ex ->
                                return
                                    raise (Exception("Should not throw this type of exception", ex))
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
                testCaseAsync
                    "Awaiting Failed ValueTask<'T> should only contain one exception and not aggregation"
                <| async {
                    let data = "lol"

                    let inner =
                        asyncEx {
                            let! result =
                                valueTask {
                                    do! Task.Yield()
                                    raise (ArgumentException "foo")
                                    return data
                                }

                            return result
                        }

                    let outer =
                        asyncEx {
                            try
                                let! result = inner
                                return ()
                            with
                            | :? ArgumentException ->
                                // Should be this exception and not AggregationException
                                return ()
                            | ex ->
                                return
                                    raise (Exception("Should not throw this type of exception", ex))
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }

                testCaseAsync
                    "Awaiting Failed CustomAwaiter should only contain one exception and not aggregation"
                <| async {
                    let data = "lol"

                    let inner =
                        asyncEx {
                            let awaiter =
                                CustomAwaiter.CustomAwaiter(
                                    (fun () -> raise (ArgumentException "foo")),
                                    (fun () -> true)
                                )

                            let! result = awaiter

                            return result
                        }

                    let outer =
                        asyncEx {
                            try
                                let! result = inner
                                return ()
                            with
                            | :? ArgumentException ->
                                // Should be this exception and not AggregationException
                                return ()
                            | ex ->
                                return
                                    raise (
                                        Exception(
                                            $"Should not throw this type of exception {ex.GetType()}",
                                            ex
                                        )
                                    )
                        }

                    let! result = outer
                    Expect.equal result () "Should return the data"
                }
            ]
            testList "TryFinally" [
                testCaseAsync "simple try finally"
                <| async {
                    let data = "foo"
                    let inner = asyncEx { return data }

                    let outer =
                        asyncEx {
                            try
                                let! result = inner
                                return result
                            finally
                                ()
                        }

                    let! result = outer
                    Expect.equal result data "Should return the data"
                }
            ]
            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        asyncEx {
                            use d = TestHelpers.makeDisposable (doDispose)
                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! using"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        asyncEx {
                            use! d =
                                TestHelpers.makeDisposable (doDispose)
                                |> async.Return

                            return data
                        }

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
                        asyncEx {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            return data
                        }

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
                        asyncEx {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask

                    let! actual =
                        asyncEx {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "use! IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask

                    let! actual =
                        asyncEx {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }

                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        asyncEx {
                            use d = null
                            return data
                        }

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
                                asyncEx {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }

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
                                asyncEx {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }

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
                                asyncEx {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }

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
                                asyncEx {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }

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
                                asyncEx {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }

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
                                asyncEx {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }

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
                                asyncEx {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }

                            Expect.equal actual index "Should be ok"
                        }
                    )
                // https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/179
                testCaseAsync "IAsyncEnumerable cancellation"
                <| async {

                    do!
                        Expect.CancellationRequested(
                            async {

                                let mutable index = 0
                                let loops = 10

                                let asyncSeq: IAsyncEnumerable<_> =
                                    AsyncEnumerable.forXtoY
                                        0
                                        loops
                                        (fun _ -> valueTaskUnit { do! Task.Yield() })

                                use cts = new CancellationTokenSource()

                                let actual =
                                    asyncEx {
                                        for (i: int) in asyncSeq do
                                            do! Task.Yield()

                                            if index >= 5 then
                                                cts.Cancel()

                                            index <- index + 1
                                    }

                                do!
                                    Async.StartAsTask(actual, cancellationToken = cts.Token)
                                    |> Async.AwaitTask
                            }
                        )
                }

            ]
        ]


    [<Tests>]
    let asyncExTests = testList "IcedTasks.AsyncEx" [ builderTests ]

module PolyfillTest =
    open IcedTasks.Polyfill.Async

    let builderTests =
        testList "SmokeTests" [
            testCaseAsync "Bind any awaitable"
            <| async {
                // Compiling this code will fail if Bind is not defined for any awaitable
                let! result = async { do! Task.Yield() }
                return result
            }
            testCaseAsync "use IAsyncDisposable sync"
            <| async {
                let data = 42
                let mutable wasDisposed = false

                let doDispose () =
                    wasDisposed <- true
                    ValueTask.CompletedTask

                let! actual =
                    async {
                        use d = TestHelpers.makeAsyncDisposable (doDispose)
                        return data
                    }

                Expect.equal actual data "Should be able to use use"
                Expect.isTrue wasDisposed ""
            }

            testCaseAsync
                "Awaiting Failed Task<'T> should only contain one exception and not aggregation"
            <| async {
                let data = "lol"

                let inner =
                    asyncEx {
                        let! result =
                            task {
                                do! Task.Yield()
                                raise (ArgumentException "foo")
                                return data
                            }

                        return result
                    }

                let outer =
                    async {
                        try
                            let! result = inner
                            return ()
                        with
                        | :? ArgumentException ->
                            // Should be this exception and not AggregationException
                            return ()
                        | ex ->
                            return raise (Exception("Should not throw this type of exception", ex))
                    }

                let! result = outer
                Expect.equal result () "Should return the data"
            }

            let withCancellation (ct: CancellationToken) (a: Async<'a>) : Async<'a> =
                async {
                    let! ct2 = Async.CancellationToken
                    use cts = CancellationTokenSource.CreateLinkedTokenSource(ct, ct2)
                    let tcs = new TaskCompletionSource<'a>()

                    use _reg =
                        cts.Token.Register(fun () ->
                            tcs.TrySetCanceled(cts.Token)
                            |> ignore
                        )

                    let a =
                        async {
                            try
                                let! a = a

                                tcs.TrySetResult a
                                |> ignore
                            with ex ->
                                tcs.TrySetException ex
                                |> ignore
                        }

                    Async.Start(a, cts.Token)

                    return!
                        tcs.Task
                        |> AsyncEx.AwaitTask
                }

            testCase "Don't cancel everything if one task cancels"
            <| fun () ->
                use cts = new CancellationTokenSource()
                cts.CancelAfter(100)

                let doWork i =
                    asyncEx {
                        try
                            let! _ =
                                Async.Sleep(100)
                                |> withCancellation cts.Token

                            ()
                        with :? OperationCanceledException as e ->
                            ()
                    }

                Seq.init
                    (Environment.ProcessorCount
                     * 2)
                    doWork
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
        ]


    [<Tests>]
    let asyncExTests =
        testList "IcedTasks.Polyfill.Async" [
            builderTests

        ]
