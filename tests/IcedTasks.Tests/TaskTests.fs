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

            testList "MergeSources" [

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

            ]

            testList "MergeSourcesParallel" [
                testPropertyWithConfig Expecto.fsCheckConfig "parallelism"
                <| fun () ->
                    asyncEx {
                        let! ct = Async.CancellationToken
                        let sequencedList = ResizeArray<_>()
                        let parallelList = ResizeArray<_>()

                        let doOtherStuff (l: ResizeArray<_>) x =
                            task {
                                l.Add(x)
                                do! Task.yieldMany 1000
                                let dt = DateTimeOffset.UtcNow
                                l.Add(x)
                                return dt
                            }

                        let! sequenced =
                            task {
                                let! a = doOtherStuff sequencedList 1
                                let! b = doOtherStuff sequencedList 2
                                let! c = doOtherStuff sequencedList 3
                                let! d = doOtherStuff sequencedList 4
                                let! e = doOtherStuff sequencedList 5
                                let! f = doOtherStuff sequencedList 6

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
                                let! a = doOtherStuff parallelList 1
                                and! b = doOtherStuff parallelList 2
                                and! c = doOtherStuff parallelList 3
                                and! d = doOtherStuff parallelList 4
                                and! e = doOtherStuff parallelList 5
                                and! f = doOtherStuff parallelList 6

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


    [<Tests>]
    let tests = testList "IcedTasks.Polyfill.Task" [ builderTests ]

    type ImplicitEnumerator(length: int) =
        let mutable index = -1
        member _.Current = index

        member _.MoveNext() =
            index <- index + 1
            index < length

    type ImplicitEnumerable(length) =
        member _.GetEnumerator() = ImplicitEnumerator(length)

    type ImplicitEnumeratorDisposable(length: int, onDispose) =
        let mutable index = -1
        member _.Current = index

        member _.MoveNext() =
            index <- index + 1
            index < length

        member _.Dispose() = onDispose ()

    type ImplicitEnumerableDisposable(length, onDispose) =
        member _.GetEnumerator() =
            ImplicitEnumeratorDisposable(length, onDispose)


    open Microsoft.FSharp.Core.CompilerServices

    type Disposable<'Disposable when 'Disposable: (member Dispose: unit -> unit)> = 'Disposable

    type Disposable =

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        [<NoEagerConstraintApplication>]
        static member inline Dispose<'Disposable when Disposable<'Disposable>>(d: 'Disposable) =
            d.Dispose()

    [<AutoOpen>]
    module LowerPriorityDisposable =
        type Disposable with

            /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            static member inline Dispose(d: #IDisposable) = d.Dispose()

    type Enumerator<'Enumerator, 'Element
        when 'Enumerator: (member Current: 'Element)
        and 'Enumerator: (member MoveNext: unit -> bool)> = 'Enumerator

    type EnumeratorDisposable<'Enumerator, 'Element
        when Enumerator<'Enumerator, 'Element> and Disposable<'Enumerator>> = 'Enumerator

    type Enumerator =

        /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
        static member inline Current(e: #IEnumerator<'Element>) = e.Current

        /// <summary>Advances the enumerator to the next element of the collection.</summary>
        static member inline MoveNext(e: #IEnumerator<'Element>) =
            let mutable e = e
            e.MoveNext()

    [<AutoOpen>]
    module LowerPriorityEnumerator =

        type Enumerator with


            /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
            [<NoEagerConstraintApplication>]
            static member inline Current<'Enumerator, 'Element
                when Enumerator<'Enumerator, 'Element>>
                (e: 'Enumerator)
                =
                e.Current

            /// <summary>Advances the enumerator to the next element of the collection.</summary>
            [<NoEagerConstraintApplication>]
            static member inline MoveNext<'Enumerator, 'Element
                when Enumerator<'Enumerator, 'Element>>
                (e: 'Enumerator)
                =
                e.MoveNext()


    type Enumerable<'Enumerable, 'Enumerator, 'Element
        when 'Enumerable: (member GetEnumerator: unit -> Enumerator<'Enumerator, 'Element>)> =
        'Enumerable

    type EnumerableDisposable<'Enumerable, 'Enumerator, 'Element
        when 'Enumerable: (member GetEnumerator: unit -> EnumeratorDisposable<'Enumerator, 'Element>)>
        = 'Enumerable

    type Enumerable =

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        static member inline GetEnumerator(e: #IEnumerable<'Element>) = e.GetEnumerator()


    [<AutoOpen>]
    module LowerPriorityEnumerable =
        open System.Collections.Generic

        type Enumerable with

            /// <summary>Returns an enumerator that iterates through a collection.</summary>
            [<NoEagerConstraintApplication>]
            static member inline GetEnumerator<'Enumerable, 'Enumerator, 'Element
                when Enumerable<'Enumerable, 'Enumerator, 'Element>>
                (e: 'Enumerable)
                =
                e.GetEnumerator()


    type TestEnumBuilder() =
        member inline _.Zero() = ()

        member inline _.For(items: 'a seq, [<InlineIfLambda>] (body: 'a -> unit)) =
            for i in items do
                body i

    [<AutoOpen>]
    module TestEnumBuilderExtensions =
        type TestEnumBuilder with

            member inline _.For<'Enumerable, 'Enumerator, 'Element
                when Enumerable<'Enumerable, 'Enumerator, 'Element>>
                (
                    items: 'Enumerable,
                    [<InlineIfLambda>] (body: 'Element -> unit)
                ) =
                let e = Enumerable.GetEnumerator items

                while Enumerator.MoveNext e do
                    body (Enumerator.Current e)


    [<AutoOpen>]
    module TestEnumBuilderExtensions2 =
        type TestEnumBuilder with

            member inline _.For<'Enumerable, 'Enumerator, 'Element
                when EnumerableDisposable<'Enumerable, 'Enumerator, 'Element>>
                (
                    items: 'Enumerable,
                    [<InlineIfLambda>] (body: 'Element -> unit)
                ) =

                let e = Enumerable.GetEnumerator items

                try
                    while Enumerator.MoveNext e do
                        body (Enumerator.Current e)
                finally
                    Disposable.Dispose e

    let b = TestEnumBuilder()

    [<Tests>]
    let implicitEnumerableTests =
        testList "Enumerable Test Builder" [
            testCase "generic enumerable 1"
            <| fun () ->
                let mutable result = 0

                b {
                    for i in [ 1..10 ] do
                        result <- i
                }

                Expect.equal result 10 "Should be 10"
            testCase "generic enumerable 2"
            <| fun () ->
                let mutable result = 0

                b {
                    for i in ImplicitEnumerable(10) do
                        result <- i
                }

                Expect.equal result 9 "Should be 9"

            testCase "generic enumerable 3"
            <| fun () ->
                let mutable result = 0
                let mutable disposed = false

                b {
                    for i in ImplicitEnumerableDisposable(10, (fun _ -> disposed <- true)) do

                        Expect.isFalse disposed "Should not be disposed"
                        result <- i
                }

                Expect.equal result 9 "Should be 9"
                Expect.isTrue disposed "Should be disposed"
        ]
