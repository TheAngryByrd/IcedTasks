(**
---
title: Use CancellableTask in a console app
category: How To Guides
categoryindex: 4
index: 1
---


# How to use CancellableTask in a console app

[See Console App docs](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/console-teleprompter)

To use a cancellableTask with a console app, we'll need to create a CancellationTokenSource and pass the CancellationToken to the cancellableTask.

In this example, we'll tie the CancellationTokenSource to the console app's cancellation token so that when the user presses Ctrl+C, the cancellableTask will be cancelled.

*)

#r "nuget: IcedTasks"

open IcedTasks

/// Set of Task based helpers
module Task =
    open System.Threading
    open System.Threading.Tasks

    /// <summary>Queues the specified work to run on the thread pool. Helper for Task.Run</summary>
    let runOnThreadpool (cancellationToken: CancellationToken) (func: unit -> Task<'b>) =
        Task.Run<'b>(func, cancellationToken)

    /// <summary>Helper for t.GetAwaiter().GetResult()</summary>
    let runSynchounously (t: Task<'b>) = t.GetAwaiter().GetResult()

module MainTask =
    open System
    open System.Threading
    open System.Threading.Tasks

    // This is a helper to cancel the CancellationTokenSource if it hasn't already been cancelled or disposed.
    let inline tryCancel (cts: CancellationTokenSource) =
        try
            cts.Cancel()
        with :? ObjectDisposedException ->
            // if CTS is disposed we're probably exiting cleanly
            ()

    // This will set up the cancellation token source to be cancelled when the user presses Ctrl+C or when the app is unloaded
    let setupCloseSignalers (cts: CancellationTokenSource) =
        Console.CancelKeyPress.Add(fun _ ->
            printfn "CancelKeyPress"
            tryCancel cts
        )

        System.Runtime.Loader.AssemblyLoadContext.Default.add_Unloading (fun _ ->
            printfn "AssemblyLoadContext unload"
            tryCancel cts
        )

        AppDomain.CurrentDomain.ProcessExit.Add(fun _ ->
            printfn "ProcessExit"
            tryCancel cts
        )

    let mainAsync (argv: string array) =
        cancellableTask {
            let! ctoken = CancellableTask.getCancellationToken ()
            printfn "Doing work!"
            do! Task.Delay(1000, ctoken)
            printfn "Work done"
            return 0
        }

    [<EntryPoint>]
    let main argv =
        use cts = new CancellationTokenSource()
        //
        setupCloseSignalers cts

        Task.runOnThreadpool cts.Token (fun () -> (mainAsync argv cts.Token))
        // This will block until the cancellableTask is done or cancelled
        // This should only be called once at the start of your app
        |> Task.runSynchounously
