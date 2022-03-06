``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                         Method |    Categories |         Mean |      Error |     StdDev |       Median |      Gen 0 |   Gen 1 |  Allocated |
|------------------------------- |-------------- |-------------:|-----------:|-----------:|-------------:|-----------:|--------:|-----------:|
|              ManyWriteFile_ply | ManyWriteFile |     2.860 ms |  0.0663 ms |  0.1892 ms |     2.805 ms |          - |       - |      10 KB |
|            ManyWriteFile_async | ManyWriteFile |     3.020 ms |  0.0509 ms |  0.0476 ms |     3.042 ms |    35.1563 | 11.7188 |     292 KB |
|             ManyWriteFile_task | ManyWriteFile |     2.605 ms |  0.0512 ms |  0.0734 ms |     2.594 ms |          - |       - |       8 KB |
|         ManyWriteFile_coldTask | ManyWriteFile |     2.655 ms |  0.0520 ms |  0.0578 ms |     2.662 ms |          - |       - |       8 KB |
|  ManyWriteFile_cancellableTask | ManyWriteFile |     2.670 ms |  0.0524 ms |  0.0815 ms |     2.663 ms |          - |       - |       8 KB |
|                                |               |              |            |            |              |            |         |            |
|              NonAsyncBinds_ply | NonAsyncBinds |    17.165 ms |  0.3325 ms |  0.4205 ms |    17.174 ms |  9468.7500 |       - |  77,344 KB |
|            NonAsyncBinds_async | NonAsyncBinds | 1,146.640 ms | 22.8664 ms | 25.4160 ms | 1,145.711 ms | 30000.0000 |       - | 248,439 KB |
|             NonAsyncBinds_task | NonAsyncBinds |    14.129 ms |  0.1884 ms |  0.1670 ms |    14.157 ms |  9468.7500 |       - |  77,344 KB |
|         NonAsyncBinds_coldTask | NonAsyncBinds |    24.192 ms |  0.2049 ms |  0.1816 ms |    24.180 ms | 11562.5000 |       - |  94,531 KB |
| NonAsyncBinds_cancellationTask | NonAsyncBinds |    23.823 ms |  0.2045 ms |  0.1913 ms |    23.760 ms | 11656.2500 |       - |  95,313 KB |
|                                |               |              |            |            |              |            |         |            |
|                 AsyncBinds_ply |    AsyncBinds |    22.826 ms |  0.4493 ms |  0.6996 ms |    22.757 ms |    62.5000 |       - |     656 KB |
|               AsyncBinds_async |    AsyncBinds |   106.207 ms |  2.1060 ms |  3.6327 ms |   105.664 ms |  1000.0000 |       - |   8,375 KB |
|                AsyncBinds_task |    AsyncBinds |    20.759 ms |  0.4147 ms |  0.7990 ms |    20.667 ms |          - |       - |     188 KB |
|            AsyncBinds_coldTask |    AsyncBinds |    21.133 ms |  0.4164 ms |  0.8412 ms |    20.993 ms |    31.2500 |       - |     328 KB |
|     AsyncBinds_cancellableTask |    AsyncBinds |    21.651 ms |  0.4324 ms |  0.5468 ms |    21.528 ms |    31.2500 |       - |     344 KB |
