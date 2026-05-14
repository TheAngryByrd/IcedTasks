# Underdocumented Features Handoff

This file captures documentation gaps found by comparing the current docs against the public API surface and tests.

## High Priority

### Functional helper modules

Helper modules expose common functions that are mostly absent from narrative docs:

- `singleton`
- `bind`
- `map`
- `apply`
- `zip`
- `parallelZip`
- `ofUnit`
- `toUnit`
- `whenAll`
- `whenAllThrottled`
- `sequential`

These appear across `CancellableTask`, `ColdTask`, `ValueTask`, `CancellableValueTask`, and pooling variants. The docs should explain which helpers exist per task type and show small examples for serial vs parallel composition.

Sources:

- `src/IcedTasks/CancellableTask.fs`
- `src/IcedTasks/ColdTask.fs`
- `src/IcedTasks/ValueTask.fs`
- `src/IcedTasks/CancellableValueTask.fs`
- `src/IcedTasks/PoolingValueTask.fs`
- `src/IcedTasks/CancellablePoolingValueTask.fs`

### CancellableValueTask and cancellablePoolingValueTask

The README names these builders, but most examples focus on `CancellableTask`. Add dedicated docs for:

- token flow
- lazy execution
- `getCancellationToken`
- `Async` interop
- `ValueTask` return behavior
- helper functions
- pooling-specific availability and tradeoffs

Sources:

- `README.md`
- `src/IcedTasks/CancellableValueTask.fs`
- `src/IcedTasks/CancellablePoolingValueTask.fs`

### Applicative `and!` support

`parallelAsync` documents `and!`, but `MergeSources` is implemented and tested for other builders too. Add a compatibility table showing which builders support `and!`, what kinds of operands can be combined, and whether the operands are started concurrently or sequenced.

Builders/features to cover:

- `task`
- `backgroundTask`
- `taskUnit`
- `valueTask`
- `valueTaskUnit`
- `poolingValueTask`
- `coldTask`
- `cancellableTask`
- `cancellableValueTask`
- `cancellablePoolingValueTask`
- `parallelAsync`

Sources:

- `src/IcedTasks/Task.fs`
- `src/IcedTasks/TaskUnit.fs`
- `src/IcedTasks/ValueTask.fs`
- `src/IcedTasks/ValueTaskUnit.fs`
- `src/IcedTasks/PoolingValueTask.fs`
- `src/IcedTasks/ColdTask.fs`
- `src/IcedTasks/CancellableTask.fs`
- `src/IcedTasks/CancellableValueTask.fs`
- `src/IcedTasks/CancellablePoolingValueTask.fs`
- `src/IcedTasks/ParallelAsync.fs`
- `tests/IcedTasks.Tests/*Tests.fs`

### Async, Task, ValueTask, ColdTask, and CancellableTask interop

The library adds many conversion and await helpers that are currently discoverable mostly from source/XML comments. Add a guide that explains how each task shape can be awaited or converted.

Items to cover:

- `Async.AwaitValueTask`
- `Async.AsValueTask`
- `Async.AwaitColdTask`
- `Async.AsColdTask`
- `Async.AwaitCancellableTask`
- `Async.AsCancellableTask`
- `Async.AwaitCancellableValueTask`
- `Async.AsCancellableValueTask`
- `AsyncEx.AwaitTask`
- `AsyncEx.AwaitValueTask`
- `AsyncEx.AwaitAwaiter`
- `AsyncEx.AwaitAwaitable`

Sources:

- `src/IcedTasks/AsyncEx.fs`
- `src/IcedTasks/ValueTask.fs`
- `src/IcedTasks/ColdTask.fs`
- `src/IcedTasks/CancellableTaskBuilderBase.fs`
- `src/IcedTasks/CancellableValueTask.fs`
- `src/IcedTasks/CancellablePoolingValueTask.fs`

## Medium Priority

### Background builders

The docs list some background builders, but they do not explain scheduler behavior or when the builders switch to `Task.Run`.

Builders to document:

