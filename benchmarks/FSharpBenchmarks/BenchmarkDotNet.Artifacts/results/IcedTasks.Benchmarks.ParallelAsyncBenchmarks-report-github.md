``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                                   Method |     Categories |          Mean |       Error |      StdDev |        Median |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------------------------------------------- |--------------- |--------------:|------------:|------------:|--------------:|----------:|---------:|---------:|----------:|
|                                        AsyncBuilder_sync |  NonAsyncBinds |      8.249 ms |   0.1610 ms |   0.2410 ms |      8.248 ms |  296.8750 |        - |        - |  2,484 KB |
|                   AsyncBuilder_sync_applicative_overhead |  NonAsyncBinds |      8.886 ms |   0.1761 ms |   0.1729 ms |      8.879 ms |  515.6250 |        - |        - |  4,320 KB |
|                 ParallelAsyncBuilderUsingStartChild_sync |  NonAsyncBinds |     50.430 ms |   2.8994 ms |   8.5490 ms |     45.826 ms | 4222.2222 | 111.1111 |        - | 34,313 KB |
|       ParallelAsyncBuilderUsingStartImmediateAsTask_sync |  NonAsyncBinds |     15.428 ms |   0.1229 ms |   0.1150 ms |     15.383 ms | 2000.0000 |        - |        - | 16,484 KB |
|                                                          |                |               |             |             |               |           |          |          |           |
|                                       AsyncBuilder_async |     AsyncBinds |     74.702 ms |   1.1459 ms |   1.0158 ms |     74.756 ms | 1000.0000 |        - |        - |  8,377 KB |
|                  AsyncBuilder_async_applicative_overhead |     AsyncBinds |     76.906 ms |   1.1282 ms |   1.2072 ms |     76.635 ms | 1285.7143 |        - |        - | 10,555 KB |
|                ParallelAsyncBuilderUsingStartChild_async |     AsyncBinds |     72.837 ms |   0.9044 ms |   0.8460 ms |     72.646 ms | 5250.0000 | 875.0000 | 125.0000 | 43,321 KB |
|      ParallelAsyncBuilderUsingStartImmediateAsTask_async |     AsyncBinds |     67.787 ms |   1.3305 ms |   1.6826 ms |     68.050 ms | 3125.0000 |        - |        - | 26,114 KB |
|                                                          |                |               |             |             |               |           |          |          |           |
|                                  AsyncBuilder_async_long | AsyncBindsLong | 11,449.167 ms |  79.4426 ms |  62.0235 ms | 11,438.866 ms |         - |        - |        - |    839 KB |
|             AsyncBuilder_async_long_applicative_overhead | AsyncBindsLong | 11,930.969 ms | 224.8506 ms | 322.4739 ms | 12,062.812 ms |         - |        - |        - |  1,057 KB |
|           ParallelAsyncBuilderUsingStartChild_async_long | AsyncBindsLong |  1,221.578 ms |  24.3126 ms |  28.9425 ms |  1,218.303 ms |         - |        - |        - |  4,315 KB |
| ParallelAsyncBuilderUsingStartImmediateAsTask_async_long | AsyncBindsLong |  1,229.845 ms |  23.8712 ms |  31.0393 ms |  1,228.502 ms |         - |        - |        - |  2,690 KB |
