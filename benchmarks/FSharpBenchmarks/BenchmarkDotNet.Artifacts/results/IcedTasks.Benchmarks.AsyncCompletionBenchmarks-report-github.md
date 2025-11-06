```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 11 (10.0.26100.6899)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2


```
| Method                                                               | Categories                                                             | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------------------- |----------------------------------------------------------------------- |-------------:|-------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| CSharp_TenBindsAsync_TaskBuilder                                     | AsyncBinds,CSharp,TaskBuilder                                          |  2,780.95 ns |    64.686 ns |   188.691 ns |  1.00 |    0.00 | 0.1250 |    1976 B |        1.00 |
| CSharp_TenBindsAsync_ValueTaskBuilder                                | AsyncBinds,CSharp,ValueTaskBuilder                                     |  2,961.75 ns |    65.470 ns |   190.979 ns |  1.07 |    0.09 | 0.1250 |    1976 B |        1.00 |
| FSharp_TenBindsAsync_AsyncBuilder                                    | AsyncBinds,FSharp,AsyncBuilder                                         | 66,611.01 ns | 1,330.629 ns | 2,563.671 ns | 23.38 |    1.50 | 0.4444 |    8224 B |        4.16 |
| FSharp_TenBindsAsync_CancellableTaskBuilder                          | AsyncBinds,FSharp,CancellableTaskBuilder                               |  2,191.38 ns |    43.374 ns |    56.398 ns |  0.78 |    0.04 | 0.0508 |     808 B |        0.41 |
| FSharp_TenBindsAsync_CancellableTaskBuilder_BindCancellableTask      | AsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableValueTask      |  2,382.94 ns |    46.612 ns |    38.923 ns |  0.83 |    0.03 | 0.0508 |     808 B |        0.41 |
| FSharp_TenBindsAsync_CancellableValueTaskBuilder                     | AsyncBinds,FSharp,CancellableValueTaskBuilder                          |  2,257.29 ns |    44.978 ns |   123.881 ns |  0.81 |    0.07 | 0.0508 |     824 B |        0.42 |
| FSharp_TenBindsAsync_CancellableValueTaskBuilder_BindCancellableTask | AsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableValueTask |  2,176.36 ns |    40.825 ns |    92.980 ns |  0.76 |    0.05 | 0.0508 |     824 B |        0.42 |
| FSharp_TenBindsAsync_PlyTaskBuilder                                  | AsyncBinds,FSharp,PlyTaskBuilder                                       |  2,178.15 ns |    43.520 ns |   121.315 ns |  0.78 |    0.06 | 0.0391 |     656 B |        0.33 |
| FSharp_TenBindsAsync_PlyValueTaskBuilder                             | AsyncBinds,FSharp,PlyValueTaskBuilder                                  |  2,259.70 ns |    41.684 ns |    64.898 ns |  0.80 |    0.04 | 0.0391 |     656 B |        0.33 |
| FSharp_TenBindsAsync_TaskBuilder                                     | AsyncBinds,FSharp,TaskBuilder                                          |  2,035.52 ns |    40.605 ns |   100.366 ns |  0.72 |    0.06 | 0.0039 |     112 B |        0.06 |
| FSharp_TenBindsAsync_TaskBuilderRuntime                              | AsyncBinds,FSharp,TaskBuilder                                          |     55.92 ns |     1.094 ns |     1.703 ns |  0.02 |    0.00 | 0.0198 |     312 B |        0.16 |
| FSharp_TenBindsAsync_ValueTaskBuilder                                | AsyncBinds,FSharp,ValueTaskBuilder                                     |  2,153.19 ns |    15.575 ns |    12.160 ns |  0.75 |    0.03 | 0.0469 |     744 B |        0.38 |
