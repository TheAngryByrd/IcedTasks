open System
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Environments
open IcedTasks.Benchmarks

[<EntryPoint>]
let main argv =
    let cfg = DefaultConfig.Instance

    BenchmarkRunner.Run<AsyncBenchmarks>(cfg)
    |> ignore

    0
