``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22000.1335/21H2)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|                                                      Method |    Categories |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |       Gen0 |   Allocated | Alloc Ratio |
|------------------------------------------------------------ |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----------:|------------:|------------:|
|                                      AsyncBinds_CSharpTasks |    AsyncBinds |   3,385.0 μs |     65.91 μs |     90.22 μs |   3,361.8 μs |  1.00 |    0.00 |     3.9063 |    112015 B |        1.00 |
|                                              AsyncBinds_ply |    AsyncBinds |   3,898.9 μs |     21.31 μs |     19.93 μs |   3,899.0 μs |  1.13 |    0.03 |    39.0625 |    672110 B |        6.00 |
|                                            AsyncBinds_async |    AsyncBinds |  56,078.1 μs |  1,094.30 μs |  1,123.77 μs |  56,059.3 μs | 16.37 |    0.45 |   444.4444 |   8224085 B |       73.42 |
|                                             AsyncBinds_task |    AsyncBinds |   3,428.8 μs |     37.05 μs |     34.65 μs |   3,431.5 μs |  1.00 |    0.03 |     7.8125 |    128009 B |        1.14 |
|                                        AsyncBinds_valueTask |    AsyncBinds |   3,638.3 μs |     25.87 μs |     24.20 μs |   3,642.3 μs |  1.06 |    0.03 |     7.8125 |    136012 B |        1.21 |
|                                AsyncBinds_coldTask_bindTask |    AsyncBinds |   3,471.3 μs |     25.18 μs |     23.55 μs |   3,472.1 μs |  1.01 |    0.03 |    23.4375 |    424016 B |        3.79 |
|                            AsyncBinds_coldTask_bindColdTask |    AsyncBinds |   3,604.3 μs |     21.80 μs |     20.40 μs |   3,600.0 μs |  1.05 |    0.02 |    11.7188 |    184011 B |        1.64 |
|                         AsyncBinds_cancellableTask_bindTask |    AsyncBinds |   3,713.3 μs |     32.59 μs |     30.48 μs |   3,706.2 μs |  1.08 |    0.03 |    27.3438 |    440011 B |        3.93 |
|                     AsyncBinds_cancellableTask_bindColdTask |    AsyncBinds |   3,485.8 μs |     37.14 μs |     32.92 μs |   3,483.7 μs |  1.01 |    0.02 |     7.8125 |    200016 B |        1.79 |
|              AsyncBinds_cancellableTask_bindCancellableTask |    AsyncBinds |   3,530.2 μs |     34.36 μs |     32.14 μs |   3,525.6 μs |  1.03 |    0.03 |    11.7188 |    200011 B |        1.79 |
|                    AsyncBinds_cancellableValueTask_bindTask |    AsyncBinds |   3,593.4 μs |     15.48 μs |     14.48 μs |   3,591.0 μs |  1.05 |    0.02 |    27.3438 |    456010 B |        4.07 |
|                AsyncBinds_cancellableValueTask_bindColdTask |    AsyncBinds |   3,588.8 μs |     23.80 μs |     21.09 μs |   3,582.0 μs |  1.04 |    0.02 |    11.7188 |    216014 B |        1.93 |
|         AsyncBinds_cancellableValueTask_bindCancellableTask |    AsyncBinds |   3,590.1 μs |     40.27 μs |     37.67 μs |   3,602.7 μs |  1.04 |    0.03 |    11.7188 |    216011 B |        1.93 |
|                                                             |               |              |              |              |              |       |         |            |             |             |
|                                   ManyWriteFile_CSharpTasks | ManyWriteFile |     479.7 μs |      9.10 μs |     12.15 μs |     478.9 μs |  1.00 |    0.00 |          - |      8248 B |        1.00 |
|                                           ManyWriteFile_ply | ManyWriteFile |     737.1 μs |     41.42 μs |    119.50 μs |     707.7 μs |  1.64 |    0.33 |          - |     10132 B |        1.23 |
|                                         ManyWriteFile_async | ManyWriteFile |     720.4 μs |     17.28 μs |     49.86 μs |     717.8 μs |  1.55 |    0.11 |    18.5547 |    298849 B |       36.23 |
|                                          ManyWriteFile_task | ManyWriteFile |     557.8 μs |     15.99 μs |     46.64 μs |     559.8 μs |  1.16 |    0.09 |          - |      8411 B |        1.02 |
|                                     ManyWriteFile_valueTask | ManyWriteFile |     546.6 μs |     16.96 μs |     48.95 μs |     543.6 μs |  1.15 |    0.10 |          - |      8422 B |        1.02 |
|                                      ManyWriteFile_coldTask | ManyWriteFile |     560.0 μs |     16.84 μs |     48.86 μs |     545.7 μs |  1.16 |    0.10 |     1.9531 |     32531 B |        3.94 |
|                               ManyWriteFile_cancellableTask | ManyWriteFile |     562.6 μs |     17.71 μs |     51.09 μs |     553.2 μs |  1.15 |    0.12 |     1.9531 |     32557 B |        3.95 |
|              ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |     593.1 μs |     24.93 μs |     71.93 μs |     580.7 μs |  1.19 |    0.09 |     1.9531 |     32731 B |        3.97 |
|             ManyWriteFile_cancellableTask_withCancellation2 | ManyWriteFile |     558.4 μs |     32.15 μs |     90.67 μs |     537.6 μs |  1.09 |    0.12 |     0.4883 |      8584 B |        1.04 |
|             ManyWriteFile_cancellableTask_withCancellation3 | ManyWriteFile |     635.2 μs |     18.46 μs |     54.42 μs |     624.4 μs |  1.33 |    0.12 |    10.7422 |    176587 B |       21.41 |
|                          ManyWriteFile_cancellableValueTask | ManyWriteFile |     560.4 μs |     21.41 μs |     62.45 μs |     560.4 μs |  1.21 |    0.11 |     1.9531 |     32572 B |        3.95 |
|         ManyWriteFile_cancellableValueTask_withCancellation | ManyWriteFile |     547.6 μs |     20.47 μs |     60.03 μs |     541.4 μs |  1.21 |    0.12 |     1.9531 |     32717 B |        3.97 |
|        ManyWriteFile_cancellableValueTask_withCancellation2 | ManyWriteFile |     695.4 μs |     66.01 μs |    194.64 μs |     665.0 μs |  1.20 |    0.37 |     0.4883 |      8603 B |        1.04 |
|        ManyWriteFile_cancellableValueTask_withCancellation3 | ManyWriteFile |     652.9 μs |     23.95 μs |     69.11 μs |     628.7 μs |  1.37 |    0.12 |     5.8594 |    112658 B |       13.66 |
|                                                             |               |              |              |              |              |       |         |            |             |             |
|                                   NonAsyncBinds_CSharpTasks | NonAsyncBinds |   8,140.8 μs |    565.08 μs |  1,666.16 μs |   8,457.5 μs |  1.00 |    0.00 |  5046.8750 |  79200004 B |       1.000 |
|                                           NonAsyncBinds_ply | NonAsyncBinds |  10,471.5 μs |    638.78 μs |  1,883.46 μs |  10,983.8 μs |  1.35 |    0.39 |  5046.8750 |  79200004 B |       1.000 |
|                                         NonAsyncBinds_async | NonAsyncBinds | 598,409.9 μs | 11,794.73 μs | 30,021.34 μs | 588,853.6 μs | 76.61 |   20.45 | 16000.0000 | 251200656 B |       3.172 |
|                                          NonAsyncBinds_task | NonAsyncBinds |   8,572.2 μs |    799.47 μs |  2,357.27 μs |   8,957.5 μs |  1.10 |    0.41 |  5046.8750 |  79200004 B |       1.000 |
|                            NonAsyncBinds_task_bindValueTask | NonAsyncBinds |   6,691.3 μs |     74.58 μs |     58.23 μs |   6,715.6 μs |  0.84 |    0.22 |   453.1250 |   7200004 B |       0.091 |
|                                     NonAsyncBinds_valueTask | NonAsyncBinds |   6,313.3 μs |    108.69 μs |    101.66 μs |   6,298.5 μs |  0.79 |    0.19 |          - |         4 B |       0.000 |
|                              tenBindSync_valueTask_bindTask | NonAsyncBinds |   8,236.7 μs |    657.82 μs |  1,939.59 μs |   8,622.7 μs |  1.07 |    0.39 |  4585.9375 |  72000004 B |       0.909 |
|                             NonAsyncBinds_coldTask_bindTask | NonAsyncBinds |  20,548.4 μs |  1,369.49 μs |  4,037.96 μs |  21,832.5 μs |  2.65 |    0.82 |  7546.8750 | 118400008 B |       1.495 |
|                        NonAsyncBinds_coldTask_bindValueTask | NonAsyncBinds |  22,335.9 μs |  1,013.72 μs |  2,988.98 μs |  23,480.6 μs |  2.88 |    0.81 |  3968.7500 |  62400015 B |       0.788 |
|                         NonAsyncBinds_coldTask_bindColdTask | NonAsyncBinds |  16,665.4 μs |  1,234.63 μs |  3,640.34 μs |  18,076.6 μs |  2.15 |    0.72 |  6015.6250 |  94400008 B |       1.192 |
|                    NonAsyncBinds_coldTask_bindColdValueTask | NonAsyncBinds |  15,643.5 μs |  1,163.78 μs |  3,431.44 μs |  15,919.5 μs |  2.03 |    0.73 |  6015.6250 |  94400008 B |       1.192 |
|                      NonAsyncBinds_cancellableTask_bindTask | NonAsyncBinds |  21,686.1 μs |  1,650.04 μs |  4,865.19 μs |  23,892.2 μs |  2.83 |    1.05 |  7593.7500 | 119200015 B |       1.505 |
|                 NonAsyncBinds_cancellableTask_bindValueTask | NonAsyncBinds |  21,971.4 μs |  1,095.62 μs |  3,230.46 μs |  23,484.7 μs |  2.84 |    0.82 |  4000.0000 |  63200015 B |       0.798 |
|                  NonAsyncBinds_cancellableTask_bindColdTask | NonAsyncBinds |  12,565.2 μs |    903.07 μs |  2,662.72 μs |  11,672.1 μs |  1.63 |    0.54 |  6062.5000 |  95200008 B |       1.202 |
|             NonAsyncBinds_cancellableTask_bindColdValueTask | NonAsyncBinds |  11,256.3 μs |    218.53 μs |    233.82 μs |  11,133.1 μs |  1.38 |    0.32 |  1984.3750 |  31200008 B |       0.394 |
|           NonAsyncBinds_cancellableTask_bindCancellableTask | NonAsyncBinds |  15,703.0 μs |  1,108.91 μs |  3,269.64 μs |  16,219.9 μs |  2.04 |    0.72 |  6062.5000 |  95200008 B |       1.202 |
|      NonAsyncBinds_cancellableTask_bindCancellableValueTask | NonAsyncBinds |  14,496.7 μs |    822.77 μs |  2,425.97 μs |  15,816.5 μs |  1.88 |    0.59 |  1984.3750 |  31200008 B |       0.394 |
|                 NonAsyncBinds_cancellableValueTask_bindTask | NonAsyncBinds |  22,346.1 μs |  1,434.08 μs |  4,228.43 μs |  23,328.3 μs |  2.90 |    0.97 |  7187.5000 | 112800008 B |       1.424 |
|            NonAsyncBinds_cancellableValueTask_bindValueTask | NonAsyncBinds |  22,763.6 μs |    942.17 μs |  2,778.01 μs |  23,546.9 μs |  2.94 |    0.81 |  3593.7500 |  56800015 B |       0.717 |
|             NonAsyncBinds_cancellableValueTask_bindColdTask | NonAsyncBinds |  14,966.2 μs |  1,197.24 μs |  3,530.08 μs |  15,480.6 μs |  1.92 |    0.61 |  5656.2500 |  88800008 B |       1.121 |
|        NonAsyncBinds_cancellableValueTask_bindColdValueTask | NonAsyncBinds |  14,462.5 μs |    764.38 μs |  2,253.80 μs |  15,417.9 μs |  1.88 |    0.61 |  1578.1250 |  24800008 B |       0.313 |
|      NonAsyncBinds_cancellableValueTask_bindCancellableTask | NonAsyncBinds |  16,601.6 μs |    966.09 μs |  2,848.53 μs |  17,177.9 μs |  2.13 |    0.58 |  5656.2500 |  88800008 B |       1.121 |
| NonAsyncBinds_cancellableValueTask_bindCancellableValueTask | NonAsyncBinds |  14,322.8 μs |    781.12 μs |  2,303.14 μs |  14,876.6 μs |  1.85 |    0.57 |  1578.1250 |  24800008 B |       0.313 |
