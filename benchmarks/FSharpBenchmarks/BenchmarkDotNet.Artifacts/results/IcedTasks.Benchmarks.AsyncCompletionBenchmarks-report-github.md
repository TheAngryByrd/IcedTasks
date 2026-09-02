```

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26100.7623/24H2/2024Update/HudsonValley)
12th Gen Intel Core i9-12900F 2.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3 DEBUG
  ShortRun : .NET 9.0.13 (9.0.13, 9.0.1326.6317), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                               | Categories                                                             | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------------------- |----------------------------------------------------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CSharp_TenBindsAsync_TaskBuilder                                     | AsyncBinds,CSharp,TaskBuilder                                          |  3.237 μs |  0.6486 μs | 0.0356 μs |  1.00 |    0.01 |      - |      96 B |        1.00 |
| CSharp_TenBindsAsync_ValueTaskBuilder                                | AsyncBinds,CSharp,ValueTaskBuilder                                     |  5.808 μs |  9.6874 μs | 0.5310 μs |  1.79 |    0.14 |      - |     133 B |        1.39 |
| FSharp_TenBindsAsync_AsyncBuilder                                    | AsyncBinds,FSharp,AsyncBuilder                                         | 66.339 μs | 19.3886 μs | 1.0628 μs | 20.50 |    0.35 | 0.5000 |    8224 B |       85.67 |
| FSharp_TenBindsAsync_CancellableTaskBuilder                          | AsyncBinds,FSharp,CancellableTaskBuilder                               |  4.008 μs |  0.5458 μs | 0.0299 μs |  1.24 |    0.01 | 0.0469 |     808 B |        8.42 |
| FSharp_TenBindsAsync_CancellableTaskBuilder_BindCancellableTask      | AsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableValueTask      |  4.022 μs |  0.5220 μs | 0.0286 μs |  1.24 |    0.01 | 0.0469 |     808 B |        8.42 |
| FSharp_TenBindsAsync_CancellableValueTaskBuilder                     | AsyncBinds,FSharp,CancellableValueTaskBuilder                          |  3.829 μs |  0.2200 μs | 0.0121 μs |  1.18 |    0.01 | 0.0508 |     824 B |        8.58 |
| FSharp_TenBindsAsync_CancellableValueTaskBuilder_BindCancellableTask | AsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableValueTask |  3.882 μs |  1.3909 μs | 0.0762 μs |  1.20 |    0.02 | 0.0469 |     824 B |        8.58 |
| FSharp_TenBindsAsync_PlyTaskBuilder                                  | AsyncBinds,FSharp,PlyTaskBuilder                                       |  4.190 μs |  3.2852 μs | 0.1801 μs |  1.29 |    0.05 | 0.0391 |     657 B |        6.84 |
| FSharp_TenBindsAsync_PlyValueTaskBuilder                             | AsyncBinds,FSharp,PlyValueTaskBuilder                                  |  3.843 μs |  0.0229 μs | 0.0013 μs |  1.19 |    0.01 | 0.0391 |     656 B |        6.83 |
| FSharp_TenBindsAsync_TaskBuilder                                     | AsyncBinds,FSharp,TaskBuilder                                          |  3.425 μs |  2.9334 μs | 0.1608 μs |  1.06 |    0.04 |      - |     112 B |        1.17 |
| FSharp_TenBindsAsync_TaskBuilderRuntime                              | AsyncBinds,FSharp,TaskBuilderRuntime                                   |        NA |         NA |        NA |     ? |       ? |     NA |        NA |           ? |
| FSharp_TenBindsAsync_ValueTaskBuilder                                | AsyncBinds,FSharp,ValueTaskBuilder                                     |  7.373 μs | 50.8676 μs | 2.7882 μs |  2.28 |    0.75 | 0.0469 |     745 B |        7.76 |

Benchmarks with issues:
  AsyncCompletionBenchmarks.FSharp_TenBindsAsync_TaskBuilderRuntime: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
