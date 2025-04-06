(**
---
title: Use CancellableTask in ASP.NET Minimal API
category: How To Guides
categoryindex: 2
index: 1
---


# How to use CancellableTask in  ASP.NET Minimal API

[See Minimal API docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-9.0#routing)

To use a cancellableTask with Minimal APIs, we'll need to get the [RequestAborted](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestaborted?view=aspnetcore-7.0) property off the HttpContext.


We'll start off with a slightly longer version but then make a helper for this.

*)
#load "../../runtime-scripts/Microsoft.AspNetCore.App-latest-8.fsx"
#r "nuget: IcedTasks"


open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open System.Threading
open System.Threading.Tasks
open IcedTasks

// This is a stand-in for some real database call like Npgsql where it would take a CancellationToken
type Database =
    static member Get(query, queryParams, cancellationToken: CancellationToken) =
        task { do! Task.Delay(10, cancellationToken) }

module ExampleVerbose =

    // Some function that's doing the real handler's work
    let myRealWork (ctx: HttpContext) =
        cancellableTask {
            // Use a lambda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsJsonAsync(result)
        }

    // A helper to get the context's RequestAborted CancellationToken and pass it to the cancellableTask
    let myCustomHandler (ctx: HttpContext) =
        task {
            let cancellationToken = ctx.RequestAborted
            return! myRealWork ctx cancellationToken
        }

    // Minimal API app
    let app =
        let builder = WebApplication.CreateBuilder()
        let app = builder.Build()

        // MapGet requires a RequestDelegate, so we need wrap it since there's no implicit conversion
        app.MapGet("/", RequestDelegate(fun ctx -> myCustomHandler ctx))
        |> ignore

        app

module ExampleRefactor1 =
    open System

    // A helper to get the context's RequestAborted CancellationToken and pass it to any cancellableTask
    // Remember a CancellableTask is a function with the signature of CancellationToken -> Task<'T>
    let inline cancellableHandler (cancellableHandler: HttpContext -> CancellableTask<unit>) =
        //ASP.NET MapGet requires a RequestDelegate, so we need wrap it since there's no implicit conversion
        RequestDelegate(fun ctx -> cancellableHandler ctx ctx.RequestAborted)

    // Some function that's doing the real handler's work
    let myRealWork (ctx: HttpContext) =
        cancellableTask {
            // Use a lambda to get the cancellableTask's current CancellationToken
            let! result =
                fun ct -> Database.Get("SELECT foo FROM bar where baz = @0", [ "@0", "buzz" ], ct)

            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsJsonAsync(result)
        }

    // Minimal API app
    let app =
        let builder = WebApplication.CreateBuilder()
        let app = builder.Build()

        app.MapGet("/", cancellableHandler myRealWork)
        |> ignore

        app
