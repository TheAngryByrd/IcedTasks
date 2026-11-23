---
title: Runtime-async task builders
category: Explanations
categoryindex: 3
index: 7
---

# Runtime-async task builders

.NET 11 adds runtime async. In a runtime-async method, the .NET runtime suspends and resumes the method. The compiler does not generate a state machine for the method.

F# 11 has preview support for runtime async. IcedTasks uses this support in the builders of the `IcedTasks.Polyfill.RuntimeTask` namespace. For the steps to use them, see [Use the runtime-async task builders](../How-To-Guides/Use-runtime-async-task-builders.html).

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

The namespace contains the builders only in the `net11.0` build of IcedTasks. In the builds for older target frameworks, the namespace is empty. Thus, in a project that targets `net10.0`, each builder name gives the state-machine builder, also when the project opens the namespace.

## The builders

When you open the namespace, these builders replace the builders with the same names. The result types do not change, so the companion modules, such as `CancellableTask.map`, continue to work.

| Builder | Result | Replaces |
| --- | --- | --- |
| `task` | `Task<'T>` | FSharp.Core `task` |
| `backgroundTask` | `Task<'T>` | FSharp.Core `backgroundTask` |
| `valueTask` | `ValueTask<'T>` | IcedTasks `valueTask` |
| `coldTask` | `unit -> Task<'T>` | IcedTasks `coldTask` |
| `backgroundColdTask` | `unit -> Task<'T>` | IcedTasks `backgroundColdTask` |
| `cancellableTask` | `CancellationToken -> Task<'T>` | IcedTasks `cancellableTask` |
| `backgroundCancellableTask` | `CancellationToken -> Task<'T>` | IcedTasks `backgroundCancellableTask` |
| `cancellableValueTask` | `CancellationToken -> ValueTask<'T>` | IcedTasks `cancellableValueTask` |

```fsharp
module Orders =
    open System.Threading
    open System.Threading.Tasks
    open IcedTasks.Polyfill.RuntimeTask

    let getTotal (getPrice: int -> CancellationToken -> Task<decimal>) (ids: int list) =
        cancellableTask {
            let mutable total = 0m

            for id in ids do
                let! price = getPrice id
                total <- total + price

            return total
        }
```

The cold and cancellable builders return a function. Each call of the function runs the computation again, as a new runtime-async method.

The `background*` builders start the computation on the thread pool when the caller has a `SynchronizationContext` or a task scheduler that is not the default. Otherwise, they start the computation on the caller thread.

## Supported expressions

The builders support:

- `let!`, `do!`, `return!`, and `and!`.
- `use` and `use!` for `IDisposable` and `IAsyncDisposable` values.
- `for` over `seq<'T>` and `IAsyncEnumerable<'T>`.
- `while`, `try/with`, `try/finally`, and `if`.

`let!` accepts these values in each builder:

- `Task<'T>`, `Task`, `ValueTask<'T>`, and `ValueTask`.
- `Async<'T>`.
- A cold task, a cold awaitable, or a function that returns an awaiter, for example `unit -> Task<'T>`, `unit -> ValueTask`, or `unit -> TaskAwaiter<'T>`.
  If a function returns a type that is both an awaitable and an awaiter, the builder calls its `GetAwaiter` method and awaits the result, as the state-machine builders do.
- An awaiter or an awaitable value, such as the result of `Task.Yield()`.

The cancellable builders also accept a `CancellationToken -> 'Awaitable` or a `CancellationToken -> 'Awaiter` value, for example a `CancellableTask<'T>`, a `CancellableValueTask<'T>`, or a `CancellationToken -> TaskAwaiter<'T>` function.

The builders await a `Task` or a `ValueTask` that an operand gives with `AsyncHelpers.Await`, and other awaiters with `AsyncHelpers.UnsafeAwaitAwaiter`. In the cancellable builders, a `CancellationToken -> 'Awaitable` value, a cold task, and an `Async` give an awaiter, so these builders await them with `AsyncHelpers.UnsafeAwaitAwaiter`.

