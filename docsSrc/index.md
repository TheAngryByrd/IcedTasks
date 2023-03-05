
## What is IcedTasks?

This library contains additional [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) for the [task CE](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) utilizing the [Resumable Code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) introduced [in F# 6.0](https://devblogs.microsoft.com/dotnet/whats-new-in-fsharp-6/#making-f-faster-and-more-interopable-with-task).

- `ValueTask<'T>` - This utilizes .NET's [ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) (which is essentially a [Discriminated Union](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) of `'Value | Task<'Value>`) for possibly better performance in synchronous scenarios. Similar to [F#'s Task Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions)

- `ColdTask<'T>` - Alias for `unit -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async).

- `CancellableTask<'T>` - Alias for `CancellationToken -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `CancellableValueTask<'T>` - Alias for `CancellationToken -> ValueTask<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `ParallelAsync<'T>` - Utilizes the [applicative syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) to allow parallel execution of [Async<'T> expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions). See [this discussion](https://github.com/dotnet/fsharp/discussions/11043) as to why this is a separate computation expression.

## Why should I use IcedTasks?

### For ValueTasks

F# doesn't currently have a `valueTask` computation expression.

### For Cold & CancellableTasks
- You want control over when your tasks are started
- You want to be able to re-run these executable tasks
- You don't want to pollute your methods/functions with extra CancellationToken parameters
- You want the computation to handle checking cancellation before every bind.


## How do I get started 

    dotnet add nuget IcedTasks

## Who are the maintainers of the project

- @TheAngryByrd


