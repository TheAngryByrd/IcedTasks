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
#if NET11_0_OR_GREATER
        let config =
            DefaultConfig.Instance
                .AddJob(buildJob "StateMachine" false)
                .AddJob(buildJob "RuntimeAsync" true)

        switcher.Run(Array.filter ((<>) runtimeAsyncSwitch) argv, config)
        |> ignore

        0
#else
        eprintfn "%s needs the net11.0 build. Run it with -f net11.0." runtimeAsyncSwitch
        1
#endif
    else
        switcher.Run(argv)
        |> ignore

        0
