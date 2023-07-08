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
module FileWriteHelpers =

    let getTempFileName () =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"))


[<MemoryDiagnoser>]
[<CategoriesColumn>]
type FileWriteBenchmarks() =
    // [<Params(1000)>]
    member val public manyIterations = manyIterationsConst with get, set


    [<Params(128)>]
    member val public bufferSize = 0 with get, set


    [<BenchmarkCategory("ManyWriteFile", csharp, taskBuilder);
      Benchmark(Baseline = true, OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_ManyWriteFile_TaskBuilder() =
        TaskPerfCSharp
            .ManyWriteFileAsync(x.manyIterations, x.bufferSize)
            .GetAwaiter()
            .GetResult()

    [<BenchmarkCategory("ManyWriteFile", csharp, valueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.CSharp_ManyWriteFile_ValueTaskBuilder() =
        TaskPerfCSharp
            .ManyWriteFileAsync_ValueTask(x.manyIterations, x.bufferSize)
            .GetAwaiter()
            .GetResult()


    [<BenchmarkCategory("ManyWriteFile", fsharp, plyTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_ply() =
        let path = getTempFileName ()

        (FSharp.Control.Tasks.Affine.task {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        })
            .GetAwaiter()
            .GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, plyTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_plyValueTask() =
        let path = getTempFileName ()

        (FSharp.Control.Tasks.Affine.vtask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        })
            .GetAwaiter()
            .GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, asyncBuilder, bindAsync);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_AsyncBuilder_BindAsync() =
        let path = getTempFileName ()

        (async {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.AsyncWrite(junk, 0, junk.Length)

        })
        |> Async.RunSynchronously

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, asyncBuilder, bindTask);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_AsyncBuilder_BindAsync_bindTask() =
        let path = getTempFileName ()

        (async {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! Async.AwaitTask(file.WriteAsync(junk, 0, junk.Length))

        })
        |> Async.RunSynchronously

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile", fsharp, taskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_TaskBuilder() =
        let path = getTempFileName ()

        (task {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        })
            .GetAwaiter()
            .GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, valueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_ValueTaskBuilder() =
        let path = getTempFileName ()

        (valueTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        })
            .GetAwaiter()
            .GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableTaskBuilder() =
        let path = getTempFileName ()

        let t = cancellableTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenOnce() =
        let path = getTempFileName ()

        let t = cancellableTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)
            let! ct = CancellableTask.getCancellationToken ()

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenMany() =
        let path = getTempFileName ()

        let t = cancellableTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                let! ct = CancellableTask.getCancellationToken ()
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenLambda() =
        let path = getTempFileName ()

        let t = cancellableTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)
        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableValueTaskBuilder() =
        let path = getTempFileName ()

        let t = cancellableValueTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)

        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenOnce() =
        let path = getTempFileName ()

        let t = cancellableValueTask {
            let! ct = CancellableValueTask.getCancellationToken ()
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenMany() =
        let path = getTempFileName ()

        let t = cancellableValueTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                let! ct = CancellableValueTask.getCancellationToken ()
                do! file.WriteAsync(junk, 0, junk.Length, ct)

        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile", fsharp, cancellableValueTaskBuilder);
      Benchmark(OperationsPerInvoke = manyIterationsConst)>]
    member x.FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenLambda() =
        let path = getTempFileName ()

        let t = cancellableValueTask {
            let junk = Array.zeroCreate x.bufferSize
            use file = File.Create(path)

            for i = 1 to x.manyIterations do
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)

        }

        (t CancellationToken.None).GetAwaiter().GetResult()

        File.Delete(path)
