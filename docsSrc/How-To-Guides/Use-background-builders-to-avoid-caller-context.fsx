(**
---
title: Use background builders to avoid caller context
category: How To Guides
categoryindex: 4
index: 10
---

# How to use background builders to avoid caller context

Use a `background*` builder when library or internal async work should not stay tied to the caller's current synchronization context or scheduler.
This is similar in intent to `ConfigureAwait(false)`: the code is saying it does not need to resume on the caller's context.

A UI thread is a single thread that owns an application's visual controls and event loop.
Frameworks like WinForms, WPF, and MAUI generally require UI updates to happen on that thread.
To make `await` convenient, those frameworks install a [`SynchronizationContext`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext) so async continuations can resume on the UI thread after an await.
Microsoft's [ExecutionContext and SynchronizationContext](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/executioncontext-synchronizationcontext) article is a good deeper reference for how this interacts with `async`/`await` and `ConfigureAwait(false)`.

That is useful for UI code that needs to update controls after awaiting. It is usually not useful for library code that reads files, calls services, parses data, or performs other work that does not touch UI controls.

ASP.NET Core usually does not have a UI-style `SynchronizationContext`, so `backgroundTask` is less commonly needed there. The same idea still applies to any host that installs a custom synchronization context or task scheduler.

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System
open System.Threading
open System.Threading.Tasks
open IcedTasks

(**
## Use `backgroundTask` for context-independent library work

Use `task` when the work should follow the caller's normal context behavior.
Use `backgroundTask` when the work does not need that context.

*)

let parseProfile (text: string) = text.Trim()

let loadProfileText () =
    backgroundTask {
        do! Task.Delay 1
        return parseProfile " Ada "
    }

(**
## See the difference with a fake synchronization context

The example below installs a small `SynchronizationContext` that records whether an async continuation posted back through it.
`task` posts back through the context after `Task.Yield()`.

*)

type RecordingSynchronizationContext() =
    inherit SynchronizationContext()

    let mutable postCount = 0

    member _.PostCount = postCount

    override _.Post(callback, state) =
        postCount <-
            postCount
            + 1

        callback.Invoke state

let runWithSynchronizationContext (context: SynchronizationContext) (work: unit -> Task<'T>) =
    let previous = SynchronizationContext.Current
    SynchronizationContext.SetSynchronizationContext context

    try
        work().GetAwaiter().GetResult()
    finally
        SynchronizationContext.SetSynchronizationContext previous

let normalTaskPostCount =
    let context = RecordingSynchronizationContext()

    runWithSynchronizationContext
        context
        (fun () ->
            task {
                do! Task.Yield()
                return context.PostCount
            }
        )

(**
`backgroundTask` escapes to the thread pool when a synchronization context or non-default scheduler is present, so it does not post through that caller context.
If it is already running on the thread pool with the default scheduler, it avoids adding an extra hop.

*)

let backgroundTaskPostCount =
    let context = RecordingSynchronizationContext()

    runWithSynchronizationContext
        context
        (fun () ->
            backgroundTask {
                do! Task.Yield()
                return context.PostCount
            }
        )

(**
## Use the matching background shape

Pick the task shape first, then use the background variant only when you want to avoid the caller context.

| Normal builder | Background builder | Result shape |
|---|---|---|
| `task` | `backgroundTask` | `Task<'T>` |
| `taskUnit` | `backgroundTaskUnit` | `Task` |
| `coldTask` | `backgroundColdTask` | `unit -> Task<'T>` |
| `cancellableTask` | `backgroundCancellableTask` | `CancellationToken -> Task<'T>` |

*)

let saveProfileAudit () = backgroundTaskUnit { do! Task.Delay 1 }

let loadProfileLater () =
    backgroundColdTask {
        let! text = loadProfileText ()
        return text
    }

let loadProfileForRequest () =
    backgroundCancellableTask {
        let! cancellationToken = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, cancellationToken)
        return "Ada"
    }

(**
## Run the examples

These values make the context behavior visible:

- `normalTaskPostCount` is greater than zero because `task` posted through the installed context.
- `backgroundTaskPostCount` is zero because `backgroundTask` avoided that context.

*)

let profileText = loadProfileText().GetAwaiter().GetResult()
let auditResult = saveProfileAudit().GetAwaiter().GetResult()

let laterProfile =
    let operation = loadProfileLater ()
    operation().GetAwaiter().GetResult()

let requestProfile =
    (loadProfileForRequest ()) CancellationToken.None
    |> Async.AwaitTask
    |> Async.RunSynchronously

(**
## When not to use a background builder

Do not use a background builder when the continuation must run on the caller's context.
For example, UI code that updates controls after an await normally needs to resume on the UI thread.

For ASP.NET Core request handlers, start with the non-background builder unless you have a specific custom scheduler or context concern.

*)
