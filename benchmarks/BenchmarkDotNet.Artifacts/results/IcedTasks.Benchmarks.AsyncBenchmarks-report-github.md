``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                         Method |    Categories |         Mean |      Error |      StdDev |      Gen 0 |  Gen 1 |  Allocated |
|------------------------------- |-------------- |-------------:|-----------:|------------:|-----------:|-------:|-----------:|
|              ManyWriteFile_ply | ManyWriteFile |     3.128 ms |  0.1152 ms |   0.3362 ms |          - |      - |      10 KB |
|            ManyWriteFile_async | ManyWriteFile |     3.549 ms |  0.0713 ms |   0.2035 ms |    31.2500 | 7.8125 |     292 KB |
|             ManyWriteFile_task | ManyWriteFile |     3.199 ms |  0.0928 ms |   0.2707 ms |          - |      - |       8 KB |
|         ManyWriteFile_coldTask | ManyWriteFile |     3.716 ms |  0.1606 ms |   0.4735 ms |    23.4375 | 7.8125 |     219 KB |
|  ManyWriteFile_cancellableTask | ManyWriteFile |     3.462 ms |  0.1497 ms |   0.4391 ms |    15.6250 | 7.8125 |     134 KB |
|                                |               |              |            |             |            |        |            |
|              NonAsyncBinds_ply | NonAsyncBinds |    18.858 ms |  0.3729 ms |   0.8862 ms |  9468.7500 |      - |  77,344 KB |
|            NonAsyncBinds_async | NonAsyncBinds | 1,401.882 ms | 53.8624 ms | 158.8146 ms | 30000.0000 |      - | 248,438 KB |
|             NonAsyncBinds_task | NonAsyncBinds |    16.959 ms |  0.3377 ms |   0.7759 ms |  9468.7500 |      - |  77,344 KB |
|         NonAsyncBinds_coldTask | NonAsyncBinds |    51.517 ms |  1.0237 ms |   2.9039 ms | 26400.0000 |      - | 216,406 KB |
| NonAsyncBinds_cancellationTask | NonAsyncBinds |    82.281 ms |  1.6268 ms |   2.2805 ms | 39571.4286 |      - | 324,219 KB |
|                                |               |              |            |             |            |        |            |
|                 AsyncBinds_ply |    AsyncBinds |    24.414 ms |  0.4566 ms |   0.8233 ms |    62.5000 |      - |     656 KB |
|               AsyncBinds_async |    AsyncBinds |   115.684 ms |  2.5590 ms |   7.4647 ms |  1000.0000 |      - |   8,375 KB |
|                AsyncBinds_task |    AsyncBinds |    22.444 ms |  0.4395 ms |   0.7812 ms |          - |      - |     188 KB |
|            AsyncBinds_coldTask |    AsyncBinds |    23.649 ms |  0.4698 ms |   0.9382 ms |   218.7500 |      - |   1,922 KB |
|     AsyncBinds_cancellableTask |    AsyncBinds |    23.748 ms |  0.4737 ms |   0.9569 ms |   218.7500 |      - |   1,930 KB |
