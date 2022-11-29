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
module Helpers =
    let bufferSize = 128
    let manyIterations = 1000

    let fewerIterations =
        manyIterations
        / 10

    let syncTask () = Task.FromResult 100
    let syncCtTask (ct: CancellationToken) = Task.FromResult 100
    let syncTask_async () = async.Return 100
    let syncTask_async2 () = Task.FromResult 100
    let asyncYield () = Async.Sleep(0)
    let asyncYieldLong () = Async.Sleep(10)
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

    let tenBindSync_async () = async {
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

    let tenBindSync_task () = task {
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


    let tenBindSync_coldTask_bindTask () = coldTask {
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

    let tenBindSync_coldTask_bindColdTask () = coldTask {
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


    let tenBindSync_cancellableTask_bindTask () = cancellableTask {
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


    let tenBindSync_cancellableTask_bindCancellableTask () = cancellableTask {
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

    let tenBindAsync_async () = async {
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

    let tenBindAsync_task () = task {
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


    let tenBindAsync_coldTask_bindTask () = coldTask {
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


    let tenBindAsync_coldTask_bindColdTask () = coldTask {
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


    let tenBindAsync_cancellableTask_bindTask () = cancellableTask {
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


    let tenBindAsync_cancellableTask_bindColdTask () = cancellableTask {
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

    let tenBindAsync_cancellableTask_bindCancellableTask () = cancellableTask {
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

    let getTempFileName () =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"))

    // ManyWriteFile

    [<BenchmarkCategory("ManyWriteFile"); Benchmark(Baseline = true)>]
    member _.ManyWriteFile_CSharpTasks() =
        TaskPerfCSharp.ManyWriteFileAsync(manyIterations).Wait()


    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_ply() =
        let path = getTempFileName ()

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
        let path = getTempFileName ()

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
        let path = getTempFileName ()

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
        let path = getTempFileName ()

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
        let path = getTempFileName ()

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
        let path = getTempFileName ()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            let! ct = CancellableTask.getCancellationToken ()

            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

        File.Delete(path)

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask_withCancellation2() =
        let path = getTempFileName ()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

    [<BenchmarkCategory("ManyWriteFile"); Benchmark>]
    member _.ManyWriteFile_cancellableTask_withCancellation3() =
        let path = getTempFileName ()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                let! ct = CancellableTask.getCancellationToken ()
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
        |> fun t -> t(CancellationToken.None).Wait()

        File.Delete(path)

    // NonAsyncBinds


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark(Baseline = true)>]
    member _.NonAsyncBinds_CSharpTasks() =
        for i in
            1 .. manyIterations
                 * 100 do
            TaskPerfCSharp.TenBindsSync_CSharp().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_ply() =
        for i in
            1 .. manyIterations
                 * 100 do
            tenBindSync_ply().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_async() =
        for i in
            1 .. manyIterations
                 * 100 do
            tenBindSync_async ()
            |> Async.RunSynchronously
            |> ignore

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_task() =
        for i in
            1 .. manyIterations
                 * 100 do
            tenBindSync_task().Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_coldTask_bindTask() =
        for i in
            1 .. manyIterations
                 * 100 do
            (tenBindSync_coldTask_bindTask () ()).Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_coldTask_bindColdTask() =
        for i in
            1 .. manyIterations
                 * 100 do
            (tenBindSync_coldTask_bindColdTask () ()).Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_cancellableTask_bindTask() =
        for i in
            1 .. manyIterations
                 * 100 do
            (tenBindSync_cancellableTask_bindTask () CancellationToken.None).Wait()

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_cancellableTask() =
        for i in
            1 .. manyIterations
                 * 100 do
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
            tenBindAsync_async ()
            |> Async.RunSynchronously

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
            (tenBindAsync_cancellableTask_bindTask () (CancellationToken.None)).Wait()


    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_cancellableTask_bindColdTask() =
        for i in 1..manyIterations do
            (tenBindAsync_cancellableTask_bindColdTask () (CancellationToken.None)).Wait()

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_cancellableTask_bindCancellableTask() =
        for i in 1..manyIterations do
            (tenBindAsync_cancellableTask_bindCancellableTask () (CancellationToken.None))
                .Wait()


module AsyncExns =

    type AsyncBuilder with

        member inline _.MergeSources(t1: Async<'T>, t2: Async<'T1>) =
            // async {
            //     let! t1r = t1
            //     let! t2r = t2
            //     return t1r,t2r
            // }
            async.Bind(t1, (fun t1r -> async.Bind(t2, (fun t2r -> async.Return(t1r, t2r)))))

open AsyncExns

[<MemoryDiagnoser>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type ParallelAsyncBenchmarks() =


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.AsyncBuilder_sync() =

        for i in 1..manyIterations do
            Helpers.tenBindSync_async ()
            |> Async.RunSynchronously
            |> ignore


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.AsyncBuilder_sync_applicative_overhead() =

        for i in 1..manyIterations do
            async {
                let! res1 = syncTask_async ()
                and! res2 = syncTask_async ()
                and! res3 = syncTask_async ()
                and! res4 = syncTask_async ()
                and! res5 = syncTask_async ()
                and! res6 = syncTask_async ()
                and! res7 = syncTask_async ()
                and! res8 = syncTask_async ()
                and! res9 = syncTask_async ()
                and! res10 = syncTask_async ()

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
            |> Async.RunSynchronously
            |> ignore


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartChild_sync() =

        for i in 1..manyIterations do
            parallelAsyncUsingStartChild {
                let! res1 = syncTask_async ()
                and! res2 = syncTask_async ()
                and! res3 = syncTask_async ()
                and! res4 = syncTask_async ()
                and! res5 = syncTask_async ()
                and! res6 = syncTask_async ()
                and! res7 = syncTask_async ()
                and! res8 = syncTask_async ()
                and! res9 = syncTask_async ()
                and! res10 = syncTask_async ()

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
            |> Async.RunSynchronously
            |> ignore


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartImmediateAsTask_sync() =
        for i in 1..manyIterations do
            parallelAsyncUsingStartImmediateAsTask {
                let! res1 = syncTask_async ()
                and! res2 = syncTask_async ()
                and! res3 = syncTask_async ()
                and! res4 = syncTask_async ()
                and! res5 = syncTask_async ()
                and! res6 = syncTask_async ()
                and! res7 = syncTask_async ()
                and! res8 = syncTask_async ()
                and! res9 = syncTask_async ()
                and! res10 = syncTask_async ()

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
            |> Async.RunSynchronously
            |> ignore

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBuilder_async() =

        for i in 1..manyIterations do
            Helpers.tenBindAsync_async ()
            |> Async.RunSynchronously


    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBuilder_async_applicative_overhead() =

        for i in 1..manyIterations do
            async {
                let! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                return ()
            }
            |> Async.RunSynchronously


    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartChild_async() =

        for i in 1..manyIterations do
            parallelAsyncUsingStartChild {
                let! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                return ()
            }
            |> Async.RunSynchronously

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartImmediateAsTask_async() =

        for i in 1..manyIterations do
            parallelAsyncUsingStartImmediateAsTask {

                let! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                and! _ = asyncYield ()
                return ()
            }
            |> Async.RunSynchronously


    [<BenchmarkCategory("AsyncBindsLong"); Benchmark>]
    member _.AsyncBuilder_async_long() =

        for i in 1..fewerIterations do
            async {
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                let! _ = asyncYieldLong ()
                return ()
            }
            |> Async.RunSynchronously


    [<BenchmarkCategory("AsyncBindsLong"); Benchmark>]
    member _.AsyncBuilder_async_long_applicative_overhead() =

        for i in 1..fewerIterations do
            async {
                let! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                return ()
            }
            |> Async.RunSynchronously

    [<BenchmarkCategory("AsyncBindsLong"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartChild_async_long() =

        for i in 1..fewerIterations do
            parallelAsyncUsingStartChild {
                let! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                return ()
            }
            |> Async.RunSynchronously

    [<BenchmarkCategory("AsyncBindsLong"); Benchmark>]
    member _.ParallelAsyncBuilderUsingStartImmediateAsTask_async_long() =

        for i in 1..fewerIterations do
            parallelAsyncUsingStartImmediateAsTask {

                let! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                and! _ = asyncYieldLong ()
                return ()
            }
            |> Async.RunSynchronously
