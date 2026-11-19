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

                testCaseAsync "while bind to 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let! actual =
                        asyncEx {
                            while index < loops do
                                do! Task.CompletedTask
                                index <- index + 1

                            return index
                        }

                    Expect.equal actual loops "Should be ok"
                }
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

                testCaseAsync "for bind to 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let! actual =
                        asyncEx {
                            for i = 1 to loops do
                                do! Task.CompletedTask
                                index <- i + i

                            return index
                        }

                    Expect.equal actual (loops * 2) "Every iteration must run"
                }

                yield!
                    [
                        10
                        10000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"IAsyncEnumerable for in {loops}"
                        <| async {
                            let mutable index = 0

                            let asyncSeq: IAsyncEnumerable<_> =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

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

                testCaseAsync "IAsyncEnumerable for in 100000 completes synchronously"
                <| async {
                    let loops = 100000

                    let mutable index = 0

                    let asyncSeq: IAsyncEnumerable<_> =
                        AsyncEnumerable.forXtoY
                            0
                            loops
                            (cancellableValueTask { do! Task.CompletedTask })

                    let! actual =
                        asyncEx {
                            for (i: int) in asyncSeq do
                                do! Task.CompletedTask
                                index <- i + i

                            return index
                        }

                    Expect.equal actual (loops * 2) "Every iteration must run"
                }
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
                                        (cancellableValueTask { do! Task.Yield() })

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


                testCaseAsync "IAsyncEnumerator receives CancellationToken"
                <| async {
                    do!
                        asyncEx {

                            let mutable index = 0
                            let loops = 10

                            let asyncSeq =
                                AsyncEnumerable.forXtoY
                                    0
                                    loops
                                    (cancellableValueTask { do! Task.Yield() })

                            use cts = new CancellationTokenSource()

                            let actual =
                                asyncEx {
                                    for (i: int) in asyncSeq do
                                        do! Task.Yield()
                                        index <- index + 1
                                }

                            do!
                                Async.StartAsTask(actual, cancellationToken = cts.Token)
                                |> Async.AwaitTask

                            Expect.equal
                                asyncSeq.LastEnumerator.Value.CancellationToken
                                cts.Token
                                ""
                        }
                }


#if TEST_NETSTANDARD2_1 || TEST_NET6_0_OR_GREATER
                testCaseAsync "TaskSeq receives CancellationToken"
                <| async {

                    do!
                        asyncEx {

                            let loops = 1

                            let mutable CancellationToken = CancellationToken.None

                            let asyncSeq =
                                taskSeq {
                                    for i in 0..loops do
                                        do!
                                            asyncEx {
                                                let! ct = Async.CancellationToken
                                                CancellationToken <- ct
                                            }

                                        yield ()
                                }

                            use cts = new CancellationTokenSource()

                            let actual =
                                asyncEx {
                                    for i in asyncSeq do
                                        do! Task.Yield()
                                }

                            do!
                                Async.StartAsTask(actual, cancellationToken = cts.Token)
                                |> Async.AwaitTask

                            Expect.equal CancellationToken cts.Token ""
                        }
                }
#endif

            ]
        ]


    let private deadline = TimeSpan.FromSeconds 10.

    let private settle (result: Task) =
        result.ContinueWith(fun (_: Task) -> ()).WaitAsync(deadline)

    let private runUntilCanceled (work: CancellationTokenSource -> Async<unit>) =
        task {
            use cts = new CancellationTokenSource()
            let result = Async.StartImmediateAsTask(work cts, cts.Token)
            do! settle result
            return result
        }

    let private countingDisposable (disposeCalls: int ref) (disposeAsync: unit -> ValueTask) =
        TestHelpers.makeAsyncDisposable (fun () ->
            Interlocked.Increment(&disposeCalls.contents)
            |> ignore

            disposeAsync ()
        )

    let private recordingDisposable
        (events: ResizeArray<string>)
        name
        (disposeAsync: unit -> ValueTask)
        =
        TestHelpers.makeAsyncDisposable (fun () ->
            lock events (fun () -> events.Add name)
            disposeAsync ()
        )

    let cancelBeforeBodyTests =
        testList "Cancellation and disposal" [
            testCaseTask
                "before the body starts, use disposes the resource once, the body does not run, and the workflow is canceled"
            <| fun () ->
                task {
                    let disposeCalls = ref 0
                    let bodyRuns = ref 0

                    let resource =
                        countingDisposable disposeCalls (fun () -> ValueTask.CompletedTask)

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use _ =
                                    cts.Cancel()
                                    resource

                                bodyRuns.Value <-
                                    bodyRuns.Value
                                    + 1
                            }
                        )

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal disposeCalls.Value 1 "use must dispose the resource once"
                    Expect.equal bodyRuns.Value 0 "The body must not run"
                }
            testCaseTask
                "before the body starts, use! disposes the resource once, the body does not run, and the workflow is canceled"
            <| fun () ->
                task {
                    let disposeCalls = ref 0
                    let bodyRuns = ref 0

                    let resource =
                        countingDisposable disposeCalls (fun () -> ValueTask.CompletedTask)

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use! acquired =
                                    CustomAwaiter.CustomAwaiter(
                                        (fun () ->
                                            cts.Cancel()
                                            resource
                                        ),
                                        (fun () -> true)
                                    )

                                bodyRuns.Value <-
                                    bodyRuns.Value
                                    + 1
                            }
                        )

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal disposeCalls.Value 1 "use! must dispose the resource once"
                    Expect.equal bodyRuns.Value 0 "The body must not run"
                }
            testCaseTask
                "before the body starts, for disposes the enumerator once, does not move next, and the workflow is canceled"
            <| fun () ->
                task {
                    let disposeCalls = ref 0
                    let moveNextCalls = ref 0

                    let! result =
                        runUntilCanceled (fun cts ->
                            let items =
                                { new IAsyncEnumerable<int> with
                                    member _.GetAsyncEnumerator(_) =
                                        cts.Cancel()

                                        { new IAsyncEnumerator<int> with
                                            member _.MoveNextAsync() =
                                                Interlocked.Increment(&moveNextCalls.contents)
                                                |> ignore

                                                ValueTask<bool>(false)

                                            member _.Current = 0

                                            member _.DisposeAsync() =
                                                Interlocked.Increment(&disposeCalls.contents)
                                                |> ignore

                                                ValueTask.CompletedTask
                                        }
                                }

                            asyncEx {
                                for _ in items do
                                    ()
                            }
                        )

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal disposeCalls.Value 1 "for must dispose the enumerator once"
                    Expect.equal moveNextCalls.Value 0 "for must not move next"
                }
            testCaseTask
                "before the body starts, a pending DisposeAsync starts once, is read once, and the workflow is canceled without waiting for it"
            <| fun () ->
                task {
                    let disposeCalls = ref 0
                    let source = AsyncDisposalOutcome.ObservedDisposalSource()
                    let resource = countingDisposable disposeCalls source.ValueTask

                    try
                        let! result =
                            runUntilCanceled (fun cts ->
                                asyncEx {
                                    use _ =
                                        cts.Cancel()
                                        resource

                                    return ()
                                }
                            )

                        Expect.isTrue result.IsCanceled "The workflow must be canceled"
                        Expect.equal disposeCalls.Value 1 "use must start DisposeAsync once"
                    finally
                        source.Complete AsyncDisposalOutcome.DisposalSucceeds

                    do! source.ResultRead.WaitAsync(deadline)

                    Expect.equal
                        source.GetResultCalls
                        1
                        "The pending DisposeAsync result must be read once"
                }
            testCaseTask
                "before the body starts, a DisposeAsync exception is discarded and the workflow is canceled"
            <| fun () ->
                task {
                    let disposeCalls = ref 0

                    let resource =
                        countingDisposable
                            disposeCalls
                            (fun () -> raise (InvalidOperationException "dispose"))

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use _ =
                                    cts.Cancel()
                                    resource

                                return ()
                            }
                        )

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal disposeCalls.Value 1 "use must call DisposeAsync once"
                }
            testCaseTask
                "an enclosing use waits for a pending inner DisposeAsync before it disposes its own resource"
            <| fun () ->
                task {
                    let events = ResizeArray<string>()
                    let innerSource = AsyncDisposalOutcome.ObservedDisposalSource()

                    let outer =
                        recordingDisposable events "outer" (fun () -> ValueTask.CompletedTask)

                    let inner = recordingDisposable events "inner" innerSource.ValueTask
                    use cts = new CancellationTokenSource()

                    let result =
                        Async.StartImmediateAsTask(
                            asyncEx {
                                use _ = outer

                                use _ =
                                    cts.Cancel()
                                    inner

                                return ()
                            },
                            cts.Token
                        )

                    try
                        do! innerSource.ContinuationRegistered.WaitAsync(deadline)

                        Expect.isFalse
                            result.IsCompleted
                            "The workflow must wait for the pending inner DisposeAsync"

                        Expect.sequenceEqual
                            (lock events (fun () -> List.ofSeq events))
                            [ "inner" ]
                            "The outer resource must stay open while the inner DisposeAsync is pending"
                    finally
                        innerSource.Complete AsyncDisposalOutcome.DisposalSucceeds

                    do! settle result
                    Expect.isTrue result.IsCanceled "The workflow must be canceled"

                    Expect.sequenceEqual
                        (lock events (fun () -> List.ofSeq events))
                        [
                            "inner"
                            "outer"
                        ]
                        "The outer resource must be disposed after the inner DisposeAsync completes"
                }
            testCaseTask "an inner DisposeAsync failure fails the enclosing workflow"
            <| fun () ->
                task {
                    let events = ResizeArray<string>()
                    let innerSource = AsyncDisposalOutcome.ObservedDisposalSource()

                    let outer =
                        recordingDisposable events "outer" (fun () -> ValueTask.CompletedTask)

                    let inner = recordingDisposable events "inner" innerSource.ValueTask
                    use cts = new CancellationTokenSource()

                    let result =
                        Async.StartImmediateAsTask(
                            asyncEx {
                                use _ = outer

                                use _ =
                                    cts.Cancel()
                                    inner

                                return ()
                            },
                            cts.Token
                        )

                    try
                        do! innerSource.ContinuationRegistered.WaitAsync(deadline)
                    finally
                        innerSource.Complete AsyncDisposalOutcome.DisposalFaults

                    do! settle result

                    Expect.isTrue
                        result.IsFaulted
                        "The inner DisposeAsync failure must fail the workflow"

                    Expect.isTrue
                        (result.Exception.InnerExceptions
                         |> Seq.exists (fun e -> e :? InvalidOperationException))
                        "The workflow must fail with the inner DisposeAsync exception"

                    Expect.sequenceEqual
                        (lock events (fun () -> List.ofSeq events))
                        [
                            "inner"
                            "outer"
                        ]
                        "The outer resource must be disposed after the inner DisposeAsync completes"
                }
            testCaseTask
                "Async.TryFinallyAsync on a canceled token runs only the compensation, with a token that cannot cancel"
            <| fun () ->
                task {
                    let computationRuns = ref 0
                    let compensationRuns = ref 0
                    let compensationCanBeCanceled = ref true
                    use cts = new CancellationTokenSource()
                    cts.Cancel()

                    let work =
                        Async.TryFinallyAsync(
                            async {
                                computationRuns.Value <-
                                    computationRuns.Value
                                    + 1
                            },
                            async {
                                let! token = Async.CancellationToken
                                compensationCanBeCanceled.Value <- token.CanBeCanceled

                                compensationRuns.Value <-
                                    compensationRuns.Value
                                    + 1
                            }
                        )

                    let result = Async.StartImmediateAsTask(work, cts.Token)
                    do! settle result

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal computationRuns.Value 0 "The computation must not run"
                    Expect.equal compensationRuns.Value 1 "The compensation must run once"

                    Expect.isFalse
                        compensationCanBeCanceled.Value
                        "The compensation must run with a token that cannot cancel"
                }
            testCaseTask "a null cancellation from the computation runs the compensation once"
            <| fun () ->
                task {
                    let compensationRuns = ref 0

                    let work =
                        Async.TryFinallyAsync(
                            Async.FromContinuations(fun (_, _, ccont) ->
                                ccont Unchecked.defaultof<OperationCanceledException>
                            ),
                            async {
                                compensationRuns.Value <-
                                    compensationRuns.Value
                                    + 1
                            }
                        )

                    let result = Async.StartImmediateAsTask work
                    do! settle result

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal compensationRuns.Value 1 "The compensation must run once"
                }
            testCaseTask "a cancellation inside the body disposes the resource once"
            <| fun () ->
                task {
                    let disposeCalls = ref 0

                    let resource =
                        countingDisposable disposeCalls (fun () -> ValueTask.CompletedTask)

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use _ = resource
                                cts.Cancel()
                                do! async { return () }
                            }
                        )

                    Expect.isTrue result.IsCanceled "The workflow must be canceled"
                    Expect.equal disposeCalls.Value 1 "use must dispose the resource once"
                }
            testCaseTask
                "a cancellation inside the body followed by a DisposeAsync failure fails the workflow"
            <| fun () ->
                task {
                    let resource =
                        TestHelpers.makeAsyncDisposable (fun () ->
                            ValueTask(Task.FromException(InvalidOperationException "dispose"))
                        )

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use _ = resource
                                cts.Cancel()
                                do! async { return () }
                            }
                        )

                    Expect.isTrue result.IsFaulted "The DisposeAsync failure must fail the workflow"

                    Expect.isTrue
                        (result.Exception.InnerExceptions
                         |> Seq.exists (fun e -> e :? InvalidOperationException))
                        "The workflow must fail with the DisposeAsync exception"
                }
            testCaseTask
                "a cancellation inside the body followed by a canceled DisposeAsync fails the workflow"
            <| fun () ->
                task {
                    let resource =
                        TestHelpers.makeAsyncDisposable (fun () ->
                            ValueTask(Task.FromException(OperationCanceledException "dispose"))
                        )

                    let! result =
                        runUntilCanceled (fun cts ->
                            asyncEx {
                                use _ = resource
                                cts.Cancel()
                                do! async { return () }
                            }
                        )

                    Expect.isTrue
                        result.IsFaulted
                        "The DisposeAsync cancellation must fail the workflow"
                }
        ]

    let disposalOutcomeTests =
        testList "DisposeAsync outcome" [
            AsyncDisposalOutcome.tests
                "asyncEx"
                AsyncDisposalOutcome.AsyncWorkflow
                [
                    AsyncDisposalOutcome.BodyCompletes
                    AsyncDisposalOutcome.BodyThrows
                ]
                AsyncDisposalOutcome.asyncExpectation
                (fun body cts resource ->
                    asyncEx {
                        use _ = resource
                        AsyncDisposalOutcome.runBody body cts
                    }
                    |> fun a -> Async.StartImmediateAsTask(a, cts.Token) :> Task
                )
                (fun body cts items ->
                    asyncEx {
                        for _ in items do
                            AsyncDisposalOutcome.runBody body cts
                    }
                    |> fun a -> Async.StartImmediateAsTask(a, cts.Token) :> Task
                )
            testCaseAsync "use disposes the resource when the body throws before its first bind"
            <| async {
                let disposeCalls = ref 0
                let bodyError = ArgumentException "body"

                let resource =
                    TestHelpers.makeAsyncDisposable (fun () ->
                        disposeCalls.Value <-
                            disposeCalls.Value
                            + 1

                        ValueTask.CompletedTask
                    )

                let! result =
                    asyncEx {
                        use _ = resource
                        raise bodyError
                    }
                    |> Async.Catch

                match result with
                | Choice2Of2 error ->
                    Expect.isTrue
                        (obj.ReferenceEquals(error, bodyError))
                        "The body exception must be the result"
                | Choice1Of2() -> failtest "The body exception must fail the computation"

                Expect.equal disposeCalls.Value 1 "use must dispose the resource"
            }
            testCaseAsync "use! disposes the resource when the body throws before its first bind"
            <| async {
                let disposeCalls = ref 0
                let bodyError = ArgumentException "body"

                let resource =
                    TestHelpers.makeAsyncDisposable (fun () ->
                        disposeCalls.Value <-
                            disposeCalls.Value
                            + 1

                        ValueTask.CompletedTask
                    )

                let! result =
                    asyncEx {
                        use! resource = async.Return resource
                        raise bodyError
                    }
                    |> Async.Catch

                match result with
                | Choice2Of2 error ->
                    Expect.isTrue
                        (obj.ReferenceEquals(error, bodyError))
                        "The body exception must be the result"
                | Choice1Of2() -> failtest "The body exception must fail the computation"

                Expect.equal disposeCalls.Value 1 "use! must dispose the resource"
            }
            testCaseAsync
                "do! of a completed IValueTaskSource ValueTask reads GetResult exactly once"
            <| async {
                let source = AsyncDisposalOutcome.ObservedDisposalSource()
                source.Complete AsyncDisposalOutcome.DisposalSucceeds

                do! asyncEx { do! source.ValueTask() }

                Expect.equal source.GetResultCalls 1 "AwaitValueTask must read the result once"
            }
            testCaseAsync
                "a faulted ValueTask fails with the input exception and does not throw at the call"
            <| async {
                let input = ArgumentException "input"
                let awaited = AsyncEx.AwaitValueTask(ValueTask(Task.FromException input))
                let! result = Async.Catch awaited

                match result with
                | Choice2Of2(:? AggregateException as error) ->
                    Expect.isTrue
                        (obj.ReferenceEquals(error.InnerException, input))
                        "The result must keep the input exception"
                | Choice2Of2 error ->
                    Expect.isTrue
                        (obj.ReferenceEquals(error, input))
                        "The result must keep the input exception"
                | Choice1Of2() -> failtest "The input exception must fail the result"
            }
            testCaseAsync
                "a canceled ValueTask fails with a cancellation and does not throw at the call"
            <| async {
                let awaited =
                    AsyncEx.AwaitValueTask(ValueTask(Task.FromCanceled(CancellationToken(true))))

                let! result = Async.Catch awaited

                match result with
                | Choice2Of2(:? OperationCanceledException) -> ()
                | other -> failtestf "Expected an OperationCanceledException, got %A" other
            }
        ]

    [<Tests>]
    let asyncExTests =
        testList "IcedTasks.AsyncEx" [
            builderTests
            disposalOutcomeTests
            cancelBeforeBodyTests
        ]

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
