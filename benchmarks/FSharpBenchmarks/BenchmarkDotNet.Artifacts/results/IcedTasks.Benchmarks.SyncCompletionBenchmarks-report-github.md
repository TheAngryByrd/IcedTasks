```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 11 (10.0.26100.6899)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2


```
| Method                                                                  | Categories                                                                | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------------------------------ |-------------------------------------------------------------------------- |-------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| CSharp_TenBindsSync_TaskBuilder_BindTask                                | NonAsyncBinds,CSharp,TaskBuilder,BindTask                                 |    53.152 ns |   1.0463 ns |   1.9132 ns |   1.00 |    0.00 | 0.0505 |     792 B |        1.00 |
| CSharp_TenBindsSync_TaskBuilder_BindValueTask                           | NonAsyncBinds,CSharp,TaskBuilder,BindValueTask                            |    16.228 ns |   0.2477 ns |   0.2317 ns |   0.31 |    0.01 | 0.0046 |      72 B |        0.09 |
| CSharp_TenBindsSync_ValueTaskBuilder_BindTask                           | NonAsyncBinds,CSharp,ValueTaskBuilder,BindTask                            |    51.333 ns |   1.2348 ns |   3.5428 ns |   1.00 |    0.07 | 0.0459 |     720 B |        0.91 |
| CSharp_TenBindsSync_ValueTaskBuilder_BindValueTask                      | NonAsyncBinds,CSharp,ValueTaskBuilder,BindValueTask                       |     9.468 ns |   0.1127 ns |   0.1055 ns |   0.18 |    0.01 |      - |         - |        0.00 |
| FSharp_TenBindsSync_AsyncBuilder_BindAsync                              | NonAsyncBinds,FSharp,AsyncBuilder,BindAsync                               | 6,699.957 ns | 133.5842 ns | 226.8360 ns | 125.86 |    5.52 | 0.1563 |    2512 B |        3.17 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindAsync                     | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindAsync                     |   893.313 ns |  12.8257 ns |  18.7997 ns |  16.71 |    0.78 | 0.3535 |    5552 B |        7.01 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableTask           | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableTask           |   168.826 ns |   3.2921 ns |   5.1253 ns |   3.17 |    0.18 | 0.0605 |     952 B |        1.20 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindCancellableValueTask      | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableValueTask      |   151.251 ns |   2.9229 ns |   3.3660 ns |   2.82 |    0.16 | 0.0198 |     312 B |        0.39 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindTask                      | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindTask                      |   167.895 ns |   3.3430 ns |   3.9796 ns |   3.14 |    0.11 | 0.0605 |     952 B |        1.20 |
| Fsharp_TenBindSync_cancellableTaskBuilder_BindValueTask                 | NonAsyncBinds,FSharp,CancellableTaskBuilder,BindValueTask                 |   150.334 ns |   2.0106 ns |   1.8807 ns |   2.85 |    0.11 | 0.0198 |     312 B |        0.39 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindAsync                | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindAsync                |   942.606 ns |  16.7212 ns |  29.7219 ns |  17.75 |    0.93 | 0.3496 |    5488 B |        6.93 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableTask      | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableTask      |   184.312 ns |   3.1427 ns |   3.7411 ns |   3.45 |    0.15 | 0.0564 |     888 B |        1.12 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindCancellableValueTask | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableValueTask |   138.886 ns |   2.7517 ns |   3.1688 ns |   2.59 |    0.13 | 0.0156 |     248 B |        0.31 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindTask                 | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindTask                 |   184.975 ns |   3.5605 ns |   4.3727 ns |   3.46 |    0.17 | 0.0564 |     888 B |        1.12 |
| Fsharp_TenBindSync_cancellableValueTaskBuilder_BindValueTask            | NonAsyncBinds,FSharp,CancellableValueTaskBuilder,BindValueTask            |   139.134 ns |   2.0115 ns |   1.8816 ns |   2.63 |    0.09 | 0.0156 |     248 B |        0.31 |
| Fsharp_TenBindSync_plyTaskBuilder_BindTask                              | NonAsyncBinds,FSharp,PlyTaskBuilder,BindTask                              |    50.831 ns |   0.8921 ns |   1.5388 ns |   0.96 |    0.03 | 0.0505 |     792 B |        1.00 |
| Fsharp_TenBindSync_plyTaskBuilder_BindValueTask                         | NonAsyncBinds,FSharp,PlyTaskBuilder,BindValueTask                         |     9.317 ns |   0.1807 ns |   0.2285 ns |   0.17 |    0.01 | 0.0046 |      72 B |        0.09 |
| Fsharp_TenBindSync_plyValueTaskBuilder_BindTask                         | NonAsyncBinds,FSharp,PlyValueTaskBuilder,BindTask                         |    50.255 ns |   0.9952 ns |   2.1422 ns |   0.95 |    0.04 | 0.0459 |     720 B |        0.91 |
| Fsharp_TenBindSync_plyValueTaskBuilder_BindValueTask                    | NonAsyncBinds,FSharp,PlyValueTaskBuilder,BindValueTask                    |     7.163 ns |   0.0560 ns |   0.0524 ns |   0.14 |    0.00 |      - |         - |        0.00 |
| Fsharp_TenBindSync_TaskBuilder_BindAsync                                | NonAsyncBinds,FSharp,TaskBuilder,BindAsync                                |   804.408 ns |  14.2860 ns |  12.6641 ns |  15.31 |    0.48 | 0.3359 |    5272 B |        6.66 |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindAsync                         | NonAsyncBinds,FSharp,TaskBuilder,BindAsync                                |   805.176 ns |  12.7655 ns |  11.9408 ns |  15.23 |    0.46 | 0.3359 |    5272 B |        6.66 |
| Fsharp_TenBindSync_TaskBuilder_BindTask                                 | NonAsyncBinds,FSharp,TaskBuilder,BindTask                                 |    69.114 ns |   1.0094 ns |   0.9442 ns |   1.31 |    0.05 | 0.0504 |     792 B |        1.00 |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindTask                          | NonAsyncBinds,FSharp,TaskBuilder,BindTask                                 |    42.045 ns |   0.8268 ns |   0.9521 ns |   0.78 |    0.04 | 0.0505 |     792 B |        1.00 |
| Fsharp_TenBindSync_TaskBuilder_BindValueTask                            | NonAsyncBinds,FSharp,TaskBuilder,BindValueTask                            |    21.980 ns |   0.4002 ns |   0.3744 ns |   0.42 |    0.02 | 0.0046 |      72 B |        0.09 |
| Fsharp_TenBindSync_TaskBuilderRuntime_BindValueTask                     | NonAsyncBinds,FSharp,TaskBuilder,BindValueTask                            |     4.352 ns |   0.0798 ns |   0.1092 ns |   0.08 |    0.00 | 0.0046 |      72 B |        0.09 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindAsync                           | NonAsyncBinds,FSharp,ValueTaskBuilder,BindAsync                           |   809.772 ns |   7.3723 ns |   9.0538 ns |  15.14 |    0.65 | 0.3359 |    5272 B |        6.66 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindTask                            | NonAsyncBinds,FSharp,ValueTaskBuilder,BindTask                            |    63.019 ns |   1.2193 ns |   1.5420 ns |   1.18 |    0.05 | 0.0459 |     720 B |        0.91 |
| Fsharp_TenBindSync_ValueTaskBuilder_BindValueTask                       | NonAsyncBinds,FSharp,ValueTaskBuilder,BindValueTask                       |    18.389 ns |   0.1492 ns |   0.1396 ns |   0.35 |    0.01 |      - |         - |        0.00 |
