# 0014. Put each builder module in the IcedTasks namespace, with no assembly-level AutoOpen

- Status: accepted
- Date: 2026-11-18
- Supersedes: [0012](0012-namespace-per-builder-file.md)
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/78
- Reason source: maintainer decision, measurements in the PR description

[0012](0012-namespace-per-builder-file.md) gave each builder file its own namespace and auto-opened all of them with assembly-level `AutoOpen` attributes in `src/IcedTasks/AutoOpens.fs`.
0.11.10-beta001, beta002, and beta003 shipped that layout.

An assembly-level `AutoOpen` opens its namespace in each file of each project that references the assembly.
A file with no `open IcedTasks` then gets the IcedTasks extension members.
The `ColdTask` overloads on the FSharp.Core `task` builder make code such as `task { do! f a b }` fail with FS0041 when `f` has no type annotation.
The layout also changed the full name of each public type outside `IcedTasks.Polyfill`, so code compiled against 0.11.9 fails at run time with `TypeLoadException` when it uses one of these types.

We put each builder module back in `namespace IcedTasks` as an `[<AutoOpen>] module`, with the 0.11.9 names, and we deleted `AutoOpens.fs`:

```fsharp
// src/IcedTasks/ColdTask.fs
namespace IcedTasks

[<AutoOpen>]
module ColdTasks =
```

- A project reference alone opens no IcedTasks namespace.
- `open IcedTasks` brings all builders except the polyfills ([0009](0009-opt-in-polyfill-namespaces.md)).
- The 0.11.9 module opens, for example `open IcedTasks.ColdTasks`, still work.
- The public metadata is the same as 0.11.9 on each target framework: type names, kinds, base types, member signatures with return types and generic parameters, and the `AutoOpen`, `RequireQualifiedAccess`, `CompilationRepresentation`, and `Extension` attributes. `tests/IcedTasks.Tests/PublicApiTests.fs` compares it with `tests/IcedTasks.Tests/PublicApi/IcedTasks-0.11.9-<tfm>.txt`.

## Consequences

Graph-based type checking links each file that declares `namespace IcedTasks` to each earlier file that has a type or an `[<AutoOpen>]` module in that namespace (`TrieMapping.fs` and `DependencyResolution.fs` in dotnet/fsharp).
So the library files check in series again:

| Layout | Graph depth (net9.0) | Typecheck median, net9.0 | Typecheck median, netstandard2.0 |
|---|---|---|---|
| 0012 (beta002) | 5 | 1.38 s | 1.31 s |
| 0014 | 15 | 3.07 s | 2.21 s |

The Typecheck phase is about 0.9 to 1.7 s longer for each target framework.
The full project build includes the F# analyzers, and its time difference is below the run-to-run noise (medians 34.0 s and 34.3 s, single runs from 24 s to 48 s).

A library file in `namespace IcedTasks` sees the types and `[<AutoOpen>]` modules of earlier library files without an `open`.
Explicit module opens, for example `open IcedTasks.TaskBase`, cannot remove a graph link, because the namespace declaration makes the link.

## Considered options

- A facade file that re-exposes the 0012 namespaces with `[<AutoOpen>]` modules. A module and a namespace with the same full name give FS0247, so the implementation namespaces need new names. The facade needs about 450 forwarders, and it does not restore the 0.11.9 metadata names.
- `[<assembly: AutoOpen>]` in a separate opt-in assembly. The compiler resolves the path in the assembly that holds the attribute, so it cannot open a namespace of `IcedTasks.dll` (FS0970).
- Signature files for all library files. The signatures that `--allsigs` makes do not compile (92 errors in the SRTP and resumable code members).
- Selective signature files. A measurement with `CancellableValueTask.fsi` and `CancellablePoolingValueTask.fsi` adjacent to their implementation files reduced the net9.0 Typecheck median from 4.35 s to 2.73 s in a compiler-only build. The generated signatures for these two files compiled without edits. This is the first option to use if the library build time becomes a problem. Each signature must expose the full public contract of its file, and it needs maintenance when the API changes.
