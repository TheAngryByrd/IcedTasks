---
title: Runtime-async task builders
category: Explanations
categoryindex: 3
index: 7
---

# Runtime-async task builders

.NET 11 adds runtime async. In a runtime-async method, the .NET runtime suspends and resumes the method. The compiler does not generate a state machine for the method.

F# 11 has preview support for runtime async. IcedTasks uses this support in the `task`, `backgroundTask`, and `valueTask` builders of the `IcedTasks.Polyfill.RuntimeTask` namespace.

## Requirements

A project that uses these builders must:

- Target `net11.0` or later.
- Use the .NET 11 SDK.
- Set `LangVersion` to `preview`.
- Run on a .NET 11 or later runtime.

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>preview</LangVersion>
</PropertyGroup>
```

If `LangVersion` is not `preview`, the compiler stops with error `FS3350: Feature 'runtime async' is not available in F# 11.0`.

Runtime async is a preview feature of F# 11. A later F# compiler can change the requirements or the behavior.

Each `net11.0` project that references IcedTasks gets the `net11.0` build, also when it does not use these builders. That build depends on FSharp.Core 11.0.101 or later.

The namespace contains the builders only in the `net11.0` build of IcedTasks. In the builds for older target frameworks, the namespace is empty. Thus, in a project that targets `net10.0`, `task` gives the FSharp.Core builder, also when the project opens the namespace.

## Use the builders

Open the namespace in the module that uses the builders. The builders shadow the FSharp.Core `task` and `backgroundTask` builders, and the IcedTasks `valueTask` builder.

```fsharp
module Orders =
    open System.Threading.Tasks
    open IcedTasks.Polyfill.RuntimeTask

    let getTotal (getPrice: int -> Task<decimal>) (ids: int list) =
        task {
            let mutable total = 0m

            for id in ids do
                let! price = getPrice id
                total <- total + price

            return total
        }
```

| Builder | Result | Return marker |
| --- | --- | --- |
| `task` | `Task<'T>` | `__runtimeAsyncReturn` |
| `backgroundTask` | `Task<'T>` | `__runtimeAsyncReturn` |
| `valueTask` | `ValueTask<'T>` | `__runtimeAsyncReturnValueTask` |

`backgroundTask` starts the computation on the thread pool when the caller has a `SynchronizationContext` or a task scheduler that is not the default. Otherwise, it starts the computation on the caller thread.

## Supported expressions

The builders support:

- `let!`, `do!`, `return!`, and `and!`.
- `use` and `use!` for `IDisposable` and `IAsyncDisposable` values.
- `for` over `seq<'T>` and `IAsyncEnumerable<'T>`.
- `while`, `try/with`, `try/finally`, and `if`.

`let!` accepts these values:

- `Task<'T>`, `Task`, `ValueTask<'T>`, and `ValueTask`.
- `Async<'T>`. The builder starts it with `Async.StartImmediateAsTask`.
- A cold task, `unit -> Task<'T>` or `unit -> Task`. The builder starts it at the bind.
- An awaiter or an awaitable value, such as the result of `Task.Yield()`.

The builder awaits `Task` and `ValueTask` values with `AsyncHelpers.Await`, and other awaiters with `AsyncHelpers.UnsafeAwaitAwaiter`.

The builder evaluates all `and!` operands from left to right before it awaits the first operand. An `Async` and a cold task start when the builder evaluates them. The builder does not start a task or a value task. It awaits the task in the state that the task has, so a task that was created and not started stays incomplete until other code starts it. An awaiter or an awaitable that starts its work only when the builder registers a continuation does not start until the builder awaits it.

If the evaluation of an operand throws, for example a cold task that throws before it returns its task, that exception is the result. The builder does not await the operands.

After the evaluation, the builder awaits the operands from left to right. If an operand fails, the builder does not await the operands to its right, and the result has the exception of that operand. The operands to its right can continue to run.

## Differences from the state-machine builders

- The F# compiler must inline all of the builder members into the method that contains the computation expression. Each member has `NoDynamicInvocation`, so a call to a member through reflection throws `TargetInvocationException` with a `NotSupportedException` inside it.
- The body of a runtime-async method is small, so the F# optimizer can inline a small function that uses the builder into its caller. If you must see the function name in stack traces, put `[<MethodImpl(MethodImplOptions.NoInlining)>]` on the function.
- The F# compiler gives generated names to the methods that contain the body, such as `func1@12.Invoke`. Stack traces show these names.
- The F# compiler issue [dotnet/fsharp#20869](https://github.com/dotnet/fsharp/issues/20869) is open. It reports that a rethrow after a suspending `with` handler or `finally` block can lose the stack trace. `use` and `for` over an `IAsyncEnumerable<'T>` await `DisposeAsync` outside the protected region, so they keep the stack trace. A `with` handler in your own code that suspends and then rethrows can lose it.

## Builders that have no runtime-async version

`taskUnit`, `backgroundTaskUnit`, `valueTaskUnit`, `coldTask`, `backgroundColdTask`, `cancellableTask`, `backgroundCancellableTask`, `cancellableValueTask`, and the pooling builders have no runtime-async version. The `vTask` alias continues to give the state-machine `valueTask` builder.

Related:

- [Polyfill namespaces and shadowing](Polyfill-namespaces-and-shadowing.html)
- [Choosing a builder](Choosing-a-builder.html)
- [Runtime-async specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)
