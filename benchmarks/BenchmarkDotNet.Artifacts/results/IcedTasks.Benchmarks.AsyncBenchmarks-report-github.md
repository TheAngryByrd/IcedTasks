``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                        Method |    Categories |         Mean |      Error |      StdDev | Ratio | RatioSD |      Gen 0 |   Gen 1 |  Allocated |
|------------------------------ |-------------- |-------------:|-----------:|------------:|------:|--------:|-----------:|--------:|-----------:|
|             ManyWriteFile_ply | ManyWriteFile |     2.688 ms |  0.0629 ms |   0.1775 ms |  1.09 |    0.09 |          - |       - |      10 KB |
|           ManyWriteFile_async | ManyWriteFile |     2.958 ms |  0.0585 ms |   0.0927 ms |  1.19 |    0.05 |    35.1563 | 11.7188 |     292 KB |
|            ManyWriteFile_task | ManyWriteFile |     2.487 ms |  0.0496 ms |   0.0968 ms |  1.00 |    0.00 |          - |       - |       8 KB |
|        ManyWriteFile_coldTask | ManyWriteFile |     2.586 ms |  0.0474 ms |   0.0752 ms |  1.04 |    0.05 |          - |       - |       8 KB |
| ManyWriteFile_cancellableTask | ManyWriteFile |     2.710 ms |  0.0775 ms |   0.2187 ms |  1.10 |    0.11 |          - |       - |       8 KB |
|                               |               |              |            |             |       |         |            |         |            |
|             NonAsyncBinds_ply | NonAsyncBinds |    17.512 ms |  0.2568 ms |   0.2145 ms |  1.02 |    0.07 |  9468.7500 |       - |  77,344 KB |
|           NonAsyncBinds_async | NonAsyncBinds | 1,311.779 ms | 36.4711 ms | 105.2275 ms | 79.11 |    7.32 | 30000.0000 |       - | 248,438 KB |
|            NonAsyncBinds_task | NonAsyncBinds |    16.620 ms |  0.3640 ms |   1.0619 ms |  1.00 |    0.00 |  9468.7500 |       - |  77,344 KB |
|        NonAsyncBinds_coldTask | NonAsyncBinds |    26.570 ms |  0.5262 ms |   1.2404 ms |  1.63 |    0.10 | 11562.5000 |       - |  94,531 KB |
| NonAsyncBinds_cancellableTask | NonAsyncBinds |    25.963 ms |  0.5191 ms |   0.9876 ms |  1.58 |    0.12 | 11656.2500 |       - |  95,313 KB |
|                               |               |              |            |             |       |         |            |         |            |
|                AsyncBinds_ply |    AsyncBinds |    23.491 ms |  0.4660 ms |   0.7785 ms |  1.13 |    0.06 |    62.5000 |       - |     656 KB |
|              AsyncBinds_async |    AsyncBinds |   125.269 ms |  3.7783 ms |  11.0810 ms |  5.89 |    0.57 |  1000.0000 |       - |   8,375 KB |
|               AsyncBinds_task |    AsyncBinds |    20.903 ms |  0.4142 ms |   0.7470 ms |  1.00 |    0.00 |          - |       - |     188 KB |
|           AsyncBinds_coldTask |    AsyncBinds |    22.618 ms |  0.4487 ms |   0.7741 ms |  1.09 |    0.05 |    31.2500 |       - |     328 KB |
|    AsyncBinds_cancellableTask |    AsyncBinds |    23.023 ms |  0.4584 ms |   1.1331 ms |  1.11 |    0.08 |    31.2500 |       - |     344 KB |
