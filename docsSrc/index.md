
## What is IcedTasks?

This library contains additional [computation expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) for the [task CE](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) utilizing the [Resumable Code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) introduced [in F# 6.0](https://devblogs.microsoft.com/dotnet/whats-new-in-fsharp-6/#making-f-faster-and-more-interopable-with-task).

- `ValueTask<'T>` - This utilizes .NET's [ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) (which is essentially a [Discriminated Union](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) of `'Value | Task<'Value>`) for possibly better performance in synchronous scenarios. Similar to [F#'s Task Expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions)

- `ColdTask<'T>` - Alias for `unit -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async).

- `CancellableTask<'T>` - Alias for `CancellationToken -> Task<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `CancellableValueTask<'T>` - Alias for `CancellationToken -> ValueTask<'T>`.  Allows for lazy evaluation (also known as Cold) of the tasks, similar to [F#'s Async being cold](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/async#core-concepts-of-async). Additionally, allows for flowing a [CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-6.0) through the computation, similar to [F#'s Async cancellation support](http://tomasp.net/blog/async-csharp-differences.aspx/#:~:text=In%20F%23%20asynchronous%20workflows%2C%20the,and%20everything%20will%20work%20automatically).

- `ParallelAsync<'T>` - Utilizes the [applicative syntax](https://docs.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-50#applicative-computation-expressions) to allow parallel execution of [Async<'T> expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions). See [this discussion](https://github.com/dotnet/fsharp/discussions/11043) as to why this is a separate computation expression.

- `AsyncEx<'T>` - Slight variation of F# async semantics described further below with examples.

## Why should I use IcedTasks?

### AsyncEx

AsyncEx is similar to Async except in the following ways:

1. Allows `use` for [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)

    ```fsharp
    open IcedTasks
    let fakeDisposable = { new IAsyncDisposable with member __.DisposeAsync() = ValueTask.CompletedTask }

    let myAsyncEx = asyncEx {
        use! _ = fakeDisposable
        return 42
    }
    ````
2. Allows `let!/do!` against Tasks/ValueTasks/[any Awaitable](https://devblogs.microsoft.com/pfxteam/await-anything/)

    ```fsharp
    open IcedTasks
    let myAsyncEx = asyncEx {
        let! _ = task { return 42 } // Task<T>
        let! _ = valueTask { return 42 } // ValueTask<T>
        let! _ = Task.Yield() // YieldAwaitable
        return 42
    }
    ```
3. When Tasks throw exceptions they will use the behavior described in [Async.Await overload (esp. AwaitTask without throwing AggregateException](https://github.com/fsharp/fslang-suggestions/issues/840)


    ```fsharp
    let data = "lol"

    let inner = asyncEx {
        do!
            task {
                do! Task.Yield()
                raise (ArgumentException "foo")
                return data
            }
            :> Task
    }

    let outer = asyncEx {
        try
            do! inner
            return ()
        with
        | :? ArgumentException ->
            // Should be this exception and not AggregationException
            return ()
        | ex ->
            return raise (Exception("Should not throw this type of exception", ex))
    }
    ```


### For [ValueTasks](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)

- F# doesn't currently have a `valueTask` computation expression. [Until this PR is merged.](https://github.com/dotnet/fsharp/pull/14755)


```fsharp
open IcedTasks

let myValueTask = task {
    let! theAnswer = valueTask { return 42 }
    return theAnswer
}
```

### For Cold & CancellableTasks
- You want control over when your tasks are started
- You want to be able to re-run these executable tasks
- You don't want to pollute your methods/functions with extra CancellationToken parameters
- You want the computation to handle checking cancellation before every bind.


### ColdTask

Short example:

```fsharp
open IcedTasks

let coldTask_dont_start_immediately = task {
    let mutable someValue = null
    let fooColdTask = coldTask { someValue <- 42 }
    do! Async.Sleep(100)
    // ColdTasks will not execute until they are called, similar to how Async works
    Expect.equal someValue null ""
    // Calling fooColdTask will start to execute it
    do! fooColdTask ()
    Expect.equal someValue 42 ""
}

```

### CancellableTask & CancellableValueTask

The examples show `cancellableTask` but `cancellableValueTask` can be swapped in.

Accessing the context's CancellationToken:

1. Binding against `CancellationToken -> Task<_>`

    ```fsharp
    let writeJunkToFile = 
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)

            for i = 1 to manyIterations do
                // You can do! directly against a function with the signature of `CancellationToken -> Task<_>` to access the context's `CancellationToken`. This is slightly more performant.
                do! fun ct -> file.WriteAsync(junk, 0, junk.Length, ct)
        }
    ```

2. Binding against `CancellableTask.getCancellationToken`

    ```fsharp
    let writeJunkToFile = 
        let path = Path.GetTempFileName()

        cancellableTask {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            // You can bind against `CancellableTask.getCancellationToken` to get the current context's `CancellationToken`.
            let! ct = CancellableTask.getCancellationToken ()
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length, ct)
        }
    ```

Short example:

```fsharp
let executeWriting = task {
    // CancellableTask is an alias for `CancellationToken -> Task<_>` so we'll need to pass in a `CancellationToken`.
    // For this example we'll use a `CancellationTokenSource` but if you were using something like ASP.NET, passing in `httpContext.RequestAborted` would be appropriate.
    use cts = new CancellationTokenSource()
    // call writeJunkToFile from our previous example
    do! writeJunkToFile cts.Token
}


```

### ParallelAsync

- When you want to execute multiple asyncs in parallel and wait for all of them to complete.

Short example:

```fsharp
open IcedTasks

let exampleHttpCall url = async {
    // Pretend we're executing an HttpClient call
    return 42
}

let getDataFromAFewSites = parallelAsync {
    let! result1 = exampleHttpCall "howManyPlantsDoIOwn"
    and! result2 = exampleHttpCall "whatsTheTemperature"
    and! result3 = exampleHttpCall "whereIsMyPhone"

    // Do something meaningful with results
    return ()
}

```

## How do I get started 

    dotnet add nuget IcedTasks

## Who are the maintainers of the project

- @TheAngryByrd


