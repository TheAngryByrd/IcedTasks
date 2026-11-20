open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open IcedTasks.Benchmarks

let runtimeAsyncSwitch = "--runtime-async"

let buildJob id runtimeAsync =
    Job.Default
        .WithId(id)
        .WithArguments([| MsBuildArgument($"/p:RuntimeAsync=%b{runtimeAsync}") :> Argument |])

[<EntryPoint>]
let main argv =
    let switcher =
        BenchmarkSwitcher.FromAssembly(typeof<SyncCompletionBenchmarks>.Assembly)

    if Array.contains runtimeAsyncSwitch argv then
        let config =
            DefaultConfig.Instance
                .AddJob(buildJob "StateMachine" false)
                .AddJob(buildJob "RuntimeAsync" true)

        switcher.Run(Array.filter ((<>) runtimeAsyncSwitch) argv, config)
        |> ignore
    else
        switcher.Run(argv)
        |> ignore

    0
