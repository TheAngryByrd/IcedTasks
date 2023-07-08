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
module AsyncHelpers =


    let asyncYield () = Async.Sleep(0)
    let taskYield () = Task.Yield()

    let taskCTYield () =
        fun (ct: CancellationToken) -> Task.Yield()

    let fsharp_tenBindAsync_AsyncBuilder () = async {
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        do! asyncYield ()
        return 100
    }

    let fsharp_tenBindAsync_PlyTaskBuilder () =
        FSharp.Control.Tasks.Affine.task {
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            return 100
        }

    let fsharp_tenBindAsync_PlyValueTaskBuilder () =
        FSharp.Control.Tasks.Affine.vtask {
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            do! taskYield ()
            return 100
        }

    let fsharp_tenBindAsync_TaskBuilder () = task {
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        return 100
    }


    let fsharp_tenBindAsync_ValueTaskBuilder () = valueTask {
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        return 100
    }


    let fsharp_tenBindAsync_CancellableTaskBuilder () = cancellableTask {
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        return 100
    }


    let fsharp_tenBindAsync_CancellableTaskBuilder_BindCancellableTask () = cancellableTask {
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        return 100
    }

    let fsharp_tenBindAsync_CancellableValueTaskBuilder () = cancellableValueTask {
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        do! taskYield ()
        return 100
    }


    let fsharp_tenBindAsync_CancellableValueTaskBuilder_BindCancellableTask () = cancellableValueTask {
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        do! taskCTYield ()
        return 100
    }


[<MemoryDiagnoser>]
[<CategoriesColumn>]
type AsyncCompletionBenchmarks() =

    member val public manyIterations = manyIterationsConst with get, set

    [<BenchmarkCategory(AsyncBinds, csharp, taskBuilder);
      Benchmark(Baseline = true, OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsAsync_TaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- TaskPerfCSharp.CSharp_TenBindsAsync_TaskBuilder().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, csharp, valueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_TenBindsAsync_ValueTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- TaskPerfCSharp.CSharp_TenBindsAsync_ValueTaskBuilder().GetAwaiter().GetResult()

        z


    [<BenchmarkCategory(AsyncBinds, fsharp, asyncBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_AsyncBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                fsharp_tenBindAsync_AsyncBuilder ()
                |> Async.RunSynchronously

        z


    [<BenchmarkCategory(AsyncBinds, fsharp, plyTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_PlyTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindAsync_PlyTaskBuilder().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, plyValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_PlyValueTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindAsync_PlyValueTaskBuilder().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, taskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_TaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindAsync_TaskBuilder().GetAwaiter().GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, valueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_ValueTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <- fsharp_tenBindAsync_ValueTaskBuilder().GetAwaiter().GetResult()

        z


    [<BenchmarkCategory(AsyncBinds, fsharp, cancellableTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_CancellableTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_tenBindAsync_CancellableTaskBuilder () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, cancellableTaskBuilder, bindCancellableValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_CancellableTaskBuilder_BindCancellableTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_tenBindAsync_CancellableTaskBuilder_BindCancellableTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, cancellableValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_CancellableValueTaskBuilder() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_tenBindAsync_CancellableValueTaskBuilder () (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z

    [<BenchmarkCategory(AsyncBinds, fsharp, cancellableValueTaskBuilder, bindCancellableValueTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_TenBindsAsync_CancellableValueTaskBuilder_BindCancellableTask() =
        let mutable z = 0

        for i in 1 .. x.manyIterations do
            z <-
                (fsharp_tenBindAsync_CancellableValueTaskBuilder_BindCancellableTask
                    ()
                    (CancellationToken.None))
                    .GetAwaiter()
                    .GetResult()

        z
