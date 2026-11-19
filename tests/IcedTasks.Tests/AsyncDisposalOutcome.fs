namespace IcedTasks.Tests

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open Expecto

module AsyncDisposalOutcome =

    type DisposalOutcome =
        | DisposalSucceeds
        | DisposalFaults
        | DisposalCancels

    type DisposalCompletion =
        | CompletedDisposal
        | PendingDisposal

    type DisposalBody =
        | BodyCompletes
        | BodyThrows
        | BodyCancels
        | BodyAwaitsCanceledTask

    type Execution =
        | Static
        | Dynamic
        | AsyncWorkflow

    type ObservedResult =
        | Completed
        | Canceled of awaited: string
        | Faulted of exceptions: string list

    type BodyException(message: string) =
        inherit Exception(message)

    type DisposeException(message: string) =
        inherit Exception(message)

    let private bodyException () = BodyException "body"
    let private disposeException () = DisposeException "dispose"

    let private timeout = TimeSpan.FromSeconds 10.

    let runBody body (cts: CancellationTokenSource) =
        match body with
        | BodyCompletes -> ()
        | BodyThrows -> raise (bodyException ())
        | BodyCancels -> cts.Cancel()
        | BodyAwaitsCanceledTask -> ()

    let awaitsCanceledTask body = body = BodyAwaitsCanceledTask

    let canceledTask () =
        Task.FromCanceled(CancellationToken(true))

    type ObservedDisposalSource() =
        let mutable core = ManualResetValueTaskSourceCore<unit>()

        let continuationRegistered =
            TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)

        let resultRead =
            TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)

        let mutable getResultCalls = 0
        do core.RunContinuationsAsynchronously <- true

        member _.ContinuationRegistered = continuationRegistered.Task
        member _.ResultRead = resultRead.Task
        member _.GetResultCalls = getResultCalls

        member this.ValueTask() =
            ValueTask(this :> IValueTaskSource, core.Version)

        member _.Complete(outcome) =
            match outcome with
            | DisposalSucceeds -> core.SetResult(())
            | DisposalFaults -> core.SetException(disposeException ())
            | DisposalCancels -> core.SetException(OperationCanceledException "dispose")

        interface IValueTaskSource with
            member _.GetStatus(token) = core.GetStatus(token)

            member _.OnCompleted(continuation, state, token, flags) =
                core.OnCompleted(continuation, state, token, flags)

                continuationRegistered.TrySetResult(())
                |> ignore

            member _.GetResult(token) =
                Interlocked.Increment(&getResultCalls)
                |> ignore

                try
                    core.GetResult(token)
                finally
                    resultRead.TrySetResult(())
                    |> ignore

    let singleItemAsyncEnumerable (disposeAsync: unit -> ValueTask) =
        { new IAsyncEnumerable<int> with
            member _.GetAsyncEnumerator(_) =
                let mutable moved = false

                { new IAsyncEnumerator<int> with
                    member _.MoveNextAsync() =
                        let hasItem = not moved
                        moved <- true
                        ValueTask<bool>(hasItem)

                    member _.Current = 1
                    member _.DisposeAsync() = disposeAsync ()
                }
        }

    let rec private describe (e: exn) =
        match e with
        | :? AggregateException as ae ->
            let inner =
                ae.InnerExceptions
                |> Seq.map describe
                |> String.concat "; "

            $"AggregateException [{inner}]"
        | e -> $"{e.GetType().Name}: {e.Message}"

    let private awaitedException (resultTask: Task) =
        try
            resultTask.GetAwaiter().GetResult()
            "no exception"
        with e ->
            describe e

    let private observe (resultTask: Task) =
        match resultTask.Status with
        | TaskStatus.RanToCompletion -> Completed
        | TaskStatus.Canceled -> Canceled(awaitedException resultTask)
        | _ ->
            match resultTask.Exception with
            | null -> Faulted []
            | e ->
                e.InnerExceptions
                |> Seq.map describe
                |> List.ofSeq
                |> Faulted

    let taskExpectation outcome body =
        match outcome, body with
        | DisposalFaults, _ -> Faulted [ "DisposeException: dispose" ]
        | DisposalCancels, _ -> Canceled "OperationCanceledException: dispose"
        | DisposalSucceeds, BodyThrows -> Faulted [ "BodyException: body" ]
        | DisposalSucceeds, BodyCancels -> Canceled(describe (OperationCanceledException()))
        | DisposalSucceeds, BodyAwaitsCanceledTask -> Canceled(describe (TaskCanceledException()))
        | DisposalSucceeds, BodyCompletes -> Completed

    let asyncExpectation outcome body =
        match outcome, body with
        | _, BodyCancels -> invalidArg (nameof body) "The asyncEx matrix has no BodyCancels case"
        | DisposalSucceeds, BodyCompletes -> Completed
        | DisposalSucceeds, BodyThrows -> Faulted [ "BodyException: body" ]
        | DisposalFaults, BodyCompletes -> Faulted [ "DisposeException: dispose" ]
        | DisposalFaults, BodyThrows ->
            Faulted [ "AggregateException [BodyException: body; DisposeException: dispose]" ]
        | DisposalCancels, BodyCompletes -> Faulted [ "OperationCanceledException: dispose" ]
        | DisposalCancels, BodyThrows ->
            Faulted [
                "AggregateException [BodyException: body; OperationCanceledException: dispose]"
            ]
        | DisposalSucceeds, BodyAwaitsCanceledTask -> Faulted [ describe (TaskCanceledException()) ]
        | DisposalFaults, BodyAwaitsCanceledTask ->
            Faulted [
                $"AggregateException [{describe (TaskCanceledException())}; DisposeException: dispose]"
            ]
        | DisposalCancels, BodyAwaitsCanceledTask ->
            Faulted [
                $"AggregateException [{describe (TaskCanceledException())}; OperationCanceledException: dispose]"
            ]

    let private waitFor (signal: Task) (failure: string) =
        task {
            try
                do! signal.WaitAsync(timeout)
            with :? TimeoutException ->
                failtest failure
        }

    let private assertOutcome
        expected
        outcome
        completion
        body
        (start: DisposalBody -> CancellationTokenSource -> (unit -> ValueTask) -> Task)
        =
        task {
            let source = ObservedDisposalSource()
            use cts = new CancellationTokenSource()

            if completion = CompletedDisposal then
                source.Complete outcome

            let disposeCalls = ref 0

            let disposeAsync () =
                Interlocked.Increment(&disposeCalls.contents)
                |> ignore

                source.ValueTask()

            let resultTask = start body cts disposeAsync

            if completion = PendingDisposal then
                do!
                    waitFor
                        source.ContinuationRegistered
                        "The builder did not await the pending DisposeAsync result"

                Expect.isFalse
                    resultTask.IsCompleted
                    "The builder must not complete before the pending DisposeAsync result"

                source.Complete outcome

            do!
                waitFor
                    (resultTask.ContinueWith(fun (_: Task) -> ()))
                    "The builder result did not complete"

            Expect.equal
                (observe resultTask)
                (expected outcome body)
                "The DisposeAsync outcome must decide the result when it faults or cancels"

            Expect.equal disposeCalls.Value 1 "The builder must call DisposeAsync exactly once"

            Expect.equal
                source.GetResultCalls
                1
                "The builder must read the DisposeAsync result exactly once"
        }

    let private cases builder execution site bodies expected start = [
        for outcome in
            [
                DisposalSucceeds
                DisposalFaults
                DisposalCancels
            ] do
            for completion in
                [
                    CompletedDisposal
                    PendingDisposal
                ] do
                for body in bodies do
                    let completionName =
                        match completion with
                        | CompletedDisposal -> "completed disposal"
                        | PendingDisposal -> "pending disposal"

                    testCaseTask
                        $"%s{builder}, %A{execution}, %s{site}, %A{outcome}, %s{completionName}, %A{body}"
                        (fun () -> assertOutcome expected outcome completion body start)
    ]

    let private useCases builder execution bodies expected useResource =
        cases
            builder
            execution
            "use"
            bodies
            expected
            (fun body cts disposeAsync ->
                useResource body cts (TestHelpers.makeAsyncDisposable disposeAsync)
            )

    let useTests
        (builder: string)
        (execution: Execution)
        (bodies: DisposalBody list)
        (expected: DisposalOutcome -> DisposalBody -> ObservedResult)
        (useResource: DisposalBody -> CancellationTokenSource -> IAsyncDisposable -> Task)
        =
        testList
            $"%s{builder} %A{execution}"
            (useCases builder execution bodies expected useResource)

    let tests
        (builder: string)
        (execution: Execution)
        (bodies: DisposalBody list)
        (expected: DisposalOutcome -> DisposalBody -> ObservedResult)
        (useResource: DisposalBody -> CancellationTokenSource -> IAsyncDisposable -> Task)
        (forItems: DisposalBody -> CancellationTokenSource -> IAsyncEnumerable<int> -> Task)
        =
        testList $"%s{builder} %A{execution}" [
            yield! useCases builder execution bodies expected useResource
            yield!
                cases
                    builder
                    execution
                    "for"
                    bodies
                    expected
                    (fun body cts disposeAsync ->
                        forItems body cts (singleItemAsyncEnumerable disposeAsync)
                    )
        ]
