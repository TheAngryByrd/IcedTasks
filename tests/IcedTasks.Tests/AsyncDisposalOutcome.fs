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

    type Execution =
        | Static
        | Dynamic

    type ObservedResult =
        | Completed
        | Canceled
        | Faulted of exceptions: string list

    let private bodyException () = ArgumentException "body"
    let private disposeException () = InvalidOperationException "dispose"

    let private timeout = TimeSpan.FromSeconds 10.

    let runBody body (cts: CancellationTokenSource) =
        match body with
        | BodyCompletes -> ()
        | BodyThrows -> raise (bodyException ())
        | BodyCancels -> cts.Cancel()

    type ObservedDisposalSource() =
        let mutable core = ManualResetValueTaskSourceCore<unit>()

        let continuationRegistered =
            TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)

        let mutable getResultCalls = 0
        do core.RunContinuationsAsynchronously <- true

        member _.ContinuationRegistered = continuationRegistered.Task
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
                getResultCalls <-
                    getResultCalls
                    + 1

                core.GetResult(token)

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

    let private observe (resultTask: Task) =
        match resultTask.Status with
        | TaskStatus.RanToCompletion -> Completed
        | TaskStatus.Canceled -> Canceled
        | _ ->
            match resultTask.Exception with
            | null -> Faulted []
            | e ->
                e.InnerExceptions
                |> Seq.map describe
                |> List.ofSeq
                |> Faulted

    type DisposalSite =
        | Use
        | For

    type DisposalCase = {
        Site: DisposalSite
        Outcome: DisposalOutcome
        Completion: DisposalCompletion
        Body: DisposalBody
    }

    let taskExpectation outcome body =
        match outcome, body with
        | DisposalFaults, _ -> Faulted [ "InvalidOperationException: dispose" ]
        | DisposalCancels, _ -> Canceled
        | DisposalSucceeds, BodyThrows -> Faulted [ "ArgumentException: body" ]
        | DisposalSucceeds, BodyCancels -> Canceled
        | DisposalSucceeds, BodyCompletes -> Completed

    let asyncExpectation outcome body =
        match outcome, body with
        | DisposalSucceeds, BodyCompletes -> Completed
        | DisposalSucceeds, _ -> Faulted [ "ArgumentException: body" ]
        | DisposalFaults, BodyCompletes -> Faulted [ "InvalidOperationException: dispose" ]
        | DisposalFaults, _ ->
            Faulted [
                "AggregateException [ArgumentException: body; InvalidOperationException: dispose]"
            ]
        | DisposalCancels, BodyCompletes -> Faulted [ "OperationCanceledException: dispose" ]
        | DisposalCancels, _ ->
            Faulted [
                "AggregateException [ArgumentException: body; OperationCanceledException: dispose]"
            ]

    let private waitFor (signal: Task) (failure: string) =
        task {
            try
                do! signal.WaitAsync(timeout)
            with :? TimeoutException ->
                failtest failure
        }

    let noKnownDefect (_: DisposalCase) : string option = None

    let private assertOutcome
        expected
        case
        (start: DisposalBody -> CancellationTokenSource -> (unit -> ValueTask) -> Task)
        =
        task {
            let source = ObservedDisposalSource()
            use cts = new CancellationTokenSource()

            if case.Completion = CompletedDisposal then
                source.Complete case.Outcome

            let resultTask = start case.Body cts source.ValueTask

            if case.Completion = PendingDisposal then
                do!
                    waitFor
                        source.ContinuationRegistered
                        "The builder did not await the pending DisposeAsync result"

                source.Complete case.Outcome

            do!
                waitFor
                    (resultTask.ContinueWith(fun (_: Task) -> ()))
                    "The builder result did not complete"

            Expect.equal
                (observe resultTask)
                (expected case.Outcome case.Body)
                "The DisposeAsync outcome must decide the result when it faults or cancels"

            Expect.equal
                source.GetResultCalls
                1
                "The builder must read the DisposeAsync result exactly once"
        }

    let private cases builder execution site bodies expected knownDefect start = [
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
                    let case = {
                        Site = site
                        Outcome = outcome
                        Completion = completion
                        Body = body
                    }

                    let siteName =
                        match site with
                        | Use -> "use"
                        | For -> "for"

                    let completionName =
                        match completion with
                        | CompletedDisposal -> "completed disposal"
                        | PendingDisposal -> "pending disposal"

                    let name =
                        $"%s{builder}, %A{execution}, %s{siteName}, %A{outcome}, %s{completionName}, %A{body}"

                    match knownDefect case with
                    | None -> testCaseTask name (fun () -> assertOutcome expected case start)
                    | Some defect ->
                        ptestCaseTask
                            $"%s{name}, pending: %s{defect}"
                            (fun () -> assertOutcome expected case start)
    ]

    let private useCases builder execution bodies expected knownDefect useResource =
        cases
            builder
            execution
            Use
            bodies
            expected
            knownDefect
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
            (useCases builder execution bodies expected noKnownDefect useResource)

    let testsWithKnownDefects
        (builder: string)
        (execution: Execution)
        (bodies: DisposalBody list)
        (expected: DisposalOutcome -> DisposalBody -> ObservedResult)
        (knownDefect: DisposalCase -> string option)
        (useResource: DisposalBody -> CancellationTokenSource -> IAsyncDisposable -> Task)
        (forItems: DisposalBody -> CancellationTokenSource -> IAsyncEnumerable<int> -> Task)
        =
        testList $"%s{builder} %A{execution}" [
            yield! useCases builder execution bodies expected knownDefect useResource
            yield!
                cases
                    builder
                    execution
                    For
                    bodies
                    expected
                    knownDefect
                    (fun body cts disposeAsync ->
                        forItems body cts (singleItemAsyncEnumerable disposeAsync)
                    )
        ]

    let tests builder execution bodies expected useResource forItems =
        testsWithKnownDefects builder execution bodies expected noKnownDefect useResource forItems
