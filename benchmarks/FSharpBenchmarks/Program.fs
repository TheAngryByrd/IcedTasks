open System
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Environments
open IcedTasks.Benchmarks


[<EntryPoint>]
let main argv =

    let summary =
        BenchmarkSwitcher.FromAssembly(typeof<WhileLoopBenchmarks>.Assembly).Run(argv)

    0
