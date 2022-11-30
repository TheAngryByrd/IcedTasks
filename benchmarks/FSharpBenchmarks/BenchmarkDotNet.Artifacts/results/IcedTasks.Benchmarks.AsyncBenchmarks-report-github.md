``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22000.1219/21H2)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|                                          Method |    Categories |         Mean |       Error |      StdDev |       Median | Ratio | RatioSD |       Gen0 |   Gen1 |    Allocated | Alloc Ratio |
|------------------------------------------------ |-------------- |-------------:|------------:|------------:|-------------:|------:|--------:|-----------:|-------:|-------------:|------------:|
|                          AsyncBinds_CSharpTasks |    AsyncBinds |   3,442.7 μs |    17.36 μs |    15.39 μs |   3,442.4 μs |  1.00 |    0.00 |     3.9063 |      - |    109.39 KB |        1.00 |
|                                  AsyncBinds_ply |    AsyncBinds |   4,639.2 μs |    92.09 μs |   232.73 μs |   4,593.0 μs |  1.40 |    0.06 |    39.0625 |      - |     656.3 KB |        6.00 |
|                                AsyncBinds_async |    AsyncBinds |  61,165.7 μs |   877.98 μs |   733.16 μs |  61,113.8 μs | 17.76 |    0.23 |   428.5714 |      - |   8031.36 KB |       73.42 |
|                                 AsyncBinds_task |    AsyncBinds |   3,469.1 μs |    17.97 μs |    16.80 μs |   3,466.3 μs |  1.01 |    0.01 |     7.8125 |      - |    125.02 KB |        1.14 |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |   3,615.9 μs |    47.39 μs |    42.01 μs |   3,598.5 μs |  1.05 |    0.01 |    23.4375 |      - |    414.08 KB |        3.79 |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |   3,540.7 μs |    20.58 μs |    17.19 μs |   3,539.8 μs |  1.03 |    0.01 |    11.7188 |      - |     179.7 KB |        1.64 |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |   3,875.8 μs |    77.12 μs |   189.17 μs |   3,903.7 μs |  1.07 |    0.04 |    27.3438 |      - |    429.71 KB |        3.93 |
|         AsyncBinds_cancellableTask_bindColdTask |    AsyncBinds |   3,897.0 μs |    95.03 μs |   280.21 μs |   3,805.4 μs |  1.12 |    0.03 |     7.8125 |      - |    195.33 KB |        1.79 |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |   3,858.6 μs |   140.61 μs |   414.60 μs |   3,788.8 μs |  1.25 |    0.07 |    11.7188 |      - |    195.33 KB |        1.79 |
|                                                 |               |              |             |             |              |       |         |            |        |              |             |
|                       ManyWriteFile_CSharpTasks | ManyWriteFile |     603.5 μs |    32.89 μs |    90.58 μs |     574.7 μs |  1.00 |    0.00 |          - |      - |      8.04 KB |        1.00 |
|                               ManyWriteFile_ply | ManyWriteFile |     619.3 μs |    22.69 μs |    64.74 μs |     609.4 μs |  1.05 |    0.19 |          - |      - |      9.88 KB |        1.23 |
|                             ManyWriteFile_async | ManyWriteFile |     708.3 μs |    16.88 μs |    48.43 μs |     693.6 μs |  1.20 |    0.16 |    18.5547 |      - |    291.84 KB |       36.29 |
|                              ManyWriteFile_task | ManyWriteFile |     574.8 μs |    22.43 μs |    63.27 μs |     565.0 μs |  0.97 |    0.17 |          - |      - |      8.23 KB |        1.02 |
|                          ManyWriteFile_coldTask | ManyWriteFile |     576.4 μs |    26.70 μs |    76.19 μs |     550.0 μs |  0.98 |    0.18 |     1.9531 |      - |     31.77 KB |        3.95 |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |     590.4 μs |    21.37 μs |    58.87 μs |     587.3 μs |  1.00 |    0.15 |     1.9531 |      - |     31.77 KB |        3.95 |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |     587.3 μs |    20.28 μs |    59.49 μs |     575.9 μs |  1.00 |    0.18 |     1.9531 |      - |     31.93 KB |        3.97 |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     513.8 μs |    24.74 μs |    70.18 μs |     507.7 μs |  0.87 |    0.15 |     0.4883 |      - |      8.32 KB |        1.04 |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |     640.7 μs |    23.20 μs |    64.30 μs |     626.5 μs |  1.08 |    0.16 |    10.7422 |      - |    172.42 KB |       21.44 |
|                                                 |               |              |             |             |              |       |         |            |        |              |             |
|                       NonAsyncBinds_CSharpTasks | NonAsyncBinds |   8,185.7 μs |   270.83 μs |   794.30 μs |   8,294.6 μs |  1.00 |    0.00 |  5046.8750 |      - |  77343.75 KB |        1.00 |
|                               NonAsyncBinds_ply | NonAsyncBinds |   9,590.6 μs |   440.91 μs | 1,300.04 μs |   9,369.0 μs |  1.18 |    0.20 |  5046.8750 |      - |  77343.76 KB |        1.00 |
|                             NonAsyncBinds_async | NonAsyncBinds | 638,314.4 μs | 4,125.87 μs | 3,445.29 μs | 638,218.6 μs | 82.25 |    8.85 | 16000.0000 |      - | 245313.14 KB |        3.17 |
|                              NonAsyncBinds_task | NonAsyncBinds |   7,102.8 μs |   221.35 μs |   627.92 μs |   6,975.9 μs |  0.88 |    0.12 |  5046.8750 | 7.8125 |  77343.75 KB |        1.00 |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  15,101.5 μs |   319.33 μs |   857.85 μs |  14,961.5 μs |  1.87 |    0.20 |  7546.8750 |      - | 115625.01 KB |        1.49 |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  13,099.4 μs |   512.94 μs | 1,496.26 μs |  12,774.3 μs |  1.61 |    0.24 |  6015.6250 |      - |  92187.51 KB |        1.19 |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  16,946.2 μs |   428.99 μs | 1,264.88 μs |  16,918.4 μs |  2.09 |    0.27 |  7593.7500 |      - | 116406.26 KB |        1.51 |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  13,613.0 μs |   555.94 μs | 1,630.48 μs |  13,240.8 μs |  1.68 |    0.24 |  6062.5000 |      - |  92968.76 KB |        1.20 |