- `backgroundTask`
- `backgroundTaskUnit`
- `backgroundColdTask`
- `backgroundCancellableTask`

Recommended coverage:

- when the builder stays on the current context
- when it escapes to the thread pool
- cancellation behavior for cancellable variants
- why a user would choose it over the non-background builder

Sources:

- `src/IcedTasks/Task.fs`
- `src/IcedTasks/TaskUnit.fs`
- `src/IcedTasks/ColdTask.fs`
- `src/IcedTasks/CancellableTask.fs`

### Unit builders

The README lists unit builders but does not explain why they exist separately from the generic builders.

Builders to document:

- `taskUnit`
- `backgroundTaskUnit`
- `valueTaskUnit`
- `vTaskUnit`

Recommended coverage:

- expected return type
- how they differ from `task { return () }` and `valueTask { return () }`
- when they help type inference
- examples using `do!` and `return!`

Sources:

- `src/IcedTasks/TaskUnit.fs`
- `src/IcedTasks/ValueTaskUnit.fs`

### AsyncEx full behavior

README has good `AsyncEx` examples, but the feature should be documented as a complete interop builder.

Add coverage for:

- awaiting arbitrary awaitables
- `Task` exception behavior
- `ValueTask`
- `IAsyncEnumerable`
- `IAsyncDisposable`
- async `finally`
- `IcedTasks.AsyncEx.PolyfillBuilders.async` and how it shadows FSharp.Core `async`

Sources:

- `README.md`
- `docsSrc/index.md`
- `src/IcedTasks/AsyncEx.fs`

### Pooling builders and target framework constraints

`poolingValueTask` and `cancellablePoolingValueTask` are available only under `NET6_0_OR_GREATER`, but this is not clearly explained in the docs.

Recommended coverage:

- target framework requirement
- use of `PoolingAsyncValueTaskMethodBuilder`
- aliases: `pvTask`, `cancelablePVTask`
- how pooling differs from regular `valueTask` and `cancellableValueTask`

Sources:

- `src/IcedTasks/PoolingValueTask.fs`
- `src/IcedTasks/CancellablePoolingValueTask.fs`

## Lower Priority

### Polyfill namespaces and shadowing

`IcedTasks.Polyfill.Task` provides replacement `task` and `backgroundTask` builders. Document import strategy and shadowing behavior so users understand when they are using IcedTasks rather than FSharp.Core builders.

Sources:

- `src/IcedTasks/Task.fs`
- `src/IcedTasks/AsyncEx.fs`

### ValueTask.FromCanceled extensions

The project adds `ValueTask.FromCanceled` and `ValueTask.FromCanceled<'T>`, but they are not mentioned in README/docs.

Sources:

- `src/IcedTasks/ValueTask.fs`

### IAsyncEnumerable and disposable support across builders

Tests and source show support for `for` over async enumerables and `use`/`use!` with `IDisposable` and `IAsyncDisposable`. Docs mostly demonstrate this through `AsyncEx`; add a compatibility note for other builders.

Sources:

- `src/IcedTasks/TaskBuilderBase.fs`
- `src/IcedTasks/CancellableTaskBuilderBase.fs`
- `src/IcedTasks/ColdTask.fs`
- `tests/IcedTasks.Tests/*Tests.fs`

### parallelAsync implementation choices

There are two named `parallelAsync` implementations and one default:

- `parallelAsyncUsingStartChild`
- `parallelAsyncUsingStartImmediateAsTask`
- `parallelAsync`

Document the behavioral difference and the default choice.

Sources:

- `src/IcedTasks/ParallelAsync.fs`

## Suggested Documentation Outputs

Recommended next docs to create or update:

- Add a builder compatibility matrix to `docsSrc/index.md`.
- Add a how-to guide for cancellable task families: `CancellableTask`, `CancellableValueTask`, and `cancellablePoolingValueTask`.
- Add an interop guide for `Async`, `Task`, `ValueTask`, `ColdTask`, and cancellable task shapes.
- Add an applicative `and!` guide with examples for serial vs parallel behavior.
- Add a short page on polyfill namespaces and import strategy.
