namespace IcedTasks.Tests

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open Expecto
open IcedTasks

// The continuation runs at once on the calling thread, so all allocations of a run are on the test thread.
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

module SuspensionAllocationTests =

    let private allocatedBytes (run: unit -> unit) =
        run ()
        let before = GC.GetAllocatedBytesForCurrentThread()
        run ()

        GC.GetAllocatedBytesForCurrentThread()
        - before

    let private expectNoAllocationPerSuspension (run: bool -> int -> unit) =
        let tenMore suspend =
            allocatedBytes (fun () -> run suspend 22)
            - allocatedBytes (fun () -> run suspend 12)

        let suspending = tenMore true
        let completed = tenMore false

        Expect.equal
            suspending
            completed
            $"Ten more suspensions must allocate no more than ten more binds of a completed awaiter. Suspending: {suspending} B, completed: {completed} B."

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

    let private suspendingTaskUnit suspend n =
        taskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
                i <- i + 1
        }

    let private suspendingBackgroundTaskUnit suspend n =
        backgroundTaskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
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

    let private suspendingValueTaskUnit suspend n =
        valueTaskUnit {
            let mutable i = 0

            while i < n do
                do! InlineResumeAwaiter(suspend)
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

    let private result (task: Task<int>) n =
        Expect.equal (task.GetAwaiter().GetResult()) n "The builder must resume after each bind."

    let private complete (task: Task) = task.GetAwaiter().GetResult()

    let private valueResult (task: ValueTask<int>) n =
        Expect.equal (task.GetAwaiter().GetResult()) n "The builder must resume after each bind."

    let private valueComplete (task: ValueTask) = task.GetAwaiter().GetResult()

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
                    complete (suspendingTaskUnit suspend n)
                )
            testCase "backgroundTaskUnit"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    complete (suspendingBackgroundTaskUnit suspend n)
                )
            testCase "valueTask"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueResult (suspendingValueTask suspend n) n
                )
            testCase "valueTaskUnit"
            <| fun () ->
                expectNoAllocationPerSuspension (fun suspend n ->
                    valueComplete (suspendingValueTaskUnit suspend n)
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
        ]
