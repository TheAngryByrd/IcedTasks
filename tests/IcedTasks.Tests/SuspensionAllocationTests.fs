namespace IcedTasks.Tests

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open System.Runtime.ExceptionServices
open Expecto
open IcedTasks

// Each continuation below runs at once on the calling thread, so all allocations of a run are on the test thread.
[<Struct; NoEquality; NoComparison>]
type InlineResumeAwaiter(suspend: bool) =
    member _.IsCompleted = not suspend
    member _.GetResult() = ()

    interface ICriticalNotifyCompletion with
        member _.OnCompleted(continuation) = continuation.Invoke()
        member _.UnsafeOnCompleted(continuation) = continuation.Invoke()

[<Struct; NoEquality; NoComparison>]
type InlineResume(suspend: bool) =
    member _.GetAwaiter() = InlineResumeAwaiter(suspend)

[<Sealed>]
type InlineDisposable(suspend: bool) =
    let mutable pending = false
    let mutable registrations = 0

    member _.Registrations = registrations

    interface IValueTaskSource with
        member _.GetStatus _ =
            if pending then
                ValueTaskSourceStatus.Pending
            else
                ValueTaskSourceStatus.Succeeded

        member _.GetResult _ = ()

        member _.OnCompleted(continuation, state, _, _) =
            registrations <-
                registrations
                + 1

            pending <- false
            continuation.Invoke(state)

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            pending <- suspend
            ValueTask(this :> IValueTaskSource, 0s)

