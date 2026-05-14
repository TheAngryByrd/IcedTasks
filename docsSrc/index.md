## What is IcedTasks?

IcedTasks provides F# computation expression builders for task-shaped async code:

- `task` and `backgroundTask`
- `valueTask`, `vTask`, and pooling ValueTask variants
- `coldTask`
- `cancellableTask`, `cancellableValueTask`, and cancellable pooling variants
- `asyncEx`
- `parallelAsync`

The library is for codebases that need more async shapes than FSharp.Core's built-in `task` expression provides, especially when cancellation, `ValueTask`, cold execution, applicative `and!`, or richer .NET async interop are part of the API design.

## Install

```sh
dotnet add package IcedTasks
```

Then open the namespace you need:

```fsharp
open IcedTasks
```

## Start here

If you are deciding which computation expression to use, start with [Choosing a builder](Explanations/Choosing-a-builder.html).

If you are new to the library, start with [Build a cancellable pipeline](Tutorials/Build-a-cancellable-pipeline.html).

If you need API signatures, generated reference documentation is available under [API Reference](reference/index.html).

## Common tasks

- [Use the cancellable task family for request cancellation](How-To-Guides/Use-cancellable-task-family-for-request-cancellation.html)
- [Use `and!` with independent operations](How-To-Guides/Use-and-bang-with-independent-operations.html)
- [Convert between async shapes](How-To-Guides/Convert-between-async-shapes.html)
- [Use AsyncEx for .NET async interop](How-To-Guides/Use-AsyncEx-for-Dotnet-Async-Interop.html)
- [Compose task values with helper functions](How-To-Guides/Compose-task-values-with-helper-functions.html)
- [Use background builders to avoid caller context](How-To-Guides/Use-background-builders-to-avoid-caller-context.html)
- [Use unit builders for non-generic task APIs](How-To-Guides/Use-unit-builders-for-non-generic-task-apis.html)

## Learn the concepts

- [Understanding `and!`](Explanations/Understanding-and-bang.html)
- [Pooling builders](Explanations/Pooling-builders.html)
- [Polyfill namespaces and shadowing](Explanations/Polyfill-namespaces-and-shadowing.html)
- [Why IcedTasks uses `Source` and specialized binds](Explanations/Why-is-there-different-binds.html)

## Framework examples

- [Use CancellableTask in a console app](How-To-Guides/Cancellable-Task-In-Console-App.html)
- [Use CancellableTask in ASP.NET Minimal API](How-To-Guides/Cancellable-Task-in-Minimal-Api.html)
- [Use CancellableTask in Giraffe](How-To-Guides/Cancellable-Task-In-Giraffe.html)
- [Use CancellableTask in Falco](How-To-Guides/Cancellable-Task-In-Falco.html)

## Maintainer

- [@TheAngryByrd](https://github.com/TheAngryByrd)
