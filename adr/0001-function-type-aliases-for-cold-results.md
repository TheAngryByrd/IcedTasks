# 0001. Define the cold result types as function type aliases

- Status: accepted
- Date: 2022-03-03
- Source: https://github.com/TheAngryByrd/IcedTasks/commit/68d9036
- Reason source: confirmed by maintainer on 2026-10-26

A cold value runs only when a caller starts it, and a caller can start it many times.
The library needs a result type for each cold builder.

We define each cold result type as an alias of a function type.
`ColdTask.fs` has `type ColdTask<'T> = unit -> Task<'T>`, and `CancellableTask.fs` has `type CancellableTask<'T> = CancellationToken -> Task<'T>`.
`CancellableValueTask.fs` and `CancellablePoolingValueTask.fs` use the same pattern.

Plain functions interoperate with existing code at no cost, and they need no wrapper allocation.
Any `fun ct -> task { ... }` is already a `CancellableTask<'T>`, and a caller starts one with `myTask ct`.

## Consequences

The compiler cannot tell a `CancellableTask<'T>` from another function of the same shape.
The result types cannot implement interfaces or have their own members.
