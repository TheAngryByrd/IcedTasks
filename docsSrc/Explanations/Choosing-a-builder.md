---
title: Choosing a builder
category: Explanations
categoryindex: 3
index: 1
---

# Choosing a builder

IcedTasks provides several computation expression builders because there is not one async shape that is best for every case.

The first question is not "which one is fastest?" The first question is what kind of async operation you are trying to represent: work that starts now, work that may complete synchronously, work that should not start until the caller runs it, work that must carry cancellation, or F# `Async` work that should keep `Async` semantics.

Related guides:

- [Use the cancellable task family for request cancellation](../How-To-Guides/Use-cancellable-task-family-for-request-cancellation.html)
- [Use `and!` with independent operations](../How-To-Guides/Use-and-bang-with-independent-operations.html)
- [Understanding `and!`](Understanding-and-bang.html)
- [Convert between async shapes](../How-To-Guides/Convert-between-async-shapes.html)
- [Use background builders to avoid caller context](../How-To-Guides/Use-background-builders-to-avoid-caller-context.html)
- [Use unit builders for non-generic task APIs](../How-To-Guides/Use-unit-builders-for-non-generic-task-apis.html)
- [Pooling builders](Pooling-builders.html)
- [Polyfill namespaces and shadowing](Polyfill-namespaces-and-shadowing.html)

## Builder comparison

| Builder | Result shape | TFM | Hot/Cold<sup>1</sup> | Multiple awaits<sup>2</sup> | Multi-start<sup>3</sup> | CancellationToken propagation<sup>4</sup> | Cancellation checks<sup>5</sup> | `and!` support<sup>6</sup> | `IAsyncDisposable`<sup>7</sup> | `IAsyncEnumerable`<sup>8</sup> |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `asyncEx` | `Async<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | No | Yes | Yes |
| `IcedTasks.Polyfill.Async.async` | `Async<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | No | Yes | Yes |
| `parallelAsync` | `Async<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | Yes | No | No |
| `parallelAsyncUsingStartChild` | `Async<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | Yes | No | No |
| `parallelAsyncUsingStartImmediateAsTask` | `Async<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | Yes | No | No |
| `task` | `Task<'T>` | netstandard2.0 | Hot | Multiple | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `backgroundTask` | `Task<'T>` | netstandard2.0 | Hot | Multiple | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `taskUnit` | `Task` | netstandard2.0 | Hot | Multiple | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `backgroundTaskUnit` | `Task` | netstandard2.0 | Hot | Multiple | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `valueTask` | `ValueTask<'T>` | netstandard2.0 | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `vTask` | `ValueTask<'T>` | netstandard2.0 | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `valueTaskUnit` | `ValueTask` | netstandard2.0 | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `vTaskUnit` | `ValueTask` | netstandard2.0 | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `poolingValueTask` | `ValueTask<'T>` | net6.0+ | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `pvTask` | `ValueTask<'T>` | net6.0+ | Hot | Once | Once-start | Explicit | Explicit | Yes | Yes | Yes |
| `coldTask` | `unit -> Task<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Explicit | Explicit | Yes | Yes | No |
| `backgroundColdTask` | `unit -> Task<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Explicit | Explicit | Yes | Yes | No |
| `cancellableTask` | `CancellationToken -> Task<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | Yes | Yes | Yes |
| `backgroundCancellableTask` | `CancellationToken -> Task<'T>` | netstandard2.0 | Cold | Multiple | Multiple | Implicit | Implicit | Yes | Yes | Yes |
| `cancellableValueTask` | `CancellationToken -> ValueTask<'T>` | netstandard2.0 | Cold | Once per start | Multiple | Implicit | Implicit | Yes | Yes | Yes |
| `cancellablePoolingValueTask` | `CancellationToken -> ValueTask<'T>` | net6.0+ | Cold | Once per start | Multiple | Implicit | Implicit | Yes | Yes | Yes |
| `cancelablePVTask` | `CancellationToken -> ValueTask<'T>` | net6.0+ | Cold | Once per start | Multiple | Implicit | Implicit | Yes | Yes | Yes |

- <sup>1</sup> Hot means the operation has started, or is represented by a started task-like value, when the computation is created. Cold means caller code must start it explicitly.
- <sup>2</sup> `ValueTask` and `ValueTask<'T>` should generally be awaited once. Cancellable ValueTask builders are multi-start because the outer value is a function, but each invocation returns a ValueTask that should generally be awaited once.
- <sup>3</sup> Multi-start means the same value can be started again. Task and ValueTask builders produce once-start task-like values; cold and cancellable builders produce functions that can be invoked again.
- <sup>4</sup> Implicit means the builder carries the ambient token through the computation. Explicit means the operation must receive or pass tokens through normal API calls.
- <sup>5</sup> Implicit cancellation checks are performed by the builder before binds and runs. Explicit cancellation checks are the caller's responsibility.
- <sup>6</sup> `and!` support means the builder implements applicative composition. For `parallelAsync`, this is the main reason to use the builder.
- <sup>7</sup> `IAsyncDisposable` support means `use` and `use!` can dispose async resources inside the computation expression.
- <sup>8</sup> `IAsyncEnumerable` support means `for` can iterate an async enumerable inside the computation expression.

