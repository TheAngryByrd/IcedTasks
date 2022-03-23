namespace IcedTasks.Tests

open System
open System.Threading.Tasks
open IcedTasks

module TestHelpers =
    let makeDisposable () =
        { new System.IDisposable with
            member this.Dispose() = () }

type Expect =

    static member CancellationRequested(operation: Async<_>) =
        async {
            try
                do! operation
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }

    static member CancellationRequested(operation: Task<_>) =
        task {
            try
                do! operation
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }

    static member CancellationRequested(operation: ColdTask<_>) =
        coldTask {
            try
                do! operation
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }


    static member CancellationRequested(operation: CancellableTask<_>) =
        cancellableTask {
            try
                do! operation
            with
            | :? TaskCanceledException as e -> ()
            | :? OperationCanceledException as e -> ()
        }
