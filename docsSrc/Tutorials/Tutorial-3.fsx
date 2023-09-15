(**
---
title: A brief introduction to IcedTasks
category: Tutorials
categoryindex: 1
index: 1
---


# A brief introduction to IcedTasks

One of F# Async's benefits is having [cancellation support](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync.html#section3) built in from the beginning. However [Task's do not](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-8.0) and must explicitly be passed a [CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-8.0). This is a problem because it creates a lot of verbosity and boilerplate code. IcedTasks is a library that aims to solve this problem by providing a `cancellableTask` computation expression that can be used to create Tasks that can be cancelled, passing cancellation token along to other cancellableTasks implicitly. And when calling code that takes a CancellationToken, you can easily get at that token and pass it along to the method.

Here we'll take a look at a very small sample of how to use IcedTasks to create a cancellableTask that can be cancelled by a CancellationToken.
*)


#r "nuget: IcedTasks"

open System
open System.Threading
open System.Threading.Tasks

open IcedTasks

/// Helper for unning a Task synchronously
module Task =
    let RunSynchronously (task: Task) = task.GetAwaiter().GetResult()

// Stand in for some database call, like Npgsql
type Person = { Name: string; Age: int }

type Database =
    static member Get<'a>(query, queryParams, cancellationToken: CancellationToken) =
        task {
            do! Task.Delay(1000, cancellationToken)
            return { Name = "Foo"; Age = 42 }
        }

// Stand in for some web call
type WebCall =
    static member HttpGet(route, cancellationToken: CancellationToken) =
        task {
            do! Task.Delay(1000, cancellationToken)
            return { Name = "Foo"; Age = 42 }
        }

let someOtherBusinessLogic (person: Person) =
    cancellableTask {
        let! ct = CancellableTask.getCancellationToken () // A helper to get the current CancellationToken
        let! result = WebCall.HttpGet("https://example.com", ct)
        return result.Age < 1000 // prevent vampires from using our app
    }

let cacheItem (key: string) value =
    async {
        let! ct = Async.CancellationToken // This token will come from the cancellable task

        let! result =
            Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", key ], ct)
            |> Async.AwaitTask

        return ()
    }

let businessLayerCall someParameter =
    cancellableTask {
        // use a lamdbda to get the cancellableTask's current CancellationToken
        // then bind against it like you normally would in any other computation expression
        let! result =
            fun cancellationToken ->
                Database.Get(
                    "SELECT foo FROM bar where baz = @0",
                    [ "@0", someParameter ],
                    cancellationToken
                )

        // This will implicitly pass the CancellationToken along to the next cancellableTask
        let! notVampire = someOtherBusinessLogic result

        // This will implicitly pass the CancellationToken along to async computation expressions as well
        do! cacheItem "buzz" result

        // Conduct some business logic
        if
            result.Age > 18
            && notVampire
        then
            return Some result
        else
            return None
    }


// Now we can use our businessLayerCall like any other Task
let tokenSource = new CancellationTokenSource()
tokenSource.CancelAfter(TimeSpan.FromSeconds(0.5)) // Should cancel our database call

// businessLayerCall is really a string -> CancellationToken -> Task<Option<Person>> so we want to pass in the cancellation token.
businessLayerCall "buzz" tokenSource.Token
|> Task.RunSynchronously

(**

---

For examples on how to use this technique in other contexts checkout:

* [Giraffe](How-To-Guides/Cancellable-Task-In-Giraffe.html)
* [Falco](How-To-Guides/Cancellable-Task-In-Falco.html)

*)
