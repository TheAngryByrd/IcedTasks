module IcedTasks.BinaryCompat.Program

open System
open System.Threading
open System.Threading.Tasks
open IcedTasks

let private lines = ResizeArray<string>()
let private record name (value: 'T) = lines.Add $"%s{name}=%A{value}"

type private AsyncResource(log: ResizeArray<string>) =
    interface IAsyncDisposable with
        member _.DisposeAsync() =
            log.Add "disposed"
            ValueTask.CompletedTask

let private coldWork: ColdTask<int> = coldTask { return 20 }

let private cancellableWork: CancellableTask<int> =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()
        return if ct.CanBeCanceled then 2 else 1
    }

let private coldTaskCase () =
    coldTask {
        let! a = Task.FromResult 1
        let! b = ValueTask<int>(2)
        let! c = async { return 3 }
        let! d = coldWork
        do! Task.Yield()

        return
            a
            + b
            + c
            + d
    }

let private cancellableTaskCase () =
    cancellableTask {
        let! a = coldWork
        let! b = cancellableWork
        and! c = Task.FromResult 3
        let mutable total = 0

        for i in [ 1..3 ] do
            do! Task.Yield()
            total <- total + i

        return
            a
            + b
            + c
            + total
    }

let private cancellableValueTaskCase () =
    cancellableValueTask {
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
    }

let private valueTaskCase () =
    valueTask {
        let! a = Task.FromResult 5
        let! b = ValueTask<int>(6)
        and! c = Task.FromResult 7

        try
            failwith "boom"
            return 0
        with e ->
            return
                a
                + b
                + c
                + e.Message.Length
    }

let private poolingValueTaskCase () =
    poolingValueTask {
        let! a = ValueTask<int>(8)
        let mutable i = 0

        while i < 3 do
            do! Task.Yield()
            i <- i + 1

        return a + i
    }

let private taskUnitCase (log: ResizeArray<string>) =
    taskUnit {
        do! Task.Yield()
        log.Add "taskUnit"
    }

let private asyncExCase () =
    asyncEx {
        let! a = Task.FromResult 9
        let! b = ValueTask<int>(10)
        let log = ResizeArray<string>()

        do!
            asyncEx {
                use! resource = async { return new AsyncResource(log) }
                ignore resource
            }

        return
            a
            + b
            + log.Count
    }

let private parallelAsyncCase () =
    parallelAsync {
        let! a = async { return 11 }
        and! b = async { return 12 }
        return a + b
    }

module private PolyfillTask =
    open IcedTasks.Polyfill.Task

    let run () =
        task {
            let! a = coldWork
            let! b = ValueTask<int>(13)
            return a + b
        }

[<EntryPoint>]
let main _ =
    let ct = CancellationToken.None
    let unitLog = ResizeArray<string>()

    record "coldTask" ((coldTaskCase ()) ()).Result
    record "cancellableTask" ((cancellableTaskCase ()) ct).Result
    record "cancellableValueTask" (((cancellableValueTaskCase ()) ct).AsTask().Result)
    record "valueTask" ((valueTaskCase ()).AsTask().Result)
    record "poolingValueTask" ((poolingValueTaskCase ()).AsTask().Result)
    (taskUnitCase unitLog).Wait()
    record "taskUnit" (List.ofSeq unitLog)
    record "asyncEx" (Async.RunSynchronously(asyncExCase ()))
    record "parallelAsync" (Async.RunSynchronously(parallelAsyncCase ()))
    record "task polyfill" (PolyfillTask.run ()).Result
    record "ColdTask.map" ((ColdTask.map (fun x -> x * 2) coldWork) ()).Result

    record
        "CancellableTask.map"
        ((CancellableTask.map (fun x -> x + 100) cancellableWork) ct).Result

    record
        "Async.AwaitCancellableTask"
        (Async.RunSynchronously(Async.AwaitCancellableTask cancellableWork))

    record "ValueTask.map" ((ValueTask.map (fun x -> x - 1) (ValueTask<int>(50))).AsTask().Result)

    let icedTasks = typeof<ColdTasks.ColdTaskBuilder>.Assembly
    eprintfn $"IcedTasks %s{string (icedTasks.GetName().Version)} from %s{icedTasks.Location}"

    for line in lines do
        printfn $"%s{line}"

    0