## Cancellation in the cancellable builders

The cancellable builders keep the `CancellationToken` that the caller gives. This token is the ambient token.

- If the token is cancelled when the caller calls the function, the function returns a cancelled task, and the computation does not run.
- The builder checks the token before each `let!`, `do!`, `return!`, and `and!`, and before each `MoveNextAsync` call and before the body in `for` over an `IAsyncEnumerable<'T>`. If the token is cancelled, the builder throws `OperationCanceledException`, and the task is cancelled.
- The builder gives the token to each bound `CancellationToken -> 'Awaitable` value, `CancellationToken -> 'Awaiter` function, and `Async<'T>` value, and to `GetAsyncEnumerator` in `for` over an `IAsyncEnumerable<'T>`. It calls a function after the check.
- Cancellation stops the computation only at a check or in an operation that uses the token. Code between two checks continues to run.
- In `for` over an `IAsyncEnumerable<'T>`, the builder always awaits a started `MoveNextAsync` before it disposes the enumerator, as C# `await foreach` does. A move that does not use the token delays the cancellation until the move completes.
- The builder does not check the token before `DisposeAsync`, `Dispose`, or a `finally` block. Thus, cleanup runs also after cancellation.
- `return` and `for` over a `seq<'T>` do not check the token.

To get the ambient token in the computation, bind `CancellableTask.getCancellationToken ()`:

```fsharp
let writeAll (stream: System.IO.Stream) (chunks: byte[] list) =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken ()

        for chunk in chunks do
            do! stream.WriteAsync(chunk, ct)
    }
```

## Start order of `and!` operands

The builders do not start a task or a value task that an operand gives. They await the task in the state that the task has. Thus, a task that was created and not started stays incomplete until other code starts it. An awaiter or an awaitable that starts its work only when the builder registers a continuation does not start until the builder awaits it.

The non-cancellable builders evaluate all `and!` operands from left to right before they await the first operand. An `Async`, a cold task, and a cold awaitable start when the builder evaluates them. If the evaluation of an operand throws, that exception is the result, and the builder does not await the operands.

The cancellable builders start a cancellable operand, an `Async`, and a cold task when they bind it, with the ambient token. For `and!`, they start these operands from left to right, and then they await all operands from left to right. If the start of an operand throws, the builder does not start the operands to its right, and the exception is the result. The builders check the token before they await each operand. These rules are the same as in the state-machine cancellable builders.

When the builder awaits the operands, it awaits them from left to right. If an operand fails, the builder does not await the operands to its right, and the result has the exception of that operand. The operands to its right can continue to run. The builder does not read the result of an operand to the right of a failed operand.

F# combines three or more operands from the right. Thus, `let! a = x and! b = y and! c = z` combines `x` with the pair of `y` and `z`. The non-cancellable runtime builders await `x`, then `y`, then `z`. The cancellable builders run the inner pair as one operation, and they start that operation with `x`. In the cancellable builders, the inner pair can read the results of `y` and `z` before `x` completes, and an exception in `y` or `z` waits until the builder awaits the inner pair. The state-machine cancellable builders behave the same way.

## Differences from the state-machine builders

- The F# compiler must inline all of the builder members into the method that contains the computation expression. Each member has `NoDynamicInvocation`, so a call to a member through reflection throws `TargetInvocationException` with a `NotSupportedException` inside it.
- The body of a runtime-async method is small, so the F# optimizer can inline a small function that uses the builder into its caller. If you must see the function name in stack traces, put `[<MethodImpl(MethodImplOptions.NoInlining)>]` on the function.
- The F# compiler gives generated names to the methods that contain the body, such as `func1@12.Invoke`. Stack traces show these names.
- The `background*` builders examine the `SynchronizationContext` and the task scheduler when the caller calls the function or starts the task. The state-machine `backgroundColdTask` and `backgroundCancellableTask` examine them when the computation expression makes the function.
- The runtime-async `coldTask` and `backgroundColdTask` call a bound cold task when they evaluate it. The state-machine `coldTask` calls it at the bind. This changes the order of side effects in `let! a = coldLeft and! b = hotRight ()`.
- In `for` over an `IAsyncEnumerable<'T>`, the state-machine cancellable builders check the token between the `MoveNextAsync` call and its await. If the move cancels the token, they dispose the enumerator while the move is pending. A C# async iterator rejects that with `NotSupportedException`. The runtime builders await the move first.
- The F# compiler issue [dotnet/fsharp#20869](https://github.com/dotnet/fsharp/issues/20869) is open. It reports that a rethrow after a suspending `with` handler or `finally` block can lose the stack trace. `use` and `for` over an `IAsyncEnumerable<'T>` await `DisposeAsync` outside the protected region, so they keep the stack trace. A `with` handler in your own code that suspends and then rethrows can lose it.

