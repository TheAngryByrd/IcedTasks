``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                          Method |    Categories |         Mean |       Error |      StdDev | Ratio | RatioSD |      Gen 0 |   Gen 1 |  Allocated |
|------------------------------------------------ |-------------- |-------------:|------------:|------------:|------:|--------:|-----------:|--------:|-----------:|
|                               ManyWriteFile_ply | ManyWriteFile |   1,089.0 μs |    21.76 μs |    51.72 μs |  1.08 |    0.05 |          - |       - |      10 KB |
|                             ManyWriteFile_async | ManyWriteFile |   1,284.2 μs |    25.50 μs |    64.45 μs |  1.30 |    0.09 |    35.1563 | 11.7188 |     292 KB |
|                              ManyWriteFile_task | ManyWriteFile |   1,004.4 μs |    19.78 μs |    28.36 μs |  1.00 |    0.00 |     0.9766 |       - |       8 KB |
|                          ManyWriteFile_coldTask | ManyWriteFile |     997.2 μs |    19.84 μs |    36.28 μs |  0.99 |    0.04 |          - |       - |       8 KB |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |     979.9 μs |    18.89 μs |    22.49 μs |  0.98 |    0.04 |     0.9766 |       - |       8 KB |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |   1,006.7 μs |    20.05 μs |    45.66 μs |  1.01 |    0.05 |          - |       - |       9 KB |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     875.5 μs |    16.31 μs |    24.91 μs |  0.87 |    0.04 |     0.9766 |       - |       8 KB |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |   1,068.6 μs |    21.30 μs |    47.63 μs |  1.07 |    0.06 |     9.7656 |  3.9063 |      79 KB |
|                                                 |               |              |             |             |       |         |            |         |            |
|                               NonAsyncBinds_ply | NonAsyncBinds |  13,058.9 μs |   260.49 μs |   381.83 μs |  1.18 |    0.05 |  9468.7500 |       - |  77,344 KB |
|                             NonAsyncBinds_async | NonAsyncBinds | 782,519.8 μs | 8,152.95 μs | 6,365.29 μs | 70.01 |    2.30 | 30000.0000 |       - | 248,443 KB |
|                              NonAsyncBinds_task | NonAsyncBinds |  11,086.4 μs |   221.10 μs |   337.64 μs |  1.00 |    0.00 |  9468.7500 |       - |  77,344 KB |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  17,893.7 μs |   253.95 μs |   225.12 μs |  1.61 |    0.05 | 11281.2500 |       - |  92,188 KB |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  20,402.1 μs |   483.94 μs | 1,419.30 μs |  1.84 |    0.12 | 11281.2500 |       - |  92,188 KB |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  20,383.2 μs |   507.50 μs | 1,480.39 μs |  1.88 |    0.13 | 11375.0000 |       - |  92,969 KB |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  21,220.2 μs |   411.01 μs | 1,172.63 μs |  1.96 |    0.13 | 11375.0000 |       - |  92,969 KB |
|                                                 |               |              |             |             |       |         |            |         |            |
|                                  AsyncBinds_ply |    AsyncBinds |  19,525.0 μs |   527.99 μs | 1,556.80 μs |  1.00 |    0.08 |    78.1250 |       - |     656 KB |
|                                AsyncBinds_async |    AsyncBinds |  83,214.8 μs | 1,660.44 μs | 3,199.10 μs |  4.38 |    0.31 |  1000.0000 |       - |   8,375 KB |
|                                 AsyncBinds_task |    AsyncBinds |  19,206.0 μs |   383.78 μs | 1,004.28 μs |  1.00 |    0.00 |    15.6250 |       - |     188 KB |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |  18,768.1 μs |   374.90 μs | 1,019.95 μs |  0.98 |    0.07 |    31.2500 |       - |     305 KB |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |  22,349.7 μs |   456.38 μs | 1,324.05 μs |  1.16 |    0.08 |    31.2500 |       - |     305 KB |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |  21,184.1 μs |   581.62 μs | 1,705.80 μs |  1.13 |    0.10 |    31.2500 |       - |     320 KB |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |  19,440.7 μs |   386.27 μs |   932.89 μs |  1.02 |    0.07 |    31.2500 |       - |     320 KB |
