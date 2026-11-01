namespace IcedTasks.Tests

open System
open System.Runtime.ExceptionServices
open System.Threading
open System.Threading.Tasks
open Expecto
open IcedTasks

/// Recursion that blocks on its own tasks (sync over async). Each case completes today without a
/// trampoline, because every start runs inline. A trampoline must not turn any of them into a deadlock.
module SyncOverAsyncTests =

    let largeStack =
        64
        * 1024
        * 1024

    /// A small stack makes the trampoline bounce at the depths below.
    let smallStack = 512 * 1024

    let private stackSize = AsyncLocal<int>()

    /// Runs `body` on a dedicated thread and fails the test if it does not finish in time.
    /// A deadlocked thread is a background thread, so it does not stop the test run.
    /// The stack is large unless `withStack` sets another size.
    let runGuarded (body: unit -> 'T) : 'T =
        let timeout = TimeSpan.FromSeconds 10.
        let result = ref ValueNone

        let thread =
            Thread(
                (fun () ->
                    result.Value <-
                        try
                            ValueSome(Ok(body ()))
                        with e ->
                            ValueSome(Error(ExceptionDispatchInfo.Capture e))
                ),
                (if stackSize.Value = 0 then largeStack else stackSize.Value),
                IsBackground = true
            )

        thread.Start()

        if not (thread.Join timeout) then
            failtestf "Deadlock: the computation did not finish in %O" timeout

        match result.Value with
        | ValueSome(Ok value) -> value
        | ValueSome(Error error) ->
            error.Throw()
            Unchecked.defaultof<_>
        | ValueNone -> failtest "The computation thread ended without a result"

    let withStack size (body: unit -> 'T) =
        let previous = stackSize.Value
        stackSize.Value <- size

        try
            body ()
        finally
            stackSize.Value <- previous

    /// Cases that do not block under a SynchronizationContext run on both stacks. On the small stack the
    /// builders bounce, where they would overflow without the trampoline.
    let stacks = [
        "large stack", largeStack
        "small stack", smallStack
    ]

    let waits: (string * (Task<int> -> int)) list = [
        "Result", (fun t -> t.Result)
        "Wait",
        (fun t ->
            t.Wait()
            t.Result
        )
        "GetAwaiter().GetResult()", (fun t -> t.GetAwaiter().GetResult())
        "Async.RunSynchronously", (fun t -> Async.RunSynchronously(Async.AwaitTask t))
    ]

    let depths = [
        100
        1_000
    ]

    /// A SynchronizationContext that drops every Post, like a UI thread that is blocked on the outer task.
    type BlockedContext() =
        inherit SynchronizationContext()
        override _.Post(_, _) = ()
        override _.Send(callback, state) = callback.Invoke state
        override this.CreateCopy() = this

    let block (wait: Task<int> -> int) (start: unit -> Task<int>) =
        runGuarded (fun () -> wait (start ()))

    let blockUnderContext (start: unit -> Task<int>) =
        runGuarded (fun () ->
            use _ = TestHelpers.setSyncContext (BlockedContext())
            (start ()).GetAwaiter().GetResult()
        )

    module Cold =

        /// Every level is let! bound: the trampoline can bounce here.
        let rec bound n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                else
                    let! x = bound (n - 1)
                    return x + 1
            }

        /// Every level starts the child from user code and blocks on it.
        let rec blocking (wait: Task<int> -> int) n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                else
                    return
                        wait (blocking wait (n - 1) ())
                        + 1
            }

        /// Odd levels block, even levels let! bind. A bounced child can end up behind a blocked frame.
        let rec mixed (wait: Task<int> -> int) n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                elif n % 2 = 0 then
                    let! x = mixed wait (n - 1)
                    return x + 1
                else
                    return
                        wait (mixed wait (n - 1) ())
                        + 1
            }

        /// The let! bound function is not an IcedTasks value: it blocks on one inside.
        let rec wrapped (wait: Task<int> -> int) n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                else
                    let! x = fun () -> Task.FromResult(wait (wrapped wait (n - 1) ()))
                    return x + 1
            }

        /// A let! of a plain function, then a blocking start from user code in the same frame.
        let rec afterPlainBind (wait: Task<int> -> int) n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                else
                    let! zero = fun () -> Task.FromResult 0

                    return
                        wait (afterPlainBind wait (n - 1) ())
                        + zero
                        + 1
            }

        /// Completing a TaskCompletionSource resumes an awaiting computation inline. It let! binds
        /// deep recursion, then the completing frame blocks on it.
        let resumeInlineThenBlock depth : ColdTask<int> =
            let source = TaskCompletionSource<int>()

            let waiter =
                coldTask {
                    let! v = source.Task
                    let! r = bound depth
                    return r + v
                }

            coldTask {
                let started = waiter ()
                let! x = bound depth
                source.SetResult 1

                return
                    started.GetAwaiter().GetResult()
                    + x
            }

        /// Like resumeInlineThenBlock, but a let! starts the computation that the source resumes. Such a
        /// computation can also bounce before it sets its result.
        let resumeInlineNestedThenBlock depth : ColdTask<int> =
            let source = TaskCompletionSource<int>()

            let resumed =
                coldTask {
                    let! v = source.Task
                    let! r = bound depth
                    return r + v
                }

            let waiter =
                coldTask {
                    let! r = resumed
                    return r
                }

            coldTask {
                let started = waiter ()
                let! x = bound depth
                source.SetResult 1

                return
                    started.GetAwaiter().GetResult()
                    + x
            }

    module Cancellable =

        let rec bound n : CancellableTask<int> =
            cancellableTask {
                if n = 0 then
                    return 0
                else
                    let! x = bound (n - 1)
                    return x + 1
            }

        let rec mixed (wait: Task<int> -> int) n : CancellableTask<int> =
            cancellableTask {
                if n = 0 then
                    return 0
                elif n % 2 = 0 then
                    let! x = mixed wait (n - 1)
                    return x + 1
                else
                    let! ct = CancellableTask.getCancellationToken ()

                    return
                        wait (mixed wait (n - 1) ct)
                        + 1
            }

        let rec wrapped (wait: Task<int> -> int) n : CancellableTask<int> =
            cancellableTask {
                if n = 0 then
                    return 0
                else
                    let! x =
                        fun (ct: CancellationToken) ->
                            Task.FromResult(wait (wrapped wait (n - 1) ct))

                    return x + 1
            }

        let rec afterPlainBind (wait: Task<int> -> int) n : CancellableTask<int> =
            cancellableTask {
                if n = 0 then
                    return 0
                else
                    let! zero = fun (_: CancellationToken) -> Task.FromResult 0
                    let! ct = CancellableTask.getCancellationToken ()

                    return
                        wait (afterPlainBind wait (n - 1) ct)
                        + zero
                        + 1
            }

        /// Mutual recursion with coldTask: blocking on the cold side, let! on the cancellable side.
        let rec mutualBound (wait: Task<int> -> int) n : CancellableTask<int> =
            cancellableTask {
                if n = 0 then
                    return 0
                else
                    let! x = mutualBlocking wait (n - 1)
                    return x + 1
            }

        and mutualBlocking (wait: Task<int> -> int) n : ColdTask<int> =
            coldTask {
                if n = 0 then
                    return 0
                else
                    return
                        wait (mutualBound wait (n - 1) CancellationToken.None)
                        + 1
            }

        let resumeInlineThenBlock depth : CancellableTask<int> =
            let source = TaskCompletionSource<int>()

            let waiter =
                cancellableTask {
                    let! v = source.Task
                    let! r = bound depth
                    return r + v
                }

            cancellableTask {
                let! ct = CancellableTask.getCancellationToken ()
                let started = waiter ct
                let! x = bound depth
                source.SetResult 1

                return
                    started.GetAwaiter().GetResult()
                    + x
            }

    module CancellableValue =

        let rec bound n : CancellableValueTask<int> =
            cancellableValueTask {
                if n = 0 then
                    return 0
                else
                    let! x = bound (n - 1)
                    return x + 1
            }

        let rec mixed (wait: Task<int> -> int) n : CancellableValueTask<int> =
            cancellableValueTask {
                if n = 0 then
                    return 0
                elif n % 2 = 0 then
                    let! x = mixed wait (n - 1)
                    return x + 1
                else
                    let! ct = CancellableValueTask.getCancellationToken ()

                    return
                        wait ((mixed wait (n - 1) ct).AsTask())
                        + 1
            }

        let rec afterPlainBind (wait: Task<int> -> int) n : CancellableValueTask<int> =
            cancellableValueTask {
                if n = 0 then
                    return 0
                else
                    let! zero = fun (_: CancellationToken) -> Task.FromResult 0
                    let! ct = CancellableValueTask.getCancellationToken ()

                    return
                        wait ((afterPlainBind wait (n - 1) ct).AsTask())
                        + zero
                        + 1
            }

    let forEachWaitAndDepth name (case: (Task<int> -> int) -> int -> int) =
        testList name [
            for stackName, stack in stacks do
                for waitName, wait in waits do
                    // A start from user code stays on the stack, so depth 1000 overflows the small stack.
                    for depth in depths do
                        if
                            stack = largeStack
                            || depth < 1000
                        then
                            testCase $"{stackName}, {waitName}, depth {depth}"
                            <| fun () ->
                                Expect.equal (withStack stack (fun () -> case wait depth)) depth ""
        ]

    let forEachDepth name (case: int -> int) =
        testList name [
            for depth in depths do
                testCase $"depth {depth}"
                <| fun () -> Expect.equal (case depth) depth ""
        ]

    let forEachStackAndDepth name (case: int -> Task<int>) =
        testList name [
            for stackName, stack in stacks do
                for depth in depths do
                    testCase $"{stackName}, depth {depth}"
                    <| fun () ->
                        let actual =
                            withStack
                                stack
                                (fun () ->
                                    runGuarded (fun () -> (case depth).GetAwaiter().GetResult())
                                )

                        Expect.equal
                            actual
                            (2 * depth
                             + 1)
                            ""
        ]

    /// Uses the stack until the runtime reports that it is low, then calls `body`.
    let rec private atLowStackFrom depth (body: unit -> int) : int =
        if System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack() then
            let result = atLowStackFrom (depth + 1) body
            // Work after the call keeps this call out of tail position, so the stack grows.
            if depth < 0 then 0 else result
        else
            body ()

    let private atLowStack body = atLowStackFrom 0 body

    let private lockAtLowStack =
        testCase "A lock is held across a start at low stack"
        <| fun () ->
            let gate = obj ()

            let body () =
                lock
                    gate
                    (fun () ->
                        let child = coldTask { return lock gate (fun () -> 1) }
                        child().Result
                    )

            Expect.equal (runGuarded (fun () -> atLowStack body)) 1 ""

    let private threadAtLowStack =
        testCase "A start at low stack stays on its thread"
        <| fun () ->
            let body () =
                let thread = Environment.CurrentManagedThreadId
                let child = coldTask { return Environment.CurrentManagedThreadId }

                if child().Result = thread then 1 else 0

            Expect.equal (runGuarded (fun () -> atLowStack body)) 1 ""

    let coldTaskTests =
        testList "coldTask" [
            lockAtLowStack
            threadAtLowStack
            forEachWaitAndDepth "Block on a user started child"
            <| fun wait depth -> block wait (Cold.blocking wait depth)
            forEachWaitAndDepth "Block and let! on alternate levels"
            <| fun wait depth -> block wait (Cold.mixed wait depth)
            forEachWaitAndDepth "let! a plain function that blocks"
            <| fun wait depth -> block wait (Cold.wrapped wait depth)
            forEachWaitAndDepth "Block after a let! of a plain function"
            <| fun wait depth -> block wait (Cold.afterPlainBind wait depth)
            forEachStackAndDepth "Inline resume from a TaskCompletionSource, then block"
            <| fun depth -> Cold.resumeInlineThenBlock depth ()
            forEachStackAndDepth "Inline resume of a let! started computation, then block"
            <| fun depth -> Cold.resumeInlineNestedThenBlock depth ()
            forEachDepth "let! recursion under a blocked SynchronizationContext"
            <| fun depth -> blockUnderContext (Cold.bound depth)
            forEachDepth "Block and let! under a blocked SynchronizationContext"
            <| fun depth ->
                blockUnderContext (Cold.mixed (fun t -> t.GetAwaiter().GetResult()) depth)
        ]

    let cancellableTaskTests =
        testList "cancellableTask" [
            forEachWaitAndDepth "Block and let! on alternate levels"
            <| fun wait depth ->
                block wait (fun () -> Cancellable.mixed wait depth CancellationToken.None)
            forEachWaitAndDepth "let! a plain function that blocks"
            <| fun wait depth ->
                block wait (fun () -> Cancellable.wrapped wait depth CancellationToken.None)
            forEachWaitAndDepth "Block after a let! of a plain function"
            <| fun wait depth ->
                block wait (fun () -> Cancellable.afterPlainBind wait depth CancellationToken.None)
            forEachWaitAndDepth "Mutual recursion with coldTask, blocking on the cold side"
            <| fun wait depth ->
                block wait (fun () -> Cancellable.mutualBound wait depth CancellationToken.None)
            forEachStackAndDepth "Inline resume from a TaskCompletionSource, then block"
            <| fun depth -> Cancellable.resumeInlineThenBlock depth CancellationToken.None
            forEachDepth "let! recursion under a blocked SynchronizationContext"
            <| fun depth ->
                blockUnderContext (fun () -> Cancellable.bound depth CancellationToken.None)
        ]

    let cancellableValueTaskTests =
        testList "cancellableValueTask" [
            forEachWaitAndDepth "Block and let! on alternate levels"
            <| fun wait depth ->
                block
                    wait
                    (fun () -> (CancellableValue.mixed wait depth CancellationToken.None).AsTask())
            forEachWaitAndDepth "Block after a let! of a plain function"
            <| fun wait depth ->
                block
                    wait
                    (fun () ->
                        (CancellableValue.afterPlainBind wait depth CancellationToken.None)
                            .AsTask()
                    )
            forEachDepth "let! recursion under a blocked SynchronizationContext"
            <| fun depth ->
                blockUnderContext (fun () ->
                    (CancellableValue.bound depth CancellationToken.None).AsTask()
                )
        ]

    [<Tests>]
    let syncOverAsyncTests =
        testList "IcedTasks.SyncOverAsync" [
            coldTaskTests
            cancellableTaskTests
            cancellableValueTaskTests
        ]
