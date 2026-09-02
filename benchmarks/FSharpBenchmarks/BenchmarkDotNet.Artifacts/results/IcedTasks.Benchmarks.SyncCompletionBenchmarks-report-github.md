```

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26100.7623/24H2/2024Update/HudsonValley)
12th Gen Intel Core i9-12900F 2.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3 DEBUG
  ShortRun : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                                  | Categories                                                                | Mean         | Error          | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------------------------------ |-------------------------------------------------------------------------- |-------------:|---------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| CSharp_TenBindsSync_TaskBuilder_BindTask                                | NonAsyncBinds,CSharp,TaskBuilder,BindTask                                 |    58.554 ns |    117.4654 ns |   6.4387 ns |   1.01 |    0.13 | 0.0505 |     792 B |        1.00 |
| CSharp_TenBindsSync_TaskBuilder_BindValueTask                           | NonAsyncBinds,CSharp,TaskBuilder,BindValueTask                            |    15.124 ns |      3.9592 ns |   0.2170 ns |   0.26 |    0.02 | 0.0046 |      72 B |        0.09 |
| CSharp_TenBindsSync_ValueTaskBuilder_BindTask                           | NonAsyncBinds,CSharp,ValueTaskBuilder,BindTask                            |    50.681 ns |     12.6474 ns |   0.6932 ns |   0.87 |    0.08 | 0.0459 |     720 B |        0.91 |
| CSharp_TenBindsSync_ValueTaskBuilder_BindValueTask                      | NonAsyncBinds,CSharp,ValueTaskBuilder,BindValueTask                       |    10.276 ns |      1.5095 ns |   0.0827 ns |   0.18 |    0.02 |      - |         - |        0.00 |
| FSharp_TenBindsSync_AsyncBuilder_BindAsync                              | NonAsyncBinds,FSharp,AsyncBuilder,BindAsync                               | 8,023.515 ns | 13,598.6038 ns | 745.3854 ns | 138.08 |   16.69 | 0.1563 |    2512 B |        3.17 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindAsync                     | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindAsync                     | 1,077.877 ns |    214.9256 ns |  11.7808 ns |  18.55 |    1.68 | 0.3516 |    5528 B |        6.98 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableTask           | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableTask           |   189.676 ns |     35.0467 ns |   1.9210 ns |   3.26 |    0.29 | 0.0605 |     952 B |        1.20 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableValueTask      | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableValueTask      |   152.270 ns |     12.5215 ns |   0.6863 ns |   2.62 |    0.24 | 0.0198 |     312 B |        0.39 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindTask                      | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindTask                      |   195.979 ns |     52.8511 ns |   2.8969 ns |   3.37 |    0.31 | 0.0605 |     952 B |        1.20 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindValueTask                 | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindValueTask                 |   157.032 ns |     15.1115 ns |   0.8283 ns |   2.70 |    0.24 | 0.0198 |     312 B |        0.39 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindAsync                | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindAsync                |   976.370 ns |    426.2325 ns |  23.3632 ns |  16.80 |    1.55 | 0.3477 |    5464 B |        6.90 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableTask      | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableTask      |   190.752 ns |     91.9561 ns |   5.0404 ns |   3.28 |    0.30 | 0.0564 |     888 B |        1.12 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableValueTask | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableValueTask |   146.246 ns |     42.3326 ns |   2.3204 ns |   2.52 |    0.23 | 0.0156 |     248 B |        0.31 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindTask                 | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindTask                 |   194.265 ns |     90.7918 ns |   4.9766 ns |   3.34 |    0.31 | 0.0564 |     888 B |        1.12 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindValueTask            | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindValueTask            |   147.972 ns |     92.5910 ns |   5.0752 ns |   2.55 |    0.24 | 0.0156 |     248 B |        0.31 |
| Fsharp_TenBindSync_plyTaskBuilder_BindTask                              | NonAsyncBinds,FSharp,PlyTaskBuilder,BindTask                              |    69.264 ns |     59.1529 ns |   3.2424 ns |   1.19 |    0.12 | 0.0505 |     792 B |        1.00 |
| Fsharp_TenBindSync_plyTaskBuilder_BindValueTask                         | NonAsyncBinds,FSharp,PlyTaskBuilder,BindValueTask                         |    11.811 ns |      2.8312 ns |   0.1552 ns |   0.20 |    0.02 | 0.0046 |      72 B |        0.09 |
| Fsharp_TenBindSync_plyValueTaskBuilder_BindTask                         | NonAsyncBinds,FSharp,PlyValueTaskBuilder,BindTask                         |    59.995 ns |     64.4577 ns |   3.5331 ns |   1.03 |    0.11 | 0.0459 |     720 B |        0.91 |
| Fsharp_TenBindSync_plyValueTaskBuilder_BindValueTask                    | NonAsyncBinds,FSharp,PlyValueTaskBuilder,BindValueTask                    |     6.608 ns |      0.9976 ns |   0.0547 ns |   0.11 |    0.01 |      - |         - |        0.00 |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindAsync                         | NonAsyncBinds,FSharp,TaskBuilderRuntime,BindAsync                         |           NA |             NA |          NA |      ? |       ? |     NA |        NA |           ? |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindTask                          | NonAsyncBinds,FSharp,TaskBuilderRuntime,BindTask                          |           NA |             NA |          NA |      ? |       ? |     NA |        NA |           ? |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindValueTask                     | NonAsyncBinds,FSharp,TaskBuilderRuntime,BindValueTask                     |           NA |             NA |          NA |      ? |       ? |     NA |        NA |           ? |
| Fsharp_TenBindSync_TaskBuilder_BindAsync                                | NonAsyncBinds,FSharp,TaskBuilder,BindAsync                                |   845.211 ns |    479.8705 ns |  26.3033 ns |  14.55 |    1.37 | 0.3359 |    5272 B |        6.66 |
| Fsharp_TenBindSync_TaskBuilder_BindTask                                 | NonAsyncBinds,FSharp,TaskBuilder,BindTask                                 |    70.066 ns |     31.9889 ns |   1.7534 ns |   1.21 |    0.11 | 0.0504 |     792 B |        1.00 |
| Fsharp_TenBindSync_TaskBuilder_BindValueTask                            | NonAsyncBinds,FSharp,TaskBuilder,BindValueTask                            |    23.122 ns |      5.1862 ns |   0.2843 ns |   0.40 |    0.04 | 0.0046 |      72 B |        0.09 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindAsync                           | NonAsyncBinds,FSharp,ValueTaskBuilder,BindAsync                           |   834.548 ns |    290.9838 ns |  15.9498 ns |  14.36 |    1.31 | 0.3359 |    5272 B |        6.66 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindTask                            | NonAsyncBinds,FSharp,ValueTaskBuilder,BindTask                            |    62.209 ns |     37.1952 ns |   2.0388 ns |   1.07 |    0.10 | 0.0459 |     720 B |        0.91 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindValueTask                       | NonAsyncBinds,FSharp,ValueTaskBuilder,BindValueTask                       |    19.224 ns |      0.5253 ns |   0.0288 ns |   0.33 |    0.03 |      - |         - |        0.00 |

Benchmarks with issues:
  SyncCompletionBenchmarks.Fsharp_TenBindSync_TaskBuilderRuntime_BindAsync: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SyncCompletionBenchmarks.Fsharp_TenBindSync_TaskBuilderRuntime_BindTask: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SyncCompletionBenchmarks.Fsharp_TenBindSync_TaskBuilderRuntime_BindValueTask: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
