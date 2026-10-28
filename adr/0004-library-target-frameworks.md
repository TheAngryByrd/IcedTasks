# 0004. Target netstandard2.0, netstandard2.1, net6.0, and net9.0

- Status: accepted
- Date: 2022-12-01
- Source: https://github.com/TheAngryByrd/IcedTasks/issues/8, https://github.com/TheAngryByrd/IcedTasks/pull/9, https://github.com/TheAngryByrd/IcedTasks/pull/13, https://github.com/TheAngryByrd/IcedTasks/pull/27, https://github.com/TheAngryByrd/IcedTasks/pull/51
- Reason source: issue, and confirmed by maintainer on 2026-10-26 for #13 and #51

The library first targeted `net6.0`, and then `netstandard2.0;netstandard2.1`.
#9 changed it to `net7.0` only, for the FSharp.Core fix in issue #8: "This does seem fixed but requires .net 7.0 TFM and F# 7.x."

We target `netstandard2.0` and `netstandard2.1` again (#13), and add `net6.0` (#27) and `net9.0` (#51).
`src/IcedTasks/IcedTasks.fsproj` has `net9.0;net6.0;netstandard2.1;netstandard2.0`.
The pooling builders use `PoolingAsyncValueTaskMethodBuilder`, which .NET 6 added, so they compile only under `#if NET6_0_OR_GREATER`.

The netstandard targets let .NET Framework and older .NET consumers use the library.
This is the same reason as the low FSharp.Core floor in [0011](0011-fsharp-core-per-target-framework.md).
`net9.0` gets F# 9 nullness, which needs FSharp.Core 9.
