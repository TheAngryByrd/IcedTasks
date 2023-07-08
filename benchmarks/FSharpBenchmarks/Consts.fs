namespace IcedTasks.Benchmarks

open System
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open System.Threading
open System.Threading.Tasks

open IcedTasks

open System.IO


[<AutoOpen>]
module Categories =
    // categories
    [<Literal>]
    let NonAsyncBinds = "NonAsyncBinds"

    [<Literal>]
    let AsyncBinds = "AsyncBinds"

    // languages
    [<Literal>]
    let csharp = "CSharp"

    [<Literal>]
    let fsharp = "FSharp"

    // builders
    [<Literal>]
    let taskBuilder = "TaskBuilder"

    [<Literal>]
    let valueTaskBuilder = "ValueTaskBuilder"

    [<Literal>]
    let asyncBuilder = "AsyncBuilder"

    [<Literal>]
    let plyTaskBuilder = "PlyTaskBuilder"

    [<Literal>]
    let plyValueTaskBuilder = "PlyValueTaskBuilder"

    [<Literal>]
    let cancellableTaskBuilder = "CancellableTaskBuilder"

    [<Literal>]
    let cancellableValueTaskBuilder = "CancellableValueTaskBuilder"

    // binding against

    [<Literal>]
    let bindTask = "BindTask"

    [<Literal>]
    let bindValueTask = "BindValueTask"

    [<Literal>]
    let bindAsync = "BindAsync"

    [<Literal>]
    let bindCancellableTask = "BindCancellableTask"

    [<Literal>]
    let bindCancellableValueTask = "BindCancellableValueTask"


    [<Literal>]
    let manyIterationsConst = 1000

// open Microsoft.FSharp.Core.CompilerServices

// type Awaiters =
//     static member inline GetResult(task: Task<'T>) = task.GetAwaiter().GetResult()

// [<AutoOpen>]
// module AwaitersExtensions =
//     type Awaiters with

//         [<NoEagerConstraintApplication>]
//         static member inline GetResult<'Awaitable, 'Awaiter, 'TResult
//             when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
//             (awaitable: 'Awaitable)
//             =
//             Awaiter.GetResult(Awaitable.GetAwaiter(awaitable))
