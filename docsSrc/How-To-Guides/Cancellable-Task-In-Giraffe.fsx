(**
---
title: Use CancellableTask in Giraffe
category: How To Guides
categoryindex: 2
index: 1
---


# How to use CancellableTask in Giraffe

[See Giraffe Docs](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md)

To use a cancellableTask with Giraffe, we'll need to get the [RequestAborted](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestaborted?view=aspnetcore-7.0) property off the HttpContext.


We'll start off with a slightly longer version but then make a helper for this.

*)
// This just the dance to get Giraffe to compile for this example.
// Really you'd set up these references in an fsproj
#load "../../runtime-scripts/Microsoft.AspNetCore.App-latest-7.fsx"
#r "nuget: Giraffe"
#r "nuget: IcedTasks"

open Microsoft.AspNetCore.Http
open System.Threading
open System.Threading.Tasks
open IcedTasks
open Giraffe

// This is a stand in for some real database call like Npgsql where it would take a CancellationToken
type Database =
    static member Get(query, queryParams, cancellationToken: CancellationToken) =
        task { do! Task.Delay(10) }


module ExampleVerbose =


    // Some function that's doing the real handler's work
    let myRealWork next ctx =
        cancellableTask {
            // use a lamdbda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            return! json result next ctx
        }

    // A helper to get the context's RequestAborted CancellationToken which will give the cancellableTask
    // the context to pass long.
    let myCustomHandler next (ctx: HttpContext) =
        task {
            let cancellationToken = ctx.RequestAborted
            return! myRealWork next ctx cancellationToken
        }

    // Some Giraffe App
    let app: HttpFunc -> HttpContext -> HttpFuncResult =
        route "/"
        >=> myCustomHandler

(**

Now that we have the basic outline we can refactor this to make `myCustomHandler` take any `cancellableTask`

*)

module ExampleRefactor1 =
    // A helper to get the context's RequestAborted CancellationToken which will give the cancellableTask
    // the context to pass long.
    let inline cancellableHandler
        (cancellableHandler: HttpFunc -> HttpContext -> CancellationToken -> Task<_>)
        (next: HttpFunc)
        (ctx: HttpContext)
        =
        task { return! cancellableHandler next ctx ctx.RequestAborted }

    // Some function that's doing the real handler's work
    let myRealWork next ctx =
        cancellableTask {
            // use a lamdbda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            return! json result next ctx
        }


    // Some Giraffe App
    let app: HttpFunc -> HttpContext -> HttpFuncResult =
        route "/"
        >=> cancellableHandler myRealWork
