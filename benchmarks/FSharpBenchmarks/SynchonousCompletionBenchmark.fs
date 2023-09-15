namespace IcedTasks.Benchmarks

open System
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open System.Threading
open System.Threading.Tasks

open IcedTasks

open System.IO

[<AutoOpen>]
module SyncHelpers =

    let sync_Task () = Task.FromResult 100

    let sync_ValueTask () = ValueTask<int>(100)

    let sync_Async () = async.Return 100

    let sync_cancellableTask () =
        fun (ct: CancellationToken) -> Task.FromResult 100

    let sync_cancellableValueTask () =
        fun (ct: CancellationToken) -> ValueTask<int>(100)

    // ===== F# Ply TaskBuilder =====

    let fsharp_tenBindSync_plyTaskBuilder_BindTask () =
        FSharp.Control.Tasks.Affine.task {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_tenBindSync_plyTaskBuilder_BindValueTask () =
        FSharp.Control.Tasks.Affine.task {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    // ==== F# Ply ValueTaskBuilder ===

    let fsharp_tenBindSync_plyValueTaskBuilder_BindTask () =
        FSharp.Control.Tasks.Affine.vtask {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_tenBindSync_plyValueTaskBuilder_BindValueTask () =
        FSharp.Control.Tasks.Affine.vtask {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    // ===== F# AsyncBuilder =====
    let fsharp_TenBindSync_AsyncBuilder_BindAsync () =
        async {
            let! res1 = sync_Async ()
            let! res2 = sync_Async ()
            let! res3 = sync_Async ()
            let! res4 = sync_Async ()
            let! res5 = sync_Async ()
            let! res6 = sync_Async ()
            let! res7 = sync_Async ()
            let! res8 = sync_Async ()
            let! res9 = sync_Async ()
            let! res10 = sync_Async ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    // ==== F# TaskBuilder =====

    let fsharp_TenBindSync_TaskBuilder_BindTask () =
        task {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    let fsharp_TenBindSync_TaskBuilder_BindValueTask () =
        task {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    let fsharp_TenBindSync_TaskBuilder_BindAsync () =
        task {
            let! res1 = sync_Async ()
            let! res2 = sync_Async ()
            let! res3 = sync_Async ()
            let! res4 = sync_Async ()
            let! res5 = sync_Async ()
            let! res6 = sync_Async ()
            let! res7 = sync_Async ()
            let! res8 = sync_Async ()
            let! res9 = sync_Async ()
            let! res10 = sync_Async ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    // ==== F# ValueTaskBuilder =====

    let fsharp_TenBindSync_ValueTaskBuilder_BindTask () =
        valueTask {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_ValueTaskBuilder_BindValueTask () =
        valueTask {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    let fsharp_TenBindSync_ValueTaskBuilder_BindAsync () =
        valueTask {
            let! res1 = sync_Async ()
            let! res2 = sync_Async ()
            let! res3 = sync_Async ()
            let! res4 = sync_Async ()
            let! res5 = sync_Async ()
            let! res6 = sync_Async ()
            let! res7 = sync_Async ()
            let! res8 = sync_Async ()
            let! res9 = sync_Async ()
            let! res10 = sync_Async ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    /// ==== F# CancellableTaskBuilder =====

    let fsharp_TenBindSync_CancellableTaskBuilder_BindCancellableTask () =
        cancellableTask {
            let! res1 = sync_cancellableTask ()
            let! res2 = sync_cancellableTask ()
            let! res3 = sync_cancellableTask ()
            let! res4 = sync_cancellableTask ()
            let! res5 = sync_cancellableTask ()
            let! res6 = sync_cancellableTask ()
            let! res7 = sync_cancellableTask ()
            let! res8 = sync_cancellableTask ()
            let! res9 = sync_cancellableTask ()
            let! res10 = sync_cancellableTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableTaskBuilder_BindTask () =
        cancellableTask {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableTaskBuilder_BindValueTask () =
        cancellableTask {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableTaskBuilder_BindCancellableValueTask () =
        cancellableTask {
            let! res1 = sync_cancellableValueTask ()
            let! res2 = sync_cancellableValueTask ()
            let! res3 = sync_cancellableValueTask ()
            let! res4 = sync_cancellableValueTask ()
            let! res5 = sync_cancellableValueTask ()
            let! res6 = sync_cancellableValueTask ()
            let! res7 = sync_cancellableValueTask ()
            let! res8 = sync_cancellableValueTask ()
            let! res9 = sync_cancellableValueTask ()
            let! res10 = sync_cancellableValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableTaskBuilder_BindAsync () =
        cancellableTask {
            let! res1 = sync_Async ()
            let! res2 = sync_Async ()
            let! res3 = sync_Async ()
            let! res4 = sync_Async ()
            let! res5 = sync_Async ()
            let! res6 = sync_Async ()
            let! res7 = sync_Async ()
            let! res8 = sync_Async ()
            let! res9 = sync_Async ()
            let! res10 = sync_Async ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

    // ==== F# CancellableValueTaskBuilder =====


    let fsharp_TenBindSync_CancellableValueTaskBuilder_BindCancellableTask () =
        cancellableValueTask {
            let! res1 = sync_cancellableTask ()
            let! res2 = sync_cancellableTask ()
            let! res3 = sync_cancellableTask ()
            let! res4 = sync_cancellableTask ()
            let! res5 = sync_cancellableTask ()
            let! res6 = sync_cancellableTask ()
            let! res7 = sync_cancellableTask ()
            let! res8 = sync_cancellableTask ()
            let! res9 = sync_cancellableTask ()
            let! res10 = sync_cancellableTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableValueTaskBuilder_BindTask () =
        cancellableValueTask {
            let! res1 = sync_Task ()
            let! res2 = sync_Task ()
            let! res3 = sync_Task ()
            let! res4 = sync_Task ()
            let! res5 = sync_Task ()
            let! res6 = sync_Task ()
            let! res7 = sync_Task ()
            let! res8 = sync_Task ()
            let! res9 = sync_Task ()
            let! res10 = sync_Task ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableValueTaskBuilder_BindValueTask () =
        cancellableValueTask {
            let! res1 = sync_ValueTask ()
            let! res2 = sync_ValueTask ()
            let! res3 = sync_ValueTask ()
            let! res4 = sync_ValueTask ()
            let! res5 = sync_ValueTask ()
            let! res6 = sync_ValueTask ()
            let! res7 = sync_ValueTask ()
            let! res8 = sync_ValueTask ()
            let! res9 = sync_ValueTask ()
            let! res10 = sync_ValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableValueTaskBuilder_BindCancellableValueTask () =
        cancellableValueTask {
            let! res1 = sync_cancellableValueTask ()
            let! res2 = sync_cancellableValueTask ()
            let! res3 = sync_cancellableValueTask ()
            let! res4 = sync_cancellableValueTask ()
            let! res5 = sync_cancellableValueTask ()
            let! res6 = sync_cancellableValueTask ()
            let! res7 = sync_cancellableValueTask ()
            let! res8 = sync_cancellableValueTask ()
            let! res9 = sync_cancellableValueTask ()
            let! res10 = sync_cancellableValueTask ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }


    let fsharp_TenBindSync_CancellableValueTaskBuilder_BindAsync () =
        cancellableValueTask {
            let! res1 = sync_Async ()
            let! res2 = sync_Async ()
            let! res3 = sync_Async ()
            let! res4 = sync_Async ()
            let! res5 = sync_Async ()
            let! res6 = sync_Async ()
            let! res7 = sync_Async ()
            let! res8 = sync_Async ()
            let! res9 = sync_Async ()
            let! res10 = sync_Async ()

            return
                res1
                + res2
                + res3
                + res4
                + res5
                + res6
                + res7
                + res8
                + res9
                + res10
        }

[<MemoryDiagnoser>]
// [<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)>]
[<CategoriesColumn>]
type SyncCompletionBenchmarks() =


    // [<Params(1000)>]
    member val public manyIterations = manyIterationsConst with get, set

    [<BenchmarkCategory(NonAsyncBinds, csharp, taskBuilder, bindTask);
      Benchmark(Baseline = true, OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsSync_TaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                TaskPerfCSharp
                    .CSharp_TenBindsSync_TaskBuilder_BindTask()
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, csharp, taskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsSync_TaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                TaskPerfCSharp
                    .CSharp_TenBindsSync_TaskBuilder_BindValueTask()
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, csharp, valueTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsSync_ValueTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                TaskPerfCSharp
                    .CSharp_TenBindsSync_ValueTaskBuilder_BindTask()
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, csharp, valueTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsSync_ValueTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                TaskPerfCSharp
                    .CSharp_TenBindsSync_ValueTaskBuilder_BindValueTask()
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, asyncBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsSync_AsyncBuilder_BindAsync() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                fsharp_TenBindSync_AsyncBuilder_BindAsync ()
                |> Async.RunSynchronously

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, plyTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_plyTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindSync_plyTaskBuilder_BindTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, plyTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_plyTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindSync_plyTaskBuilder_BindValueTask().GetAwaiter().GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, plyValueTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_plyValueTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindSync_plyValueTaskBuilder_BindTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, plyValueTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_plyValueTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindSync_plyValueTaskBuilder_BindValueTask().GetAwaiter().GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, taskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_TaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_TaskBuilder_BindTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, taskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_TaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_TaskBuilder_BindValueTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, taskBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_TaskBuilder_BindAsync() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_TaskBuilder_BindAsync().GetAwaiter().GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, valueTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_ValueTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_ValueTaskBuilder_BindTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, valueTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_ValueTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_ValueTaskBuilder_BindValueTask().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, valueTaskBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_ValueTaskBuilder_BindAsync() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_TenBindSync_TaskBuilder_BindAsync().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableTaskBuilder, bindCancellableTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableTaskBuilder_BindCancellableTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableTaskBuilder_BindTask () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableTaskBuilder_BindValueTask () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableTaskBuilder, bindCancellableValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableTaskBuilder_BindCancellableValueTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableTaskBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableTaskBuilder_BindAsync() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableTaskBuilder_BindAsync () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableValueTaskBuilder, bindCancellableTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableValueTaskBuilder_BindCancellableTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableValueTaskBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableValueTaskBuilder_BindTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableValueTaskBuilder_BindTask () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z


    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableValueTaskBuilder, bindValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableValueTaskBuilder_BindValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableValueTaskBuilder_BindValueTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableValueTaskBuilder, bindCancellableValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableValueTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableValueTaskBuilder_BindCancellableValueTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(NonAsyncBinds, fsharp, cancellableValueTaskBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.Fsharp_TenBindSync_cancellableValueTaskBuilder_BindAsync() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_TenBindSync_CancellableValueTaskBuilder_BindAsync
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z
