``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22000.1219/21H2)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|                                          Method |    Categories |         Mean |        Error |       StdDev |       Median |  Ratio | RatioSD |       Gen0 |    Allocated | Alloc Ratio |
|------------------------------------------------ |-------------- |-------------:|-------------:|-------------:|-------------:|-------:|--------:|-----------:|-------------:|------------:|
|                          AsyncBinds_CSharpTasks |    AsyncBinds |   3,329.6 μs |     28.27 μs |     26.45 μs |   3,334.1 μs |   1.00 |    0.00 |     3.9063 |    109.39 KB |        1.00 |
|                                  AsyncBinds_ply |    AsyncBinds |   4,031.5 μs |     31.04 μs |     29.03 μs |   4,037.5 μs |   1.21 |    0.01 |    39.0625 |    656.28 KB |        6.00 |
|                                AsyncBinds_async |    AsyncBinds |  58,129.1 μs |  1,131.04 μs |  1,210.20 μs |  58,010.4 μs |  17.38 |    0.37 |   444.4444 |   8031.33 KB |       73.42 |
|                                 AsyncBinds_task |    AsyncBinds |   3,489.1 μs |     26.76 μs |     23.73 μs |   3,488.1 μs |   1.05 |    0.01 |     7.8125 |    125.01 KB |        1.14 |
|                    AsyncBinds_coldTask_bindTask |    AsyncBinds |   3,673.6 μs |     51.34 μs |     48.02 μs |   3,672.7 μs |   1.10 |    0.02 |    23.4375 |    414.08 KB |        3.79 |
|                AsyncBinds_coldTask_bindColdTask |    AsyncBinds |   3,615.4 μs |     41.60 μs |     36.87 μs |   3,619.7 μs |   1.09 |    0.02 |    11.7188 |     179.7 KB |        1.64 |
|             AsyncBinds_cancellableTask_bindTask |    AsyncBinds |   3,529.3 μs |     48.39 μs |     42.90 μs |   3,514.9 μs |   1.06 |    0.02 |    27.3438 |     429.7 KB |        3.93 |
|         AsyncBinds_cancellableTask_bindColdTask |    AsyncBinds |   3,537.8 μs |     42.27 μs |     39.54 μs |   3,534.9 μs |   1.06 |    0.02 |    11.7188 |    195.32 KB |        1.79 |
|  AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |   3,614.9 μs |     25.58 μs |     23.93 μs |   3,612.6 μs |   1.09 |    0.01 |    11.7188 |    195.32 KB |        1.79 |
|                                                 |               |              |              |              |              |        |         |            |              |             |
|                       ManyWriteFile_CSharpTasks | ManyWriteFile |     506.0 μs |     10.06 μs |     21.01 μs |     501.4 μs |   1.00 |    0.00 |          - |      8.04 KB |        1.00 |
|                               ManyWriteFile_ply | ManyWriteFile |     620.4 μs |     15.20 μs |     43.36 μs |     613.8 μs |   1.24 |    0.11 |          - |      9.88 KB |        1.23 |
|                             ManyWriteFile_async | ManyWriteFile |     685.9 μs |     13.71 μs |     34.14 μs |     678.4 μs |   1.36 |    0.08 |    17.5781 |    291.85 KB |       36.29 |
|                              ManyWriteFile_task | ManyWriteFile |     522.9 μs |     13.02 μs |     36.50 μs |     518.7 μs |   1.05 |    0.08 |          - |      8.22 KB |        1.02 |
|                          ManyWriteFile_coldTask | ManyWriteFile |     551.3 μs |     17.84 μs |     50.90 μs |     534.6 μs |   1.10 |    0.13 |     1.9531 |     31.75 KB |        3.95 |
|                   ManyWriteFile_cancellableTask | ManyWriteFile |     538.7 μs |     16.97 μs |     48.95 μs |     526.9 μs |   1.07 |    0.10 |     1.9531 |     31.76 KB |        3.95 |
|  ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |     537.2 μs |     16.01 μs |     46.70 μs |     527.8 μs |   1.06 |    0.12 |     1.9531 |     31.91 KB |        3.97 |
| ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     470.3 μs |     13.95 μs |     41.13 μs |     460.0 μs |   0.91 |    0.08 |     0.4883 |      8.33 KB |        1.04 |
| ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |     566.3 μs |     13.07 μs |     38.12 μs |     561.6 μs |   1.15 |    0.07 |     9.7656 |    149.01 KB |       18.53 |
|                                                 |               |              |              |              |              |        |         |            |              |             |
|                       NonAsyncBinds_CSharpTasks | NonAsyncBinds |   5,439.2 μs |    184.20 μs |    540.22 μs |   5,198.5 μs |   1.00 |    0.00 |  5046.8750 |  77343.75 KB |        1.00 |
|                               NonAsyncBinds_ply | NonAsyncBinds |   6,704.3 μs |    184.23 μs |    528.58 μs |   6,423.5 μs |   1.24 |    0.16 |  5046.8750 |  77343.76 KB |        1.00 |
|                             NonAsyncBinds_async | NonAsyncBinds | 599,715.7 μs | 11,605.08 μs | 12,417.30 μs | 594,821.4 μs | 111.24 |    9.89 | 16000.0000 | 245313.09 KB |        3.17 |
|                              NonAsyncBinds_task | NonAsyncBinds |   5,906.6 μs |    148.35 μs |    437.41 μs |   5,704.4 μs |   1.10 |    0.14 |  5046.8750 |  77343.75 KB |        1.00 |
|                 NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  10,023.2 μs |    265.76 μs |    771.03 μs |   9,604.4 μs |   1.86 |    0.24 |  6015.6250 |  92187.51 KB |        1.19 |
|             NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |   9,991.7 μs |    279.32 μs |    823.59 μs |   9,555.4 μs |   1.85 |    0.22 |  6015.6250 |  92187.51 KB |        1.19 |
|          NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  14,051.6 μs |    344.87 μs |  1,016.86 μs |  13,672.6 μs |   2.61 |    0.30 |  7593.7500 | 116406.26 KB |        1.51 |
|                   NonAsyncBinds_cancellableTask | NonAsyncBinds |  10,324.6 μs |    285.78 μs |    842.62 μs |   9,813.6 μs |   1.92 |    0.23 |  6062.5000 |  92968.76 KB |        1.20 |