## Performance

The benchmarks in `benchmarks/FSharpBenchmarks` compare the state-machine builders and the runtime-async builders on the same code. To run the comparison, run `benchmarks/FSharpBenchmarks/runtimeAsync.ps1`.

The table shows runs on Windows 11 with .NET 11.0.0 on X64. Each value is the time and the allocated memory for one computation expression with ten binds. The rows with binds that suspend come from a run with three processes for each builder kind.

| Builder and binds | State-machine builder | Runtime-async builder |
| --- | --- | --- |
| `task`, ten completed `Task` values | 163.7 ns, 792 B | 30.8 ns, 144 B |
| `valueTask`, ten completed `Task` values | 110.3 ns, 720 B | 14.7 ns, 72 B |
| `cancellableValueTask`, ten completed `CancellableValueTask` values | 154.7 ns, 248 B | 7.1 ns, 0 B |
| `task`, ten binds that suspend | 2.06 μs, 112 B | 2.32 μs, 176 B |
| `valueTask`, ten binds that suspend | 2.16 μs, 120 B | 2.37 μs, 176 B |
| `cancellableTask`, ten binds that suspend | 2.05 μs, 184 B | 2.23 μs, 184 B |

- When the bound values are complete, the runtime-async builders are usually much faster and allocate less memory.
- In these benchmarks, the JIT compiler also removes some `Task.FromResult` allocations, because it can see the whole method. Code that gets its tasks from other methods can get a smaller gain.
- In .NET 11.0.0, a runtime-async method that awaits ten or more `ValueTask` values that it creates itself, such as `let! x = ValueTask<int>(100)`, is slower. The JIT compiler does not inline the awaits in this case. In a test, ten such binds took approximately 40 ns, and nine took approximately 15 ns. C# code with the same shape has the same limit.
- When a bind suspends, the runtime-async builders allocate 176 to 184 B in these benchmarks. The state-machine builders allocate the same memory or less (112 to 184 B). The times differ by approximately 10 %, which is the same as the difference between two runs of the unchanged Ply benchmark.
- When a bind suspends, the state-machine builders give the awaiter with its own type to the .NET method builder, so a .NET awaiter is not boxed. This applies to code that the compiler makes into a static state machine and that is compiled against the `net6.0` or later build of IcedTasks. The dynamic state machine, and code compiled against the `netstandard2.0` or `netstandard2.1` build, still box the awaiter. Code that was compiled against an earlier IcedTasks version keeps the earlier inlined code until it is compiled again.
- In the benchmarks that write to a file, the I/O time is much larger than the cost of the builder. The two kinds of builders give the same result in these benchmarks.

These numbers come from one machine. Measure your own code before you change builders.

## Builders that have no runtime-async version

`taskUnit`, `backgroundTaskUnit`, `valueTaskUnit`, the pooling builders, `asyncEx`, and `parallelAsync` have no runtime-async version. The `vTask` alias continues to give the state-machine `valueTask` builder.

Related:

- [Use the runtime-async task builders](../How-To-Guides/Use-runtime-async-task-builders.html)
- [Polyfill namespaces and shadowing](Polyfill-namespaces-and-shadowing.html)
- [Choosing a builder](Choosing-a-builder.html)
- [Runtime-async specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md)
