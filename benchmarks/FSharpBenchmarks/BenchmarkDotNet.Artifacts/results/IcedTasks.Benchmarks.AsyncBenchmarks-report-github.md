``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                          Method |    Categories |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |      Gen 0 |   Gen 1 |  Allocated |
|------------------------------------------------ |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----------:|--------:|-----------:|
|                       ManyWriteFile_CSharpTasks | ManyWriteFile |   1,078.9 μs |     25.94 μs |     73.18 μs |   1,060.2 μs |  1.00 |    0.00 |          - |       - |      10 KB |
|                               ManyWriteFile_ply | ManyWriteFile |   1,182.8 μs |     50.84 μs |    148.30 μs |   1,115.1 μs |  1.10 |    0.16 |          - |       - |      10 KB |
|                             ManyWriteFile_async | ManyWriteFile |   1,300.5 μs |     21.41 μs |     43.73 μs |   1,293.1 μs |  1.22 |    0.09 |    35.1563 | 11.7188 |     292 KB |
|                              ManyWriteFile_task | ManyWriteFile |   1,051.1 μs |     21.02 μs |     44.34 μs |   1,050.3 μs |  0.98 |    0.08 |          - |       - |       8 KB |
|                          ManyWriteFile_coldTask | ManyWriteFile |   1,135.9 μs |     39.39 μs |    115.54 μs |   1,086.7 μs |  1.06 |    0.11 |          - |       - |       8 KB |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |   1,124.4 μs |     43.13 μs |    125.80 μs |   1,073.6 μs |  1.04 |    0.12 |          - |       - |       8 KB |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |   1,067.2 μs |     25.44 μs |     72.16 μs |   1,051.6 μs |  0.99 |    0.10 |          - |       - |       9 KB |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     951.3 μs |     18.90 μs |     47.06 μs |     938.7 μs |  0.89 |    0.07 |     0.9766 |       - |       8 KB |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |   1,109.2 μs |     21.85 μs |     51.51 μs |   1,105.6 μs |  1.03 |    0.08 |     9.7656 |  3.9063 |      79 KB |
|                                                 |               |              |              |              |              |       |         |            |         |            |
|                       NonAsyncBinds_CSharpTasks | NonAsyncBinds |  10,343.1 μs |    198.70 μs |    204.05 μs |  10,319.2 μs |  1.00 |    0.00 |  9468.7500 |       - |  77,344 KB |
|                               NonAsyncBinds_ply | NonAsyncBinds |  14,213.2 μs |    280.04 μs |    354.17 μs |  14,095.1 μs |  1.39 |    0.05 |  9468.7500 |       - |  77,344 KB |
|                             NonAsyncBinds_async | NonAsyncBinds | 833,404.7 μs | 14,209.76 μs | 13,291.82 μs | 830,307.9 μs | 80.75 |    2.57 | 30000.0000 |       - | 248,439 KB |
|                              NonAsyncBinds_task | NonAsyncBinds |  12,405.7 μs |    244.80 μs |    388.28 μs |  12,435.8 μs |  1.19 |    0.04 |  9468.7500 |       - |  77,344 KB |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  20,537.8 μs |    359.23 μs |    413.68 μs |  20,457.3 μs |  1.99 |    0.07 | 11281.2500 |       - |  92,188 KB |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  20,325.6 μs |    399.94 μs |    533.91 μs |  20,309.0 μs |  1.94 |    0.05 | 11281.2500 |       - |  92,188 KB |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  21,093.6 μs |    416.13 μs |    479.22 μs |  21,166.3 μs |  2.04 |    0.04 | 11375.0000 |       - |  92,969 KB |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  21,430.7 μs |    324.82 μs |    287.95 μs |  21,404.5 μs |  2.08 |    0.05 | 11375.0000 |       - |  92,969 KB |
|                                                 |               |              |              |              |              |       |         |            |         |            |
|                          AsyncBinds_CSharpTasks |    AsyncBinds |  16,633.3 μs |    390.48 μs |  1,151.35 μs |  16,912.3 μs |  1.00 |    0.00 |          - |       - |     109 KB |
|                                  AsyncBinds_ply |    AsyncBinds |  19,719.0 μs |    390.94 μs |    480.11 μs |  19,798.5 μs |  1.30 |    0.08 |    62.5000 |       - |     656 KB |
|                                AsyncBinds_async |    AsyncBinds |  80,239.3 μs |  1,338.74 μs |  1,186.76 μs |  79,870.7 μs |  5.37 |    0.25 |  1000.0000 |       - |   8,376 KB |
|                                 AsyncBinds_task |    AsyncBinds |  18,808.0 μs |    450.99 μs |  1,329.76 μs |  18,603.0 μs |  1.14 |    0.13 |    15.6250 |       - |     188 KB |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |  18,126.6 μs |    361.36 μs |    983.10 μs |  17,961.7 μs |  1.10 |    0.09 |    31.2500 |       - |     305 KB |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |  18,207.5 μs |    363.97 μs |    608.11 μs |  18,177.3 μs |  1.18 |    0.06 |    31.2500 |       - |     305 KB |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |  18,366.8 μs |    359.93 μs |    527.57 μs |  18,456.9 μs |  1.21 |    0.07 |    31.2500 |       - |     320 KB |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |  15,207.0 μs |    298.83 μs |    377.93 μs |  15,213.7 μs |  1.01 |    0.05 |    31.2500 |       - |     320 KB |