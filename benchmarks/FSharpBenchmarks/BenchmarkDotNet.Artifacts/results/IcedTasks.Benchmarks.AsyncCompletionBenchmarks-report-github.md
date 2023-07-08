``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1848)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.8 (7.0.823.31807), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 7.0.8 (7.0.823.31807), X64 RyuJIT AVX2


```
|                                                               Method |                                                             Categories |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------------------------------------- |----------------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
|                                     CSharp_TenBindsAsync_TaskBuilder |                                          AsyncBinds,CSharp,TaskBuilder |  3.650 μs | 0.0702 μs | 0.0690 μs |  1.00 |    0.00 |      - |     112 B |        1.00 |
|                                CSharp_TenBindsAsync_ValueTaskBuilder |                                     AsyncBinds,CSharp,ValueTaskBuilder |  3.533 μs | 0.0527 μs | 0.0467 μs |  0.97 |    0.02 | 0.0039 |     120 B |        1.07 |
|                                    FSharp_TenBindsAsync_AsyncBuilder |                                         AsyncBinds,FSharp,AsyncBuilder | 70.017 μs | 1.3369 μs | 1.7383 μs | 19.18 |    0.61 | 0.4286 |    8224 B |       73.43 |
|                          FSharp_TenBindsAsync_CancellableTaskBuilder |                               AsyncBinds,FSharp,CancellableTaskBuilder |  3.607 μs | 0.0167 μs | 0.0130 μs |  0.99 |    0.02 | 0.0117 |     200 B |        1.79 |
|      FSharp_TenBindsAsync_CancellableTaskBuilder_BindCancellableTask |      AsyncBinds,FSharp,CancellableTaskBuilder,BindCancellableValueTask |  3.725 μs | 0.0737 μs | 0.1081 μs |  1.02 |    0.04 | 0.0117 |     200 B |        1.79 |
|                     FSharp_TenBindsAsync_CancellableValueTaskBuilder |                          AsyncBinds,FSharp,CancellableValueTaskBuilder |  3.549 μs | 0.0698 μs | 0.1378 μs |  0.97 |    0.04 | 0.0117 |     216 B |        1.93 |
| FSharp_TenBindsAsync_CancellableValueTaskBuilder_BindCancellableTask | AsyncBinds,FSharp,CancellableValueTaskBuilder,BindCancellableValueTask |  3.684 μs | 0.0689 μs | 0.0575 μs |  1.01 |    0.02 | 0.0117 |     216 B |        1.93 |
|                                  FSharp_TenBindsAsync_PlyTaskBuilder |                                       AsyncBinds,FSharp,PlyTaskBuilder |  4.137 μs | 0.0235 μs | 0.0220 μs |  1.13 |    0.03 | 0.0430 |     672 B |        6.00 |
|                             FSharp_TenBindsAsync_PlyValueTaskBuilder |                                  AsyncBinds,FSharp,PlyValueTaskBuilder |  3.869 μs | 0.0393 μs | 0.0329 μs |  1.06 |    0.02 | 0.0391 |     672 B |        6.00 |
|                                     FSharp_TenBindsAsync_TaskBuilder |                                          AsyncBinds,FSharp,TaskBuilder |  3.574 μs | 0.0243 μs | 0.0227 μs |  0.98 |    0.02 | 0.0078 |     128 B |        1.14 |
|                                FSharp_TenBindsAsync_ValueTaskBuilder |                                     AsyncBinds,FSharp,ValueTaskBuilder |  3.510 μs | 0.0669 μs | 0.0626 μs |  0.96 |    0.03 | 0.0078 |     136 B |        1.21 |