`IAsyncDisposable` and `IAsyncEnumerable` support is not limited to `asyncEx`. The task, value-task, cancellable, and pooling builders support these features where the table says "Yes". `coldTask` supports `IAsyncDisposable`, but not `for` over `IAsyncEnumerable<'T>`.

## Use task when the work is actually async

Use `task` when the operation is naturally task-based and you know the work is actually asynchronous.

This is the closest shape to normal .NET `Task<'T>` code. A task is hot: once the computation is created, it represents work that has started or is ready to complete. It is a good default when you are calling APIs that already return `Task` and you do not need the laziness or implicit cancellation behavior of the cancellable builders.

Use `taskUnit` when the operation returns a non-generic `Task` instead of `Task<'T>`.

## Use valueTask when completion might be synchronous

Use `valueTask` when you do not know whether the work will complete synchronously or asynchronously and you want to optimize the synchronous completion case.

`ValueTask<'T>` can avoid allocating a `Task<'T>` when the result is already available. This makes it useful for APIs where synchronous completion is common but asynchronous completion is still possible.

`ValueTask<'T>` has stricter consumption rules than `Task<'T>`. In general, treat a `ValueTask<'T>` as a value that should be awaited once. If you need repeated awaits or broad compatibility with APIs that expect `Task<'T>`, prefer `task` or convert intentionally.

Use `valueTaskUnit` when the operation returns a non-generic `ValueTask` instead of `ValueTask<'T>`.

## Use cancellableTask or cancellableValueTask when cancellation is part of the operation

Use `cancellableTask` when the caller must provide a `CancellationToken` and the result should be a `Task<'T>`.

Use `cancellableValueTask` when the same cancellation model should return `ValueTask<'T>`, usually because the operation might complete synchronously.

These builders represent functions:

```fsharp
CancellationToken -> Task<'T>
CancellationToken -> ValueTask<'T>
```

That means the operation is cold until the caller supplies a token. The token can then flow through nested cancellable work, and the builder checks cancellation before binds and runs.

Use these builders when cancellation is a real part of the API contract. If the caller should not have to provide a token, use `task`, `valueTask`, `coldTask`, or `asyncEx` instead.

## Use coldTask when you want cold task-shaped work without cancellation

Use `coldTask` when you want to represent a cold async operation that does not start until the caller invokes it, and that can be invoked more than once.

`ColdTask<'T>` is an alias for:

```fsharp
unit -> Task<'T>
```

This gives you task-shaped work with explicit start behavior. It is useful when you want laziness and repeatability, but do not need cancellation to flow through the computation.

If cancellation matters, prefer `cancellableTask` or `cancellableValueTask`.

## Use asyncEx when you want F# Async semantics with more interop

Use `asyncEx` when you like F# `Async` properties and want additional interop features.

`asyncEx` is useful when you want cold, repeatable `Async`-style work, but also want to:

- bind to `Task`, `ValueTask`, or other awaitables easily
- use `IAsyncDisposable`
- iterate over `IAsyncEnumerable<'T>`
- preserve the task exception behavior described in this project's `AsyncEx` docs

Use `asyncEx` when the operation conceptually belongs in the F# `Async` world but needs smoother interop with modern .NET async APIs.

## Use parallelAsync for Async work that should start together

Use `parallelAsync` when you have multiple `Async<'T>` operations and want to run them in parallel with applicative `and!` syntax.

For example:

```fsharp
let combined =
    parallelAsync {
        let! first = loadFirst ()
        and! second = loadSecond ()
        return first, second
    }
```

Use `parallelAsync` for parallel composition of `Async` values. If you are composing task-like or cancellable task-like values, use the builder for that task shape and check that its `and!` behavior matches the operation you want. See [Understanding `and!`](Understanding-and-bang.html) for the builder-by-builder behavior.

## Use pooling variants only after the basic choice is clear

`poolingValueTask` and `cancellablePoolingValueTask` are variants for allocation-sensitive .NET 6 or later code.

Consider them after you have already decided that `valueTask` or `cancellableValueTask` is the right shape. Otherwise, start with `valueTask` or `cancellableValueTask`; they are easier defaults and communicate the same broad API shape. See [Pooling builders](Pooling-builders.html) for details.

## Background builders are about where work runs

The `background*` builders are variants that switch execution to the thread pool when needed.

They are similar in intent to `ConfigureAwait(false)`: use them when you do not want the continuation to stay tied to the current synchronization context or scheduler.

They do not change the core task shape decision. First choose the shape:

- `task`
- `taskUnit`
- `coldTask`
- `cancellableTask`

Then use the matching background builder only when you need that operation to escape the current synchronization context or scheduler behavior.

See [Use background builders to avoid caller context](../How-To-Guides/Use-background-builders-to-avoid-caller-context.html) for a fuller explanation of UI threads, `SynchronizationContext`, and why this is less commonly needed in ASP.NET Core.

## A short decision path

If you know the work is actually asynchronous and task-shaped, use `task`.

If the work may complete synchronously and you want to optimize that case, use `valueTask`.

If cancellation is part of the contract, use `cancellableTask` or `cancellableValueTask`.

If you want a cold task-shaped operation without cancellation, use `coldTask`.

If you want F# `Async` semantics with better interop, use `asyncEx`.

If you want multiple `Async` operations to run in parallel with `and!`, use `parallelAsync`.

If you are considering a pooling builder, measure first.
