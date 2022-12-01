``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22000.1219/21H2)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|                                          Method |    Categories |         Mean |        Error |       StdDev |       Median |  Ratio | RatioSD |       Gen0 |   Gen1 |   Allocated | Alloc Ratio |
|------------------------------------------------ |-------------- |-------------:|-------------:|-------------:|-------------:|-------:|--------:|-----------:|-------:|------------:|------------:|
|                          AsyncBinds_CSharpTasks |    AsyncBinds |   3,325.9 μs |     17.90 μs |     16.75 μs |   3,329.0 μs |   1.00 |    0.00 |     3.9063 |      - |    112011 B |        1.00 |
|                                  AsyncBinds_ply |    AsyncBinds |   4,028.4 μs |     16.22 μs |     13.54 μs |   4,025.2 μs |   1.21 |    0.01 |    39.0625 |      - |    672036 B |        6.00 |
|                                AsyncBinds_async |    AsyncBinds |  61,266.5 μs |    687.39 μs |    574.00 μs |  61,297.2 μs |  18.44 |    0.18 |   444.4444 |      - |   8224085 B |       73.42 |
|                                 AsyncBinds_task |    AsyncBinds |   3,540.1 μs |     16.54 μs |     13.81 μs |   3,539.1 μs |   1.07 |    0.00 |     7.8125 |      - |    128015 B |        1.14 |
|                            AsyncBinds_valueTask |    AsyncBinds |   3,547.2 μs |     11.49 μs |      9.59 μs |   3,543.7 μs |   1.07 |    0.01 |     7.8125 |      - |    136021 B |        1.21 |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |   3,741.0 μs |     23.27 μs |     21.77 μs |   3,744.1 μs |   1.12 |    0.01 |    23.4375 |      - |    424016 B |        3.79 |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |   3,593.0 μs |     27.79 μs |     24.63 μs |   3,586.9 μs |   1.08 |    0.01 |    11.7188 |      - |    184017 B |        1.64 |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |   3,606.1 μs |     16.99 μs |     15.06 μs |   3,602.2 μs |   1.08 |    0.01 |    27.3438 |      - |    440021 B |        3.93 |
|         AsyncBinds_cancellableTask_bindColdTask |    AsyncBinds |   3,534.9 μs |     48.84 μs |     45.69 μs |   3,540.6 μs |   1.06 |    0.02 |    11.7188 |      - |    200015 B |        1.79 |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |   3,624.9 μs |     29.66 μs |     27.75 μs |   3,621.8 μs |   1.09 |    0.01 |     7.8125 |      - |    200023 B |        1.79 |
|                                                 |               |              |              |              |              |        |         |            |        |             |             |
|                       ManyWriteFile_CSharpTasks | ManyWriteFile |     493.9 μs |      9.08 μs |      7.58 μs |     492.9 μs |   1.00 |    0.00 |          - |      - |      8239 B |        1.00 |
|                               ManyWriteFile_ply | ManyWriteFile |     577.1 μs |     13.05 μs |     37.24 μs |     567.9 μs |   1.16 |    0.07 |          - |      - |     10130 B |        1.23 |
|                             ManyWriteFile_async | ManyWriteFile |     696.6 μs |     15.91 μs |     45.64 μs |     677.7 μs |   1.41 |    0.10 |    18.5547 |      - |    298849 B |       36.27 |
|                              ManyWriteFile_task | ManyWriteFile |     512.5 μs |     13.52 μs |     39.44 μs |     497.7 μs |   1.06 |    0.07 |          - |      - |      8407 B |        1.02 |
|                         ManyWriteFile_valueTask | ManyWriteFile |     512.3 μs |     10.22 μs |     22.64 μs |     509.7 μs |   1.03 |    0.02 |     0.4883 |      - |      8414 B |        1.02 |
|                          ManyWriteFile_coldTask | ManyWriteFile |     517.0 μs |     14.86 μs |     42.39 μs |     509.2 μs |   1.03 |    0.10 |     1.9531 |      - |     32518 B |        3.95 |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |     546.0 μs |     13.20 μs |     38.29 μs |     544.6 μs |   1.05 |    0.05 |     1.9531 |      - |     32545 B |        3.95 |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |     530.0 μs |     17.92 μs |     51.42 μs |     521.0 μs |   1.12 |    0.10 |     1.9531 |      - |     32721 B |        3.97 |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     432.4 μs |     16.31 μs |     46.01 μs |     413.1 μs |   0.89 |    0.09 |          - |      - |      8537 B |        1.04 |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |     572.7 μs |     14.51 μs |     42.79 μs |     561.7 μs |   1.21 |    0.09 |    10.7422 |      - |    176552 B |       21.43 |
|                                                 |               |              |              |              |              |        |         |            |        |             |             |
|                       NonAsyncBinds_CSharpTasks | NonAsyncBinds |   5,467.4 μs |    160.22 μs |    472.41 μs |   5,204.7 μs |   1.00 |    0.00 |  5046.8750 |      - |  79200004 B |       1.000 |
|                               NonAsyncBinds_ply | NonAsyncBinds |   6,987.4 μs |    196.55 μs |    573.33 μs |   6,676.6 μs |   1.29 |    0.15 |  5046.8750 |      - |  79200004 B |       1.000 |
|                             NonAsyncBinds_async | NonAsyncBinds | 615,834.3 μs | 11,060.72 μs | 10,346.21 μs | 612,380.5 μs | 113.37 |   11.30 | 16000.0000 |      - | 251200432 B |       3.172 |
|                              NonAsyncBinds_task | NonAsyncBinds |   6,048.6 μs |    163.35 μs |    473.91 μs |   5,774.8 μs |   1.12 |    0.13 |  5046.8750 | 7.8125 |  79200004 B |       1.000 |
|                NonAsyncBinds_task_bindValueTask | NonAsyncBinds |   6,835.5 μs |     63.71 μs |     56.47 μs |   6,822.1 μs |   1.25 |    0.11 |   453.1250 |      - |   7200004 B |       0.091 |
|                         NonAsyncBinds_valueTask | NonAsyncBinds |   6,403.7 μs |    121.95 μs |    125.24 μs |   6,356.0 μs |   1.19 |    0.09 |          - |      - |         4 B |       0.000 |
|                  tenBindSync_valueTask_bindTask | NonAsyncBinds |   5,790.0 μs |    164.19 μs |    478.94 μs |   5,584.0 μs |   1.07 |    0.13 |  4585.9375 |      - |  72000004 B |       0.909 |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  13,081.5 μs |    348.32 μs |  1,021.57 μs |  12,666.3 μs |   2.41 |    0.26 |  7546.8750 |      - | 118400008 B |       1.495 |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  10,000.6 μs |    233.68 μs |    685.35 μs |   9,662.6 μs |   1.84 |    0.20 |  6015.6250 |      - |  94400008 B |       1.192 |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  14,202.6 μs |    371.16 μs |  1,076.80 μs |  13,883.6 μs |   2.62 |    0.31 |  7593.7500 |      - | 119200008 B |       1.505 |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  10,382.8 μs |    281.62 μs |    830.35 μs |   9,894.2 μs |   1.92 |    0.25 |  6062.5000 |      - |  95200008 B |       1.202 |
