# 0011. Set FSharp.Core per target framework with central package management

- Status: accepted
- Date: 2025-03-29
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/32, https://github.com/TheAngryByrd/IcedTasks/pull/51, https://github.com/TheAngryByrd/IcedTasks/pull/53, https://github.com/TheAngryByrd/IcedTasks/commit/131da45
- Reason source: PR description, and confirmed by maintainer on 2026-10-26 for the floor and the split

The library needs a low FSharp.Core version for most targets and FSharp.Core 9 for `net9.0`.
Paket did not give enough control over dependencies by MSBuild condition (#51).

We removed Paket and use NuGet central package management (`Directory.Packages.props`) (#51).
`src/IcedTasks/IcedTasks.fsproj` uses a `Choose` block that sets FSharp.Core for TFMs compatible with `net9.0` and for the other TFMs (#53).
Today these versions are `9.0.300` (commit 131da45) and `6.0.4` (#53).
`Directory.Build.props` sets `LangVersion` 8.0, and `LangVersion` 9.0 with `Nullable` enabled for `net9.0`.

#51 says "Has to remove paket because we need better control over dependencies based on msbuild conditions."
F# 6 added the `task` computation expression, so #32 lowered the floor from `>= 7.0.0` to `>= 6.0.1` to allow more consumers.
`net9.0` uses FSharp.Core 9 because F# 9 nullness needs the FSharp.Core 9 nullness annotations.
No source records why the other TFMs use `LangVersion` 8.0.
