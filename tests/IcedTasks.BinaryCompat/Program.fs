module IcedTasks.BinaryCompat.Program

open System
open System.Collections.Generic
open System.Runtime.Versioning
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open IcedTasks

let private deadline = TimeSpan.FromSeconds 60.

let private lines = ResizeArray<string>()

let mutable private current = "startup"
let private sinceStart = Diagnostics.Stopwatch.StartNew()

let private start name =
    current <- name
    sinceStart.Restart()

let private record name (value: 'T) = lines.Add $"%s{name}=%A{value}"

let private result (task: Task<'T>) = task.Result

let private complete (task: Task) = task.Wait()

let private run (computation: Async<'T>) = Async.RunSynchronously computation

type private AsyncResource(log: ResizeArray<string>) =
    interface IAsyncDisposable with
        member _.DisposeAsync() =
            log.Add "disposed"
            ValueTask.CompletedTask

// Each move stays pending until the awaiter registers its continuation, so each await suspends.
type private PendingMove() =
    let mutable registered = false

    interface IValueTaskSource<bool> with
        member _.GetStatus _ =
            if registered then
                ValueTaskSourceStatus.Succeeded
            else
                ValueTaskSourceStatus.Pending

        member _.GetResult _ = true

        member _.OnCompleted(continuation, state, _, _) =
            registered <- true

            ThreadPool.QueueUserWorkItem((fun s -> continuation.Invoke s), state)
            |> ignore

type private Items(items: int list) =
    interface IAsyncEnumerable<int> with
        member _.GetAsyncEnumerator(_) =
            let mutable rest = items
            let mutable current = 0

            { new IAsyncEnumerator<int> with
                member _.Current = current

                member _.MoveNextAsync() =
                    match rest with
                    | head :: tail ->
                        current <- head
                        rest <- tail
                        ValueTask<bool>(PendingMove(), 0s)
                    | [] -> ValueTask<bool>(false)

                member _.DisposeAsync() = ValueTask.CompletedTask
            }

let private coldWork: ColdTask<int> = coldTask { return 20 }

let private cancellableWork: CancellableTask<int> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        return if ct.CanBeCanceled then 2 else 1
    }

let private status (task: Task<'T>) =
    task.ContinueWith(fun (t: Task<'T>) -> string t.Status)

let private cases (ct: CancellationToken) (cancelled: CancellationToken) =
    start "coldTask"

    record
        "coldTask"
        (result (
            (coldTask {
                let! a = Task.FromResult 1
                let! b = ValueTask<int>(2)
                let! c = async { return 3 }
                let! d = coldWork
                do! Task.Yield()
                do! Task.CompletedTask
                let log = ResizeArray<string>()

                do!
                    coldTask {
                        use _ = new AsyncResource(log)
                        ()
                    }

                return
                    a
                    + b
                    + c
                    + d
                    + log.Count
            }) ()
        ))

    start "backgroundColdTask"

    record "backgroundColdTask" (result ((backgroundColdTask { return! coldWork }) ()))

    start "cancellableTask"

    record
        "cancellableTask"
        (result (
            (cancellableTask {
                let! a = coldWork
                let! b = cancellableWork
                and! c = Task.FromResult 3
                let mutable total = 0

                for i in [ 1..3 ] do
                    do! Task.Yield()
                    total <- total + i

                try
                    do! Task.Yield()
                finally
                    total <-
                        total
                        + 100

                return (a, b, c, total)
            })
                ct
        ))

    start "backgroundCancellableTask"

    record
        "backgroundCancellableTask"
        (result ((backgroundCancellableTask { return! cancellableWork }) ct))

    start "cancellableTask cancelled"

    record "cancellableTask cancelled" (result (status ((cancellableTask { return 1 }) cancelled)))

    start "cancellableValueTask"

    record
        "cancellableValueTask"
        (result (
            ((cancellableValueTask {
                let! a = ValueTask<int>(4)
                let! b = cancellableWork
                let log = ResizeArray<string>()

                do!
                    cancellableValueTask {
                        use _ = new AsyncResource(log)
                        do! Task.Yield()
                    }

                return
                    a
                    + b
                    + log.Count
            })
                ct)
                .AsTask()
        ))

    start "cancellableValueTask cancelled"

    record
        "cancellableValueTask cancelled"
        (result (status (((cancellableValueTask { return 1 }) cancelled).AsTask())))

    start "cancellablePoolingValueTask"

    record
        "cancellablePoolingValueTask"
        (result (
            ((cancellablePoolingValueTask {
                let! a = cancellableWork
                and! b = fun (_: CancellationToken) -> Task.FromResult 5
                let! c = fun (_: CancellationToken) -> ValueTask<int>(6)
                return (a, b, c)
            })
                ct)
                .AsTask()
        ))

    start "valueTask"

    record
        "valueTask"
        (result (
            (valueTask {
                let! a = Task.FromResult 5
                let! b = ValueTask<int>(6)
                and! c = Task.FromResult 7

                try
                    failwith "boom"
                    return (0, 0, 0, "")
                with e ->
                    return (a, b, c, e.Message)
            })
                .AsTask()
        ))

    start "poolingValueTask"

    record
        "poolingValueTask"
        (result (
            (poolingValueTask {
                let! a = ValueTask<int>(8)
                let mutable i = 0

                while i < 3 do
                    do! Task.Yield()
                    i <- i + 1

                return! ValueTask<int>(a + i)
            })
                .AsTask()
        ))

    start "unit builders"
    let unitLog = ResizeArray<string>()

    complete (
        taskUnit {
            do! Task.Yield()
            unitLog.Add "taskUnit"
        }
    )

    complete (
        backgroundTaskUnit {
            do! Task.CompletedTask
            unitLog.Add "backgroundTaskUnit"
        }
    )

    complete (
        (valueTaskUnit {
            do! ValueTask.CompletedTask
            unitLog.Add "valueTaskUnit"
        })
            .AsTask()
    )

    record "unit builders" (List.ofSeq unitLog)

    start "asyncEx"

    record
        "asyncEx"
        (run (
            asyncEx {
                let! a = Task.FromResult 9
                let! b = ValueTask<int>(10)
                do! Task.CompletedTask
                let log = ResizeArray<string>()

                do!
                    asyncEx {
                        use! resource = async { return new AsyncResource(log) }
                        ignore resource
                    }

                let mutable total = 0

                for i in
                    Items [
                        3
                        1
                        2
                    ] do
                    total <-
                        total * 10
                        + i

                let mutable n = 0

                while n < 2 do
                    n <- n + 1

                try
                    total <- total + n
                finally
                    log.Add "finally"

                return
                    a
                    + b
                    + total
                    + log.Count
            }
        ))

    start "parallelAsync"

    record
        "parallelAsync"
        (run (
            parallelAsync {
                let! a = async { return 11 }
                and! b = async { return 12 }
                return (a, b)
            }
        ))

    start "parallelAsyncUsingStartChild"

    record
        "parallelAsyncUsingStartChild"
        (run (
            parallelAsyncUsingStartChild {
                let! a = async { return 13 }
                and! b = async { return 14 }
                return (a, b)
            }
        ))

    start "ColdTask.map"
    record "ColdTask.map" (result ((ColdTask.map (fun x -> x * 2) coldWork) ()))

    start "CancellableTask.map"

    record
        "CancellableTask.map"
        (result ((CancellableTask.map (fun x -> x + 100) cancellableWork) ct))

    start "Async.AwaitCancellableTask"
    record "Async.AwaitCancellableTask" (run (Async.AwaitCancellableTask cancellableWork))

    start "ValueTask.map"

    record "ValueTask.map" (result ((ValueTask.map (fun x -> x - 1) (ValueTask<int>(50))).AsTask()))

module private PolyfillTask =
    open IcedTasks.Polyfill.Task

    let cases () =
        let log = ResizeArray<string>()

        start "task polyfill"

        record
            "task polyfill"
            (result (
                task {
                    let! a = coldWork
                    let! b = ValueTask<int>(13)

                    for i in
                        Items [
                            1
                            2
                        ] do
                        log.Add(string i)

                    return
                        a
                        + b
                        + log.Count
                }
            ))

        start "backgroundTask polyfill"

        record "backgroundTask polyfill" (result (backgroundTask { return! coldWork () }))

[<EntryPoint>]
let main _ =
    use _watchdog =
        new Timer(
            (fun _ ->
                if sinceStart.Elapsed > deadline then
                    eprintfn $"%s{current} did not complete in %O{deadline}"
                    exit 3
            ),
            null,
            TimeSpan.FromSeconds 1.,
            TimeSpan.FromSeconds 1.
        )

    use live = new CancellationTokenSource()
    use cts = new CancellationTokenSource()
    cts.Cancel()
    cases live.Token cts.Token
    PolyfillTask.cases ()

    let icedTasks = typeof<ColdTasks.ColdTaskBuilder>.Assembly

    let framework =
        icedTasks.GetCustomAttributes(typeof<TargetFrameworkAttribute>, false)
        |> Array.tryHead
        |> Option.map (fun a -> (a :?> TargetFrameworkAttribute).FrameworkName)
        |> Option.defaultValue "unknown"

    eprintfn
        $"IcedTasks %s{string (icedTasks.GetName().Version)} %s{framework} from %s{icedTasks.Location}"

    for line in lines do
        printfn $"%s{line}"

    0
