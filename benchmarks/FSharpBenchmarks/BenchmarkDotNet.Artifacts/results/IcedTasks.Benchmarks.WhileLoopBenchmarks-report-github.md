``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1848)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.400-preview.23226.4
  [Host]     : .NET 7.0.8 (7.0.823.31807), X64 RyuJIT AVX2 DEBUG
  Job-YLXCDK : .NET 7.0.8 (7.0.823.31807), X64 RyuJIT AVX2

Runtime=.NET 7.0  Server=True  Toolchain=net7.0  

```
|                                  Method | Length |         Mean |      Error |     StdDev |    Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------- |------- |-------------:|-----------:|-----------:|---------:|--------:|----------:|------------:|
|                  CSharp_Whileloop_Tasks |  10000 |     2.160 μs |  0.0077 μs |  0.0124 μs |     1.00 |    0.00 |      72 B |        1.00 |
|            CSharp_Whileloop_Tasks_async |  10000 | 3,115.359 μs | 21.9573 μs | 19.4645 μs | 1,440.76 |   12.17 |     189 B |        2.62 |
|                         CancellableTask |  10000 |     3.611 μs |  0.0198 μs |  0.0185 μs |     1.67 |    0.01 |     152 B |        2.11 |
|                        CancellableTask2 |  10000 |           NA |         NA |         NA |        ? |       ? |         - |           ? |
|                        CancellableTask3 |  10000 |           NA |         NA |         NA |        ? |       ? |         - |           ? |
|        CancellableTask_syncDoBlockTrick |  10000 |     2.160 μs |  0.0117 μs |  0.0110 μs |     1.00 |    0.01 |     128 B |        1.78 |
|                   CancellableTask_async |  10000 | 3,077.223 μs | 27.2582 μs | 25.4974 μs | 1,422.40 |   15.53 |     307 B |        4.26 |
|  CancellableTask_asyncCancellableLambda |  10000 | 2,964.279 μs | 55.2893 μs | 49.0125 μs | 1,370.82 |   19.18 |     309 B |        4.29 |
| CancellableTask_asyncCancellableLambda2 |  10000 | 3,045.664 μs | 56.3261 μs | 52.6874 μs | 1,407.80 |   25.38 |     307 B |        4.26 |
|      CancellableTask_asyncCancellableCE |  10000 | 4,491.386 μs | 69.1838 μs | 64.7145 μs | 2,076.06 |   31.82 | 2080315 B |   28,893.26 |
|     CancellableTask_asyncCancellableCE2 |  10000 | 4,474.445 μs | 66.4772 μs | 62.1828 μs | 2,068.26 |   32.57 | 1280384 B |   17,783.11 |
|     CancellableTask_asyncCancellableCE3 |  10000 | 4,431.639 μs | 48.7732 μs | 45.6224 μs | 2,048.46 |   24.98 | 1280386 B |   17,783.14 |
|     CancellableTask_asyncCancellableCE4 |  10000 | 4,362.897 μs | 79.9664 μs | 70.8881 μs | 2,017.69 |   34.19 | 1520315 B |   21,115.49 |
|     CancellableTask_asyncCancellableCE5 |  10000 |           NA |         NA |         NA |        ? |       ? |         - |           ? |

Benchmarks with issues:
  WhileLoopBenchmarks.CancellableTask2: Job-YLXCDK(Runtime=.NET 7.0, Server=True, Toolchain=net7.0) [Length=10000]
  WhileLoopBenchmarks.CancellableTask3: Job-YLXCDK(Runtime=.NET 7.0, Server=True, Toolchain=net7.0) [Length=10000]
  WhileLoopBenchmarks.CancellableTask_asyncCancellableCE5: Job-YLXCDK(Runtime=.NET 7.0, Server=True, Toolchain=net7.0) [Length=10000]
