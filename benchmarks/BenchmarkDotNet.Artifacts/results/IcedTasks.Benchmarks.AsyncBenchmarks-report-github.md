``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                         Method |    Categories |         Mean |      Error |      StdDev |       Median |      Gen 0 |   Gen 1 |  Allocated |
|------------------------------- |-------------- |-------------:|-----------:|------------:|-------------:|-----------:|--------:|-----------:|
|              ManyWriteFile_ply | ManyWriteFile |     2.749 ms |  0.0534 ms |   0.0907 ms |     2.730 ms |          - |       - |      10 KB |
|            ManyWriteFile_async | ManyWriteFile |     2.977 ms |  0.0593 ms |   0.0906 ms |     2.968 ms |    35.1563 | 11.7188 |     292 KB |
|             ManyWriteFile_task | ManyWriteFile |     2.761 ms |  0.0549 ms |   0.1095 ms |     2.769 ms |          - |       - |       8 KB |
|         ManyWriteFile_coldTask | ManyWriteFile |     2.797 ms |  0.0547 ms |   0.0958 ms |     2.805 ms |          - |       - |       8 KB |
|  ManyWriteFile_cancellableTask | ManyWriteFile |     2.781 ms |  0.0548 ms |   0.1042 ms |     2.752 ms |          - |       - |       8 KB |
|                                |               |              |            |             |              |            |         |            |
|              NonAsyncBinds_ply | NonAsyncBinds |    18.027 ms |  0.3547 ms |   0.3795 ms |    17.964 ms |  9468.7500 |       - |  77,344 KB |
|            NonAsyncBinds_async | NonAsyncBinds | 1,268.306 ms | 46.0299 ms | 132.8067 ms | 1,219.354 ms | 30000.0000 |       - | 248,438 KB |
|             NonAsyncBinds_task | NonAsyncBinds |    14.717 ms |  0.1840 ms |   0.1537 ms |    14.714 ms |  9468.7500 |       - |  77,344 KB |
|         NonAsyncBinds_coldTask | NonAsyncBinds |    27.717 ms |  0.5509 ms |   1.3820 ms |    28.139 ms | 11562.5000 |       - |  94,531 KB |
| NonAsyncBinds_cancellationTask | NonAsyncBinds |    24.367 ms |  0.2168 ms |   0.1810 ms |    24.439 ms | 11656.2500 |       - |  95,313 KB |
|                                |               |              |            |             |              |            |         |            |
|                 AsyncBinds_ply |    AsyncBinds |    23.127 ms |  0.3770 ms |   0.3342 ms |    23.149 ms |    62.5000 |       - |     656 KB |
|               AsyncBinds_async |    AsyncBinds |   110.963 ms |  2.1657 ms |   4.7080 ms |   109.955 ms |  1000.0000 |       - |   8,375 KB |
|                AsyncBinds_task |    AsyncBinds |    22.727 ms |  0.4370 ms |   0.9122 ms |    22.564 ms |          - |       - |     188 KB |
|            AsyncBinds_coldTask |    AsyncBinds |    21.655 ms |  0.4086 ms |   0.4013 ms |    21.704 ms |    31.2500 |       - |     328 KB |
|     AsyncBinds_cancellableTask |    AsyncBinds |    22.558 ms |  0.4411 ms |   0.4126 ms |    22.610 ms |    31.2500 |       - |     344 KB |
