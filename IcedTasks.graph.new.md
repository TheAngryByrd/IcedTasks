```mermaid
flowchart RL
    0["obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.fs"]
    1["AssemblyInfo.fs"]
    2["TaskLike.fs"]
    3["TaskBuilderBase.fs"]
    4["ValueTask.fs"]
    5["PoolingValueTask.fs"]
    6["ValueTaskUnit.fs"]
    7["TaskUnit.fs"]
    8["Task.fs"]
    9["AsyncEx.fs"]
    10["ParallelAsync.fs"]
    11["ColdTask.fs"]
    12["CancellableTaskBuilderBase.fs"]
    13["CancellableValueTask.fs"]
    14["CancellablePoolingValueTask.fs"]
    15["CancellableTask.fs"]
    16["Opens.fs"]
    3 --> 2
    4 --> 2
    4 --> 3
    5 --> 2
    5 --> 3
    6 --> 2
    6 --> 3
    7 --> 2
    7 --> 3
    8 --> 2
    8 --> 3
    9 --> 2
    9 --> 3
    9 --> 1
    10 --> 2
    10 --> 3
    10 --> 9
    10 --> 1
    11 --> 2
    11 --> 3
    11 --> 9
    11 --> 10
    12 --> 2
    12 --> 3
    12 --> 9
    12 --> 10
    13 --> 2
    13 --> 3
    13 --> 4
    13 --> 9
    13 --> 10
    13 --> 12
    14 --> 2
    14 --> 3
    14 --> 4
    14 --> 5
    14 --> 9
    14 --> 10
    14 --> 12
    15 --> 2
    15 --> 3
    15 --> 9
    15 --> 10
    15 --> 12
    16 --> 2
    16 --> 3
    16 --> 9
    16 --> 10
```
