``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6.2 (20G314) [Darwin 20.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                         Method |    Categories |       Mean |      Error |     StdDev | Ratio | RatioSD |      Gen 0 |  Gen 1 |  Allocated |
|----------------------------------------------- |-------------- |-----------:|-----------:|-----------:|------:|--------:|-----------:|-------:|-----------:|
|                              ManyWriteFile_ply | ManyWriteFile |   1.107 ms |  0.0215 ms |  0.0180 ms |  1.07 |    0.03 |          - |      - |      10 KB |
|                            ManyWriteFile_async | ManyWriteFile |   1.287 ms |  0.0132 ms |  0.0110 ms |  1.24 |    0.02 |    35.1563 | 9.7656 |     292 KB |
|                             ManyWriteFile_task | ManyWriteFile |   1.029 ms |  0.0185 ms |  0.0254 ms |  1.00 |    0.00 |          - |      - |       8 KB |
|                         ManyWriteFile_coldTask | ManyWriteFile |   1.032 ms |  0.0193 ms |  0.0189 ms |  1.00 |    0.03 |          - |      - |       8 KB |
|                  ManyWriteFile_cancellableTask | ManyWriteFile |   1.047 ms |  0.0162 ms |  0.0144 ms |  1.01 |    0.02 |          - |      - |       8 KB |
| ManyWriteFile_cancellableTask_withCancellation | ManyWriteFile |   1.016 ms |  0.0203 ms |  0.0180 ms |  0.98 |    0.03 |          - |      - |       9 KB |
|                                                |               |            |            |            |       |         |            |        |            |
|                              NonAsyncBinds_ply | NonAsyncBinds |  14.976 ms |  0.2448 ms |  0.2044 ms |  1.16 |    0.03 |  9468.7500 |      - |  77,344 KB |
|                            NonAsyncBinds_async | NonAsyncBinds | 871.476 ms | 16.8612 ms | 16.5599 ms | 67.40 |    1.65 | 30000.0000 |      - | 248,444 KB |
|                             NonAsyncBinds_task | NonAsyncBinds |  12.925 ms |  0.1447 ms |  0.1282 ms |  1.00 |    0.00 |  9468.7500 |      - |  77,344 KB |
|                         NonAsyncBinds_coldTask | NonAsyncBinds |  21.464 ms |  0.4169 ms |  0.3900 ms |  1.66 |    0.03 | 11281.2500 |      - |  92,188 KB |
|                  NonAsyncBinds_cancellableTask | NonAsyncBinds |  21.575 ms |  0.3903 ms |  0.3651 ms |  1.67 |    0.02 | 11375.0000 |      - |  92,969 KB |
|                                                |               |            |            |            |       |         |            |        |            |
|                                 AsyncBinds_ply |    AsyncBinds |  15.096 ms |  0.3678 ms |  1.0844 ms |  0.87 |    0.06 |    78.1250 |      - |     656 KB |
|                               AsyncBinds_async |    AsyncBinds |  78.382 ms |  0.5136 ms |  0.4289 ms |  5.11 |    0.24 |  1000.0000 |      - |   8,375 KB |
|                                AsyncBinds_task |    AsyncBinds |  17.366 ms |  0.4141 ms |  1.2144 ms |  1.00 |    0.00 |    15.6250 |      - |     188 KB |
|                            AsyncBinds_coldTask |    AsyncBinds |  15.610 ms |  0.3418 ms |  1.0023 ms |  0.91 |    0.11 |    31.2500 |      - |     305 KB |
|                     AsyncBinds_cancellableTask |    AsyncBinds |  18.537 ms |  0.3673 ms |  0.8366 ms |  1.10 |    0.11 |    31.2500 |      - |     320 KB |
