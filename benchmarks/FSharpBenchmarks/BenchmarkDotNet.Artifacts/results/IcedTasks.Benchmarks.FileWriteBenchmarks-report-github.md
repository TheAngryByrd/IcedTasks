```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 11 (10.0.26100.6899)
12th Gen Intel Core i9-12900F, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX2


```
| Method                                                                      | Categories                                       | bufferSize | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------------------------------------- |------------------------------------------------- |----------- |-----------:|---------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| CSharp_ManyWriteFile_TaskBuilder                                            | ManyWriteFile,CSharp,TaskBuilder                 | 128        |   753.0 ns | 27.90 ns |  81.83 ns |   728.7 ns |  1.00 |    0.00 |      - |       8 B |        1.00 |
| CSharp_ManyWriteFile_ValueTaskBuilder                                       | ManyWriteFile,CSharp,ValueTaskBuilder            | 128        |   745.9 ns | 20.90 ns |  60.30 ns |   726.9 ns |  1.00 |    0.12 |      - |       8 B |        1.00 |
| FSharp_ManyWriteFile_AsyncBuilder_BindAsync                                 | ManyWriteFile,FSharp,AsyncBuilder,BindAsync      | 128        | 1,914.9 ns | 64.37 ns | 187.76 ns | 1,894.3 ns |  2.57 |    0.40 | 0.0703 |    1155 B |      144.38 |
| FSharp_ManyWriteFile_AsyncBuilder_BindAsync_bindTask                        | ManyWriteFile,FSharp,AsyncBuilder,BindTask       | 128        | 1,633.3 ns | 45.71 ns | 131.88 ns | 1,628.0 ns |  2.19 |    0.29 | 0.0176 |     298 B |       37.25 |
| FSharp_ManyWriteFile_CancellableTaskBuilder                                 | ManyWriteFile,FSharp,CancellableTaskBuilder      | 128        | 1,575.0 ns | 41.90 ns | 122.87 ns | 1,554.3 ns |  2.12 |    0.29 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenOnce        | ManyWriteFile,FSharp,CancellableTaskBuilder      | 128        | 1,631.2 ns | 32.45 ns |  92.04 ns | 1,632.2 ns |  2.19 |    0.26 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenMany        | ManyWriteFile,FSharp,CancellableTaskBuilder      | 128        | 1,646.6 ns | 34.72 ns | 100.18 ns | 1,655.9 ns |  2.21 |    0.27 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableTaskBuilder_getCancellationTokenLambda      | ManyWriteFile,FSharp,CancellableTaskBuilder      | 128        | 1,670.1 ns | 33.35 ns |  93.51 ns | 1,669.1 ns |  2.24 |    0.28 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableValueTaskBuilder                            | ManyWriteFile,FSharp,CancellableValueTaskBuilder | 128        | 1,629.6 ns | 38.73 ns | 112.97 ns | 1,620.4 ns |  2.19 |    0.25 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenOnce   | ManyWriteFile,FSharp,CancellableValueTaskBuilder | 128        | 1,407.5 ns | 61.09 ns | 178.21 ns | 1,343.7 ns |  1.89 |    0.32 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenMany   | ManyWriteFile,FSharp,CancellableValueTaskBuilder | 128        | 1,368.4 ns | 55.58 ns | 161.25 ns | 1,323.9 ns |  1.83 |    0.27 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_CancellableValueTaskBuilder_getCancellationTokenLambda | ManyWriteFile,FSharp,CancellableValueTaskBuilder | 128        | 1,733.7 ns | 45.76 ns | 132.75 ns | 1,726.8 ns |  2.33 |    0.30 |      - |       9 B |        1.12 |
| FSharp_ManyWriteFile_ply                                                    | ManyWriteFile,FSharp,PlyTaskBuilder              | 128        | 1,772.6 ns | 36.98 ns | 107.88 ns | 1,775.1 ns |  2.38 |    0.29 |      - |      10 B |        1.25 |
| FSharp_ManyWriteFile_plyValueTask                                           | ManyWriteFile,FSharp,PlyTaskBuilder              | 128        | 1,736.1 ns | 47.65 ns | 136.73 ns | 1,737.1 ns |  2.33 |    0.31 |      - |      10 B |        1.25 |
| FSharp_ManyWriteFile_TaskBuilder                                            | ManyWriteFile,FSharp,TaskBuilder                 | 128        | 1,706.6 ns | 43.56 ns | 126.38 ns | 1,708.1 ns |  2.30 |    0.32 |      - |       8 B |        1.00 |
| FSharp_ManyWriteFile_TaskBuilderRuntime                                     | ManyWriteFile,FSharp,TaskBuilder                 | 128        | 1,740.9 ns | 40.95 ns | 117.51 ns | 1,735.3 ns |  2.34 |    0.29 |      - |      18 B |        2.25 |
| FSharp_ManyWriteFile_ValueTaskBuilder                                       | ManyWriteFile,FSharp,ValueTaskBuilder            | 128        | 1,647.2 ns | 40.35 ns | 118.35 ns | 1,647.8 ns |  2.21 |    0.27 |      - |       9 B |        1.12 |
