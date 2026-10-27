# 0010. Depend on Microsoft.Bcl.AsyncInterfaces on netstandard2.0

- Status: accepted
- Date: 2024-01-31
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/39, https://github.com/TheAngryByrd/IcedTasks/commit/4f4c0e0
- Reason source: commit message

netstandard2.0 does not contain `IAsyncDisposable`, `IAsyncEnumerable`, `IAsyncEnumerator`, or `ValueTask`.

We reference `Microsoft.Bcl.AsyncInterfaces` only for `netstandard2.0` in `src/IcedTasks/IcedTasks.fsproj`.
`Directory.Packages.props` pins its version.

The 0.11.0 release commit says "This allows for IAsyncDisposable, IAsyncEnumerable, and IAsyncEnumerator, and ValueTask to be used in netstandard2.0."

## Consequences

Each netstandard2.0 consumer gets one more package dependency.
