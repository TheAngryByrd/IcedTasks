

Below is an example homepage.  You should start by simply explaining your project in plainest and simplest terms possible.

## What is My IcedTasks?

This library contains additional [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) for the [task CE](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) utilizing the [Resumable Code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) introduced [in F# 6.0](https://devblogs.microsoft.com/dotnet/whats-new-in-fsharp-6/#making-f-faster-and-more-interopable-with-task).

- `ValueTask<'T>` - This utilizes .NET's [ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) (which is essentially a [Discriminated Union](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) of `'Value | Task<'Value>`) for possibly better performance in synchronous scenarios. Similar to [F#'s Task Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions)

- `ColdTask<'T>` - Alias for `unit -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async).

- `CancellableTask<'T>` - Alias for `CancellationToken -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `CancellableValueTask<'T>` - Alias for `CancellationToken -> ValueTask<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `ParallelAsync<'T>` - Utilizes the [applicative syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) to allow parallel execution of [Async<'T> expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions). See [this discussion](https://github.com/dotnet/fsharp/discussions/11043) as to why this is a separate computation expression.



| Computation Expression<sup>1</sup> | Library<sup>2</sup> | TFM<sup>3</sup> | Hot/Cold<sup>4</sup> | Multi-start<sup>5</sup> | Tailcalls<sup>6</sup> | CancellationToken propagation<sup>7</sup> | Cancellation checks<sup>8</sup> | Parallel when using and!<sup>9</sup> |
|------------------------------------|---------------------|-----------------|----------------------|-------------------------|-----------------------|-------------------------------------------|---------------------------------|--------------------------------------|
| F# Async                           | FSharp.Core         | netstandard2.0  | Cold                 | multiple                | tailcalls             | implicit                                  | implicit                        | No                                   |
| F# ParallelAsync                   | IcedTasks           | netstandard2.0  | Cold                 | multiple                | tailcalls             | implicit                                  | implicit                        | Yes                                  |
| F# Task/C# Task                    | FSharp.Core         | netstandard2.0  | Hot                  | once-start              | no tailcalls          | explicit                                  | explicit                        | No                                   |
| F# ValueTask                       | IcedTasks           | netstandard2.1  | Hot                  | once-start              | no tailcalls          | explicit                                  | explicit                        | Yes                                  |
| F# ColdTask                        | IcedTasks           | netstandard2.0  | Cold                 | multiple                | no tailcalls          | explicit                                  | explicit                        | Yes                                  |
| F# CancellableTask                 | IcedTasks           | netstandard2.0  | Cold                 | multiple                | no tailcalls          | implicit                                  | implicit                        | Yes                                  |
| F# CancellableValueTask            | IcedTasks           | netstandard2.1  | Cold                 | multiple                | no tailcalls          | implicit                                  | implicit                        | Yes                                  |

- <sup>1</sup> - [Computation Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
- <sup>2</sup> - Which [Nuget](https://www.nuget.org/) package do they come from
- <sup>3</sup> - Which [Target Framework Moniker](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) these are available in
- <sup>4</sup> - Hot refers to the asynchronous code block already been started and will eventually produce a value. Cold refers to the asynchronous code block that is not started and must be started explicitly by caller code. See [F# Async Tutorial](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async) and [Asynchronous C# and F# (II.): How do they differ?](http://tomasp.net/blog/async-csharp-differences.aspx/) for more info.
- <sup>5</sup> - Multi-start refers to being able to start the asynchronous code block again.  See [FAQ on Task Start](https://devblogs.microsoft.com/pfxteam/faq-on-task-start/#:~:text=Question%3A%20Can%20I%20call%20Start,will%20result%20in%20an%20exception.) for more info.
- <sup>6</sup> - Allows use of `let rec` with the computation expression. See [Tail call Recursion](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/recursive-functions-the-rec-keyword#tail-recursion) for more info.
- <sup>7</sup> - `CancellationToken` is propagated to all types the support implicit `CancellatationToken` passing. Calling `cancellableTask { ... }` nested inside `async { ... }` (or any of those combinations) will use the `CancellationToken` from when the code was started.
- <sup>8</sup> - Cancellation will be checked before binds and runs.
- <sup>9</sup> - Allows parallel execution of the asynchronous code using the [Applicative Syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) in computation expressions. 



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


