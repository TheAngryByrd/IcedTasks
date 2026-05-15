---
title: Polyfill namespaces and shadowing
category: Explanations
categoryindex: 3
index: 6
---

# Polyfill namespaces and shadowing

IcedTasks provides polyfill namespaces for builders that use familiar names:

| Namespace | Shadows | Builder you get |
|---|---|---|
| `IcedTasks.Polyfill.Task` | FSharp.Core `task` and `backgroundTask` | IcedTasks task builders |
| `IcedTasks.Polyfill.Async` | FSharp.Core `async` | `asyncEx` |

Opening one of these namespaces intentionally changes which computation expression builder is chosen for the familiar name.

```fsharp
module LocalTaskCode =
    open IcedTasks.Polyfill.Task

    let work =
        task {
            return 42
        }
```

Prefer local opens at first: open the polyfill namespace inside the module or file that intentionally wants IcedTasks behavior. Opening these namespaces broadly is also acceptable when a project has decided to standardize on the IcedTasks builders, but broad opens make shadowing less obvious to readers.

## IcedTasks task vs FSharp.Core task

FSharp.Core's [`task` expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) is the built-in default for writing `Task<'T>` code in F#.

Use the FSharp.Core builder when it already supports the code you are writing. Use `IcedTasks.Polyfill.Task.task` when you want the IcedTasks task builder behavior under the familiar `task { ... }` name.

The IcedTasks task builder is intended as a polyfill for task-builder behavior that cannot be backported everywhere FSharp.Core is used. In this project, the IcedTasks builder participates in the broader IcedTasks interop surface:

- `and!` applicative composition with supported task-like operands
- `IAsyncDisposable` support with `use` and `use!`
- `IAsyncEnumerable<'T>` support in `for` loops
- binding supported awaitables through the IcedTasks `Source`/awaiter machinery

The result shape is still `Task<'T>`, and `backgroundTask` still returns `Task<'T>`.

## Background task shadowing

`IcedTasks.Polyfill.Task` also provides `backgroundTask`.

The IcedTasks `backgroundTask` is similar in intent to `ConfigureAwait(false)`: it avoids staying tied to the caller's synchronization context or non-default scheduler when needed. See [Use background builders to avoid caller context](../How-To-Guides/Use-background-builders-to-avoid-caller-context.html) for the full explanation.

## Async shadowing

`IcedTasks.Polyfill.Async` makes `async { ... }` mean `asyncEx { ... }` in that scope.

Use this when you want F# `Async<'T>` semantics, but with AsyncEx interop features under the familiar `async` name:

- bind `Task`, `ValueTask`, and other awaitables directly
- use `IAsyncDisposable`
- iterate `IAsyncEnumerable<'T>`
- use AsyncEx task exception behavior

For most code, prefer importing `IcedTasks.AsyncEx` and writing `asyncEx { ... }` explicitly until the team has decided that shadowing `async { ... }` improves readability in that scope.

## Import strategy

Use one of these patterns:

| Pattern | When to use it |
|---|---|
| Local module open | Preferred default. Keeps shadowing visible and contained. |
| File-level open | Useful when the whole file intentionally uses IcedTasks builders. |
| Project-wide convention | Reasonable after the team agrees that IcedTasks builders are the standard in that codebase. |

The important part is consistency. If a file opens a polyfill namespace, readers should be able to tell quickly that `task { ... }`, `backgroundTask { ... }`, or `async { ... }` means the IcedTasks builder, not the FSharp.Core default.
