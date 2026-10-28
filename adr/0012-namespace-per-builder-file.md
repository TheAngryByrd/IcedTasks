# 0012. Give each builder file its own namespace and auto-open all of them

- Status: accepted
- Date: 2025-10-19
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/59, https://github.com/TheAngryByrd/IcedTasks/commit/2bfba41
- Reason source: PR description, commit message

Graph-based type checking checks two files in parallel when neither file depends on the other.
Commit 2bfba41 ("Dogfood parallel compilation flags") added `--test:GraphBasedChecking --test:ParallelIlxGen --test:ParallelOptimization` to `Directory.Build.props`.

We give each builder file its own namespace, for example `IcedTasks.ColdTasks` in `src/IcedTasks/ColdTask.fs`.
`src/IcedTasks/AutoOpens.fs` auto-opens the 14 namespaces with assembly-level `AutoOpen` attributes, so `open IcedTasks` still gives all builders except the polyfills ([0009](0009-opt-in-polyfill-namespaces.md)).

#59 says that it "Changes how files reference each other so make the graph for [graph based type-checking](https://devblogs.microsoft.com/dotnet/a-new-fsharp-compiler-feature-graphbased-typechecking/)".

## Considered options

A reviewer suggested one facade module in place of the many `AutoOpen` attributes.
The maintainer replied "This is intentional". No source records the reason.

## Consequences

#59 says "Oh yeah moving everything into their own namespace is a binary breaking change, but anything directly compiling won't notice."
#59 also says "_Unfortunately_ the time savings isn't much. ~1 second on the `src` project."
A new builder namespace needs a line in `AutoOpens.fs`.