module SuspensionAllocationTests =

    let private allocatedBytes (run: unit -> unit) =
        run ()
        let before = GC.GetAllocatedBytesForCurrentThread()
        run ()

        GC.GetAllocatedBytesForCurrentThread()
        - before

    // A new thread has no SynchronizationContext and uses the default TaskScheduler, so the background builders start inline.
    let private onNewThread (measure: unit -> unit) =
        let mutable failure = None

        let thread =
            Thread(fun () ->
                try
                    measure ()
                with e ->
                    failure <- Some(ExceptionDispatchInfo.Capture e)
            )

        thread.IsBackground <- true
        thread.Start()
        Expect.isTrue (thread.Join(TimeSpan.FromSeconds 30.)) "The measurement must finish in 30 s."

        failure
        |> Option.iter (fun e -> e.Throw())

    // AsyncTaskMethodBuilder caches the Task<int> of results up to 8, so both runs return larger results.
    let private expectNoAllocationPerSuspension (run: bool -> int -> unit) =
        onNewThread (fun () ->
            let tenMore suspend =
                allocatedBytes (fun () -> run suspend 22)
                - allocatedBytes (fun () -> run suspend 12)

            let suspending = tenMore true
            let completed = tenMore false

            Expect.equal
                suspending
                completed
                $"Ten more suspensions must allocate the same bytes as ten more binds of a completed awaiter. Suspending: {suspending} B, completed: {completed} B."
        )

    let private polyfillTask suspend n =
        IcedTasks.Polyfill.Task.Tasks.TaskBuilder.task {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private polyfillBackgroundTask suspend n =
        IcedTasks.Polyfill.Task.Tasks.TaskBuilder.backgroundTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingTaskUnit (count: int ref) suspend n =
        taskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)

                count.Value <-
                    count.Value
                    + 1

                i <- i + 1
        }

    let private suspendingBackgroundTaskUnit (count: int ref) suspend n =
        backgroundTaskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)

                count.Value <-
                    count.Value
                    + 1

                i <- i + 1
        }

    let private suspendingValueTask suspend n =
        valueTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingValueTaskUnit (count: int ref) suspend n =
        valueTaskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)

                count.Value <-
                    count.Value
                    + 1

                i <- i + 1
        }

    let private suspendingPoolingValueTask suspend n =
        poolingValueTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingCancellableTask suspend n =
        cancellableTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingCancellableTaskWithToken suspend n =
        cancellableTask {
            let mutable i = 0

            while i < n do
                do! fun (_: CancellationToken) -> InlineResume(suspend)
                i <- i + 1

            return i
        }

    let private suspendingBackgroundCancellableTask suspend n =
        backgroundCancellableTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingCancellableValueTask suspend n =
        cancellableValueTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private suspendingCancellablePoolingValueTask suspend n =
        cancellablePoolingValueTask {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1

            return i
        }

    let private disposingValueTask (resource: IAsyncDisposable) n =
        valueTask {
            let mutable i = 0

            while i < n do
                use _ = resource
                i <- i + 1

            return i
        }

    let private disposingCancellableTask (resource: IAsyncDisposable) n =
        cancellableTask {
            let mutable i = 0

            while i < n do
                use _ = resource
                i <- i + 1

            return i
        }

    let private disposingCancellableValueTask (resource: IAsyncDisposable) n =
        cancellableValueTask {
            let mutable i = 0

            while i < n do
                use _ = resource
                i <- i + 1

            return i
        }

    let private result (task: Task<int>) n =
        Expect.isTrue
            task.IsCompleted
            "The builder must resume on the calling thread after each bind."

        Expect.equal task.Result n "The builder must run each iteration."

    let private complete (start: int ref -> Task) n =
        let count = ref 0
        let task = start count

        Expect.isTrue
            task.IsCompleted
            "The builder must resume on the calling thread after each bind."

        task.GetAwaiter().GetResult()
        Expect.equal count.Value n "The builder must run each iteration."

    let private valueResult (task: ValueTask<int>) n =
        Expect.isTrue
            task.IsCompleted
            "The builder must resume on the calling thread after each bind."

        Expect.equal (task.GetAwaiter().GetResult()) n "The builder must run each iteration."

    let private valueComplete (start: int ref -> ValueTask) n =
        let count = ref 0
        let task = start count

        Expect.isTrue
            task.IsCompleted
            "The builder must resume on the calling thread after each bind."

        task.GetAwaiter().GetResult()
        Expect.equal count.Value n "The builder must run each iteration."

    let private disposed suspend n (start: IAsyncDisposable -> Task<int>) =
        let resource = InlineDisposable suspend
        result (start resource) n

        Expect.equal
            resource.Registrations
            (if suspend then n else 0)
            "Each pending DisposeAsync must register one continuation."

    [<Tests>]
    let tests =
        testList "A suspension does not box the awaiter" [
            testCase "task"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n -> result (polyfillTask suspend n) n)
            testCase "backgroundTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    result (polyfillBackgroundTask suspend n) n
                )
            testCase "taskUnit"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    complete (fun count -> suspendingTaskUnit count suspend n) n
                )
            testCase "backgroundTaskUnit"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    complete (fun count -> suspendingBackgroundTaskUnit count suspend n) n
                )
            testCase "valueTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueResult (suspendingValueTask suspend n) n
                )
            testCase "valueTaskUnit"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueComplete (fun count -> suspendingValueTaskUnit count suspend n) n
                )
            testCase "poolingValueTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueResult (suspendingPoolingValueTask suspend n) n
                )
            testCase "cancellableTask, bind of an awaiter"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    result (suspendingCancellableTask suspend n CancellationToken.None) n
                )
            testCase "cancellableTask, bind of a function that takes the token"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    result (suspendingCancellableTaskWithToken suspend n CancellationToken.None) n
                )
            testCase "backgroundCancellableTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    result (suspendingBackgroundCancellableTask suspend n CancellationToken.None) n
                )
            testCase "cancellableValueTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueResult (suspendingCancellableValueTask suspend n CancellationToken.None) n
                )
            testCase "cancellablePoolingValueTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueResult
                        (suspendingCancellablePoolingValueTask suspend n CancellationToken.None)
                        n
                )
            testCase "valueTask, use of an IAsyncDisposable"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    disposed suspend n (fun resource -> (disposingValueTask resource n).AsTask())
                )
            testCase "cancellableTask, use of an IAsyncDisposable"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    disposed
                        suspend
                        n
                        (fun resource ->
                            disposingCancellableTask resource n CancellationToken.None
                        )
                )
            testCase "cancellableValueTask, use of an IAsyncDisposable"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    disposed
                        suspend
                        n
                        (fun resource ->
                            (disposingCancellableValueTask resource n CancellationToken.None)
                                .AsTask()
                        )
                )
        ]
