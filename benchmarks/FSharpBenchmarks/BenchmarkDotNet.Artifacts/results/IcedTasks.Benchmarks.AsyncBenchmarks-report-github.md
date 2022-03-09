``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                          Method |    Categories |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |      Gen 0 |   Gen 1 |  Gen 2 |  Allocated |
|------------------------------------------------ |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----------:|--------:|-------:|-----------:|
|                       ManyWriteFile_CSharpTasks | ManyWriteFile |   1,017.6 μs |     19.77 μs |     21.16 μs |   1,017.2 μs |  1.00 |    0.00 |          - |       - |      - |      10 KB |
|                               ManyWriteFile_ply | ManyWriteFile |   1,221.4 μs |     52.10 μs |    148.66 μs |   1,169.5 μs |  1.25 |    0.14 |          - |       - |      - |      10 KB |
|                             ManyWriteFile_async | ManyWriteFile |   1,307.2 μs |     25.89 μs |     41.06 μs |   1,294.4 μs |  1.29 |    0.04 |    35.1563 | 11.7188 | 1.9531 |     292 KB |
|                              ManyWriteFile_task | ManyWriteFile |   1,061.5 μs |     21.11 μs |     48.49 μs |   1,049.5 μs |  1.04 |    0.04 |          - |       - |      - |       8 KB |
|                          ManyWriteFile_coldTask | ManyWriteFile |   1,057.7 μs |     21.10 μs |     53.71 μs |   1,038.9 μs |  1.05 |    0.08 |          - |       - |      - |       8 KB |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |   1,099.6 μs |     23.24 μs |     67.78 μs |   1,102.5 μs |  1.05 |    0.04 |          - |       - |      - |       8 KB |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |   1,082.9 μs |     21.58 μs |     58.70 μs |   1,063.9 μs |  1.08 |    0.07 |          - |       - |      - |       9 KB |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     951.3 μs |     18.68 μs |     32.71 μs |     947.9 μs |  0.94 |    0.04 |     0.9766 |       - |      - |       8 KB |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |   1,241.2 μs |     54.48 μs |    155.45 μs |   1,201.5 μs |  1.20 |    0.16 |     9.7656 |  3.9063 |      - |      79 KB |
|                                                 |               |              |              |              |              |       |         |            |         |        |            |
|                       NonAsyncBinds_CSharpTasks | NonAsyncBinds |  11,670.3 μs |    231.87 μs |    339.87 μs |  11,684.8 μs |  1.00 |    0.00 |  9468.7500 |       - |      - |  77,344 KB |
|                               NonAsyncBinds_ply | NonAsyncBinds |  16,193.3 μs |    242.80 μs |    215.23 μs |  16,209.9 μs |  1.40 |    0.04 |  9468.7500 |       - |      - |  77,344 KB |
|                             NonAsyncBinds_async | NonAsyncBinds | 909,374.0 μs | 12,104.86 μs | 10,730.64 μs | 911,352.0 μs | 78.89 |    2.61 | 30000.0000 |       - |      - | 248,443 KB |
|                              NonAsyncBinds_task | NonAsyncBinds |  13,635.9 μs |    169.10 μs |    141.21 μs |  13,634.0 μs |  1.19 |    0.04 |  9468.7500 |       - |      - |  77,344 KB |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  22,285.8 μs |    328.94 μs |    291.60 μs |  22,266.3 μs |  1.93 |    0.05 | 11281.2500 |       - |      - |  92,188 KB |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  22,976.5 μs |    457.59 μs |    801.43 μs |  23,027.2 μs |  1.97 |    0.08 | 11281.2500 |       - |      - |  92,188 KB |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  23,018.4 μs |    174.25 μs |    145.50 μs |  23,031.6 μs |  2.00 |    0.06 | 11375.0000 |       - |      - |  92,969 KB |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  23,183.0 μs |    371.83 μs |    397.86 μs |  23,260.6 μs |  2.00 |    0.06 | 11375.0000 |       - |      - |  92,969 KB |
|                                                 |               |              |              |              |              |       |         |            |         |        |            |
|                          AsyncBinds_CSharpTasks |    AsyncBinds |  17,388.5 μs |    603.48 μs |  1,779.38 μs |  17,583.3 μs |  1.00 |    0.00 |          - |       - |      - |     109 KB |
|                                  AsyncBinds_ply |    AsyncBinds |  22,273.6 μs |    525.30 μs |  1,507.19 μs |  22,066.7 μs |  1.30 |    0.17 |    62.5000 |       - |      - |     656 KB |
|                                AsyncBinds_async |    AsyncBinds |  88,393.7 μs |  1,737.46 μs |  2,378.26 μs |  87,425.4 μs |  5.71 |    0.56 |  1000.0000 |       - |      - |   8,375 KB |
|                                 AsyncBinds_task |    AsyncBinds |  19,577.5 μs |    442.81 μs |  1,277.62 μs |  19,482.5 μs |  1.14 |    0.13 |          - |       - |      - |     188 KB |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |  18,582.4 μs |    623.38 μs |  1,828.28 μs |  18,193.2 μs |  1.09 |    0.18 |    31.2500 |       - |      - |     305 KB |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |  19,229.0 μs |    375.91 μs |    742.00 μs |  19,219.6 μs |  1.19 |    0.14 |    31.2500 |       - |      - |     305 KB |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |  19,890.7 μs |    392.33 μs |    687.13 μs |  19,941.4 μs |  1.24 |    0.13 |    31.2500 |       - |      - |     320 KB |
|         AsyncBinds_cancellableTask_bindColdTask |    AsyncBinds |  19,412.4 μs |    387.11 μs |    857.81 μs |  19,334.6 μs |  1.19 |    0.12 |    31.2500 |       - |      - |     320 KB |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |  20,201.2 μs |    400.21 μs |    852.88 μs |  20,049.0 μs |  1.25 |    0.15 |    31.2500 |       - |      - |     320 KB |
