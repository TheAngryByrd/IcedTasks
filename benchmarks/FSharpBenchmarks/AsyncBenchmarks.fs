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


type DemoBenchmarks() =
    [<Params(0, 1, 15, 100)>]
    member val public sleepTime = 0 with get, set

    // [<GlobalSetup>]
    // member self.GlobalSetup() =
    //     printfn "%s" "Global Setup"

    // [<GlobalCleanup>]
    // member self.GlobalCleanup() =
    //     printfn "%s" "Global Cleanup"

    // [<IterationSetup>]
    // member self.IterationSetup() =
    //     printfn "%s" "Iteration Setup"

    // [<IterationCleanup>]
    // member self.IterationCleanup() =
    //     printfn "%s" "Iteration Cleanup"

    [<Benchmark>]
    member this.Thread() =
        System.Threading.Thread.Sleep(this.sleepTime)

    [<Benchmark>]
    member this.Task() =
        System.Threading.Tasks.Task.Delay(this.sleepTime)

    [<Benchmark>]
    member this.AsyncToTask() =
        Async.Sleep(this.sleepTime) |> Async.StartAsTask

    [<Benchmark>]
    member this.AsyncToSync() =
        Async.Sleep(this.sleepTime)
        |> Async.RunSynchronously


[<AutoOpen>]
module Helpers =
    let bufferSize = 128
    let manyIterations = 1000
    let syncTask () = Task.FromResult 100
    let syncCtTask (ct: CancellationToken) = Task.FromResult 100
    let syncTask_async () = async.Return 100
    let syncTask_async2 () = Task.FromResult 100
    let asyncYield () = Async.Sleep(0)
    let asyncTask () = Task.Yield()
    let asyncTaskCt (ct: CancellationToken) = Task.Yield()

    let tenBindSync_ply () =
        FSharp.Control.Tasks.Affine.task {
            let! res1 = syncTask ()
            let! res2 = syncTask ()
            let! res3 = syncTask ()
            let! res4 = syncTask ()
            let! res5 = syncTask ()
            let! res6 = syncTask ()
            let! res7 = syncTask ()
            let! res8 = syncTask ()
            let! res9 = syncTask ()
            let! res10 = syncTask ()

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

    let tenBindSync_async () =
        async {
            let! res1 = syncTask_async ()
            let! res2 = syncTask_async ()
            let! res3 = syncTask_async ()
            let! res4 = syncTask_async ()
            let! res5 = syncTask_async ()
            let! res6 = syncTask_async ()
            let! res7 = syncTask_async ()
            let! res8 = syncTask_async ()
            let! res9 = syncTask_async ()
            let! res10 = syncTask_async ()

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

    let tenBindSync_task () =
        task {
            let! res1 = syncTask ()
            let! res2 = syncTask ()
            let! res3 = syncTask ()
            let! res4 = syncTask ()
            let! res5 = syncTask ()
            let! res6 = syncTask ()
            let! res7 = syncTask ()
            let! res8 = syncTask ()
            let! res9 = syncTask ()
            let! res10 = syncTask ()

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


    let tenBindSync_coldTask_bindTask () =
        coldTask {
            let! res1 = syncTask ()
            let! res2 = syncTask ()
            let! res3 = syncTask ()
            let! res4 = syncTask ()
            let! res5 = syncTask ()
            let! res6 = syncTask ()
            let! res7 = syncTask ()
            let! res8 = syncTask ()
            let! res9 = syncTask ()
            let! res10 = syncTask ()

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

    let tenBindSync_coldTask_bindColdTask () =
        coldTask {
            let! res1 = syncTask
            let! res2 = syncTask
            let! res3 = syncTask
            let! res4 = syncTask
            let! res5 = syncTask
            let! res6 = syncTask
            let! res7 = syncTask
            let! res8 = syncTask
            let! res9 = syncTask
            let! res10 = syncTask

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


    let tenBindSync_cancellableTask_bindTask () =
        cancellableTask {
            let! res1 = syncTask ()
            let! res2 = syncTask ()
            let! res3 = syncTask ()
            let! res4 = syncTask ()
            let! res5 = syncTask ()
            let! res6 = syncTask ()
            let! res7 = syncTask ()
            let! res8 = syncTask ()
            let! res9 = syncTask ()
            let! res10 = syncTask ()

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


    let tenBindSync_cancellableTask_bindCancellableTask () =
        cancellableTask {
            let! res1 = syncCtTask
            let! res2 = syncCtTask
            let! res3 = syncCtTask
            let! res4 = syncCtTask
            let! res5 = syncCtTask
            let! res6 = syncCtTask
            let! res7 = syncCtTask
            let! res8 = syncCtTask
            let! res9 = syncCtTask
            let! res10 = syncCtTask

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


    let tenBindAsync_ply () =
        FSharp.Control.Tasks.Affine.task {
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
        }

    let tenBindAsync_async () =
        async {
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
        }

    let tenBindAsync_task () =
        task {
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
        }


    let tenBindAsync_coldTask_bindTask () =
        coldTask {
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
        }



    let tenBindAsync_coldTask_bindColdTask () =
        coldTask {
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
            do! asyncTask
        }




    let tenBindAsync_cancellableTask_bindTask () =
        cancellableTask {
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
            do! asyncTask ()
        }

    let tenBindAsync_cancellableTask_bindCancellableTask () =
        cancellableTask {
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
            do! asyncTaskCt
        }

[<MemoryDiagnoser>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type AsyncBenchmarks() =


    // ManyWriteFile

    [<BenchmarkCategory("ManyWriteFile"); Benchmark(Baseline = true)>]
    member _.ManyWriteFile_CSharpTasks() =
        TaskPerfCSharp
            .ManyWriteFileAsync(manyIterations)
            .Wait()


    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_ply() =
        let path = Path.GetTempFileName()

        FSharp.Control.Tasks.Affine.task {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_async() =
        let path = Path.GetTempFileName()

        async {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! Async.AwaitTask(file.WriteAsync(junk, 0, junk.Length))
        }
        |> Async.RunSynchronously

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_task() =
        let path = Path.GetTempFileName()

        task {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_coldTask() =
        let path = Path.GetTempFileName()

        coldTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t().Wait()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask() =
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t(CancellationToken.None).Wait()

        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask_withCancellation() =
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            let! ct = CancellableTask.getCancellationToken

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask_withCancellation2() =
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask_withCancellation3() =
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                let! ct = CancellableTask.getCancellationToken
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

        File.Delete(path)

    // NonAsyncBinds


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark(Baseline = true)>]
    member _.NonAsyncBinds_CSharpTasks() =
        for i in 1 .. manyIterations * 100 do
            TaskPerfCSharp.TenBindsSync_CSharp().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_ply() =
        for i in 1 .. manyIterations * 100 do
            tenBindSync_ply().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_async() =
        for i in 1 .. manyIterations * 100 do
            tenBindSync_async ()
            |> Async.RunSynchronously
            |> ignore

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_task() =
        for i in 1 .. manyIterations * 100 do
            tenBindSync_task().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_coldTask_bindTask() =
        for i in 1 .. manyIterations * 100 do
            (tenBindSync_coldTask_bindTask () ()).Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_coldTask_bindColdTask() =
        for i in 1 .. manyIterations * 100 do
            (tenBindSync_coldTask_bindColdTask () ()).Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_cancellableTask_bindTask() =
        for i in 1 .. manyIterations * 100 do
            (tenBindSync_cancellableTask_bindTask () CancellationToken.None)
                .Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_cancellableTask() =
        for i in 1 .. manyIterations * 100 do
            (tenBindSync_cancellableTask_bindCancellableTask () CancellationToken.None)
                .Wait()

    //AsyncBinds


    [<BenchmarkCategory("AsyncBinds"); Benchmark(Baseline = true)>]
    member _.AsyncBinds_CSharpTasks() =
        for i in 1..manyIterations do
            TaskPerfCSharp.TenBindsAsync_CSharp().Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_ply() =
        for i in 1..manyIterations do
            tenBindAsync_ply().Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_async() =
        for i in 1..manyIterations do
            tenBindAsync_async () |> Async.RunSynchronously

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_task() =
        for i in 1..manyIterations do
            tenBindAsync_task().Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_coldTask_bindTask() =
        for i in 1..manyIterations do
            (tenBindAsync_coldTask_bindTask () ()).Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_coldTask_bindColdTask() =
        for i in 1..manyIterations do
            (tenBindAsync_coldTask_bindColdTask () ()).Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_cancellableTask_bindTask() =
        for i in 1..manyIterations do
            (tenBindAsync_cancellableTask_bindTask () (CancellationToken.None))
                .Wait()


    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_cancellableTask_bindCancellableTask() =
        for i in 1..manyIterations do
            (tenBindAsync_cancellableTask_bindCancellableTask () (CancellationToken.None))
                .Wait()
