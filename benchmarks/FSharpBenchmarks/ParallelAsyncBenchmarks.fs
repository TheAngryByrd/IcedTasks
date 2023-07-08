namespace IcedTasks.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open IcedTasks


[<AutoOpen>]
module Helpers =

    [<Literal>]
    let fewerIterations = 100

    let asyncYieldLong () = Async.Sleep(10)

[<AutoOpen>]
module AsyncExns =

    type AsyncBuilder with

        member inline _.MergeSources(t1: Async<'T>, t2: Async<'T1>) =
            // async {
            //     let! t1r = t1
            //     let! t2r = t2
            //     return t1r,t2r
            // }
            async.Bind(t1, (fun t1r -> async.Bind(t2, (fun t2r -> async.Return(t1r, t2r)))))


[<MemoryDiagnoser>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type ParallelAsyncBenchmarks() =


    [<BenchmarkCategory("AsyncBindsLong");
      Benchmark(Baseline = true, OperationsPerInvoke = fewerIterations)>]
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


    [<BenchmarkCategory("AsyncBindsLong"); Benchmark(OperationsPerInvoke = fewerIterations)>]
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

    [<BenchmarkCategory("AsyncBindsLong"); Benchmark(OperationsPerInvoke = fewerIterations)>]
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

    [<BenchmarkCategory("AsyncBindsLong"); Benchmark(OperationsPerInvoke = fewerIterations)>]
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
