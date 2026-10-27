# 0009. Put the `task` and `async` polyfills in opt-in namespaces

- Status: accepted
- Date: 2023-11-07
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/34, https://github.com/TheAngryByrd/IcedTasks/pull/35
- Reason source: commit message, PR description

IcedTasks has replacements for the FSharp.Core `task`, `backgroundTask`, and `async` builders.
An auto-opened namespace with these replacements shadows the FSharp.Core builders in each file that opens `IcedTasks`.

We put the polyfills in `IcedTasks.Polyfill.Task` (`Task.fs`) and `IcedTasks.Polyfill.Async` (`AsyncEx.fs`).
`src/IcedTasks/AutoOpens.fs` does not auto-open either namespace, so a consumer must open it.

The #35 commit says "Put Polyfill into specific namespace to opt in precisely".
#34 says that the namespace is "for fixing things in FSharp.Core that haven't made it upstream such as #31/https://github.com/dotnet/fsharp/issues/16189."
`docsSrc/Explanations/Polyfill-namespaces-and-shadowing.md` explains how to use the namespaces.
