---
title: Pooling builders
category: Explanations
categoryindex: 3
index: 5
---

# Pooling builders

IcedTasks includes pooling-backed `ValueTask` builders for allocation-sensitive .NET 6+ code:

| Builder | Alias | Result shape |
|---|---|---|
| `poolingValueTask` | `pvTask` | `ValueTask<'T>` |
| `cancellablePoolingValueTask` | `cancelablePVTask` | `CancellationToken -> ValueTask<'T>` |

These builders use `PoolingAsyncValueTaskMethodBuilder<'T>`, described in the .NET blog post [Async ValueTask Pooling in .NET 5](https://devblogs.microsoft.com/dotnet/async-valuetask-pooling-in-net-5/). They are available only when the package is compiled for `net6.0` or later because the builder is guarded by `NET6_0_OR_GREATER` in the source.

## Why they exist

`ValueTask<'T>` is useful when an operation might complete synchronously and you want to avoid allocating a `Task<'T>` for that common path.

The pooling builders go one step further. They use a pooling method builder for the async state machine itself, which can reduce allocations in some hot paths where the operation does not complete synchronously.

That does not make them the default. They are more specialized than `valueTask` and `cancellableValueTask`.

## When to consider them

Consider a pooling builder when:

- you are targeting .NET 6 or later
- the API shape should already be `ValueTask<'T>`
- the code is allocation-sensitive
- the operation is on a hot path

Start with `valueTask` or `cancellableValueTask` when you are still choosing the API shape. Move to `poolingValueTask` or `cancellablePoolingValueTask` when the `ValueTask` shape is already right and pooling is worth considering for that workload.

## What does not change

The pooling builders keep the same broad rules as their non-pooling counterparts:

- `poolingValueTask` is hot and returns `ValueTask<'T>`.
- `cancellablePoolingValueTask` is cold until the caller supplies a `CancellationToken`.
- Each produced `ValueTask<'T>` should generally be awaited once.
- `and!`, `IAsyncDisposable`, and `IAsyncEnumerable` support follow the same compatibility expectations documented in [Choosing a builder](Choosing-a-builder.html).

## Relationship to the aliases

`pvTask` is a short alias for `poolingValueTask`.

`cancelablePVTask` is a short alias for `cancellablePoolingValueTask`.

Use the longer names in public documentation and examples when clarity matters. Use the aliases when the surrounding code already makes the builder family obvious.
