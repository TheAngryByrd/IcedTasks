---
title: Explanation 1
category: Explanations
categoryindex: 3
index: 1
---


# Comparison between different Async types



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

