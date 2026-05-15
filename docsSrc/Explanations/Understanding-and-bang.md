---
title: Understanding and!
category: Explanations
categoryindex: 3
index: 4
---

# Understanding `and!`

[`and!` is F# computation expression syntax for applicative composition](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions#and). In IcedTasks, use it when multiple operations are independent and the following code needs all of their results.

The baseline behavior is `cancellableTask`:

```fsharp
let combined =
    cancellableTask {
        let! customer = loadCustomer customerId
        and! orders = loadOrders customerId
        return customer, orders
    }
```

For `cancellableTask`, both operations receive the same ambient `CancellationToken` and are started before either result is awaited. This is concurrent start, not a guarantee of CPU parallelism. A task may complete synchronously, continue on a thread-pool thread, or block synchronously if the underlying operation blocks.

## Why `and!` is different from sequential `let!`

Sequential `let!` means the second operation is not started until the first result is available:

```fsharp
cancellableTask {
    let! customer = loadCustomer customerId
    let! orders = loadOrders customerId
    return customer, orders
}
```

`and!` says the operations do not depend on each other:

```fsharp
cancellableTask {
    let! customer = loadCustomer customerId
    and! orders = loadOrders customerId
    return customer, orders
}
```

Use `and!` only when each operand can start without the result of the others.

## Builder behavior

| Builder family | What `and!` combines | Start behavior |
|---|---|---|
| `cancellableTask`, `backgroundCancellableTask` | Cancellable operations and supported awaitables | Gets the ambient token, starts token-dependent operands with that token, then awaits results. |
| `cancellableValueTask`, `cancellablePoolingValueTask` | Cancellable ValueTask operations and supported awaitables | Same token-flow model as `cancellableTask`, returning `ValueTask` for each start. |
| `coldTask`, `backgroundColdTask` | Cold task operations and supported awaitables | Invokes the cold operands before awaiting either result. |
| `task`, `backgroundTask`, `taskUnit`, `backgroundTaskUnit` | Task-like awaitables | Combines awaitables in the task builder. The operands are task-like values, so any work represented by them may already be started. |
| `valueTask`, `poolingValueTask`, `valueTaskUnit` | ValueTask-like awaitables | Combines awaitables in the value-task builder. Treat each `ValueTask` result as single-consumption. |
| `parallelAsync` | `Async<'T>` values | Explicitly starts `Async` values in parallel. The default `parallelAsync` uses `Async.StartImmediateAsTask`. |

`asyncEx` does not provide `and!`. Use `parallelAsync` when you specifically want `Async<'T>` operations to start in parallel with applicative syntax.

## Operand mixing

The task-like builders support more than one operand shape. For example, a `cancellableTask` expression can combine cancellable work, `Task`, `ValueTask`, cold task work, and other awaitables when the builder has a `Source` overload for that shape.

That flexibility is for interop. It does not change the core rule: use `and!` for independent operations, and use sequential `let!` when one operation needs the previous result.

For compiler-checked examples, see [Use and! with independent operations](../How-To-Guides/Use-and-bang-with-independent-operations.html).
