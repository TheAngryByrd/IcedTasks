(**
---
title: Use CancellableTask in Falco
category: How To Guides
categoryindex: 2
index: 1
---


# How to use CancellableTask in Falco

[See Falco Docs](https://www.falcoframework.com/docs/)

To use a cancellableTask with Falco, we'll need to get the [RequestAborted](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestaborted?view=aspnetcore-7.0) property off the HttpContext.


We'll start off with a slightly longer version but then make a helper for this.

*)
// This just the dance to get Falco to compile for this example.
// Really you'd set up these references in an fsproj
#load "../../runtime-scripts/Microsoft.AspNetCore.App-latest-8.fsx"
#r "nuget: Falco"
#r "nuget: IcedTasks"

open Microsoft.AspNetCore.Http
open System.Threading
open System.Threading.Tasks
open IcedTasks
open Falco
open Falco.Routing


// This is a stand in for some real database call like Npgsql where it would take a CancellationToken
type Database =
    static member Get(query, queryParams, cancellationToken: CancellationToken) =
        task { do! Task.Delay(10) }


module ExampleVerbose =


    // Some function that's doing the real handler's work
    let myRealWork ctx =
        cancellableTask {
            // use a lamdbda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            return! Response.ofJson result ctx
        }

    // A helper to get the context's RequestAborted CancellationToken which will give the cancellableTask
    // the context to pass long.
    let myCustomHandler (ctx: HttpContext) =
        task {
            let cancellationToken = ctx.RequestAborted
            return! myRealWork ctx cancellationToken
        }
        :> Task

    let endpoints = [ get "/" myCustomHandler ]

(**

We'll end up creating our own Routing Handler helpers to make it easier when adding to a webapp.

*)


module RoutingC =
    type CancellableHandler = HttpContext -> CancellationToken -> Task<unit>

    // A helper to get the context's RequestAborted CancellationToken which will give the cancellableTask
    // the context to pass long.
    let inline toHandler
        (cancellableHandler: HttpContext -> CancellationToken -> Task<unit>)
        (ctx: HttpContext)
        =
        task { return! cancellableHandler ctx ctx.RequestAborted } :> Task

    /// HttpEndpoint constructor that matches any HttpVerb.
    let any (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route ANY pattern (toHandler handler)

    /// GET HttpEndpoint constructor.
    let get (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route GET pattern (toHandler handler)

    /// HEAD HttpEndpoint constructor.
    let head (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route HEAD pattern (toHandler handler)

    /// POST HttpEndpoint constructor.
    let post (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route POST pattern (toHandler handler)

    /// PUT HttpEndpoint constructor.
    let put (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route PUT pattern (toHandler handler)

    /// PATCH HttpEndpoint constructor.
    let patch (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route PATCH pattern (toHandler handler)

    /// DELETE HttpEndpoint constructor.
    let delete (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route DELETE pattern (toHandler handler)

    /// OPTIONS HttpEndpoint constructor.
    let options (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route OPTIONS pattern (toHandler handler)

    /// TRACE HttpEndpoint construct.
    let trace (pattern: string) (handler: CancellableHandler) : HttpEndpoint =
        route TRACE pattern (toHandler handler)

module ExampleRefactor1 =

    // Some function that's doing the real handler's work
    let myRealWork ctx =
        cancellableTask {
            // use a lamdbda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            return! Response.ofJson result ctx
        }

    let endpoints = [ RoutingC.get "/" myRealWork ]
