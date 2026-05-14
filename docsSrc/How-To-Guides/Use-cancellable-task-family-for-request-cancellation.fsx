(**
---
title: Use the cancellable task family for request cancellation
category: How To Guides
categoryindex: 2
index: 1
---

# How to use the cancellable task family for request cancellation

Use the cancellable builders when cancellation is part of the operation's contract.
In web applications, the boundary token is commonly `HttpContext.RequestAborted`; in other hosts, it may come from a message processor, worker shutdown token, or command timeout.

The cancellable task family uses the same shape:

- `CancellableTask<'T>` is `CancellationToken -> Task<'T>`
- `CancellableValueTask<'T>` is `CancellationToken -> ValueTask<'T>`
- `cancellablePoolingValueTask` also returns `CancellationToken -> ValueTask<'T>` and uses a pooling method builder on .NET 6+

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System
open System.Threading
open System.Threading.Tasks
open IcedTasks

(**
## Pass the request token at the boundary

The important boundary rule is simple: keep the cancellation-aware workflow as a value, then start it by passing the request token.
This sample uses a minimal request context so the example stays focused on the token flow. In ASP.NET, this token is `HttpContext.RequestAborted`.

*)

type RequestContext = { RequestAborted: CancellationToken }

type OrderId = OrderId of int

type Order = {
    Id: OrderId
    Total: decimal
    CanCompleteSynchronously: bool
}

module OrderStore =
    let loadAsTask (orderId: OrderId) (cancellationToken: CancellationToken) =
        task {
            do! Task.Delay(1, cancellationToken)

            return {
                Id = orderId
                Total = 42.00M
                CanCompleteSynchronously = false
            }
        }

    let loadAsValueTask (orderId: OrderId) (cancellationToken: CancellationToken) =
        cancellationToken.ThrowIfCancellationRequested()

        let (OrderId id) = orderId

        if id = 0 then
            ValueTask<Order> {
                Id = orderId
                Total = 0.00M
                CanCompleteSynchronously = true
            }
        else
            task {
                do! Task.Delay(1, cancellationToken)

                return {
                    Id = orderId
                    Total = 42.00M
                    CanCompleteSynchronously = false
                }
            }
            |> ValueTask<Order>

    let writeAudit (order: Order) (cancellationToken: CancellationToken) =
        task {
            do! Task.Delay(1, cancellationToken)
            return order
        }

(**
## Use `cancellableTask` when the work is actually async

Use `cancellableTask` when the operation naturally returns `Task<'T>` or is expected to do asynchronous work.
Inside the builder, `CancellableTask.getCancellationToken()` gives you the token supplied by the caller.

*)

let loadOrderWithTask (orderId: OrderId) : CancellableTask<Order> =
    cancellableTask {
        let! cancellationToken = CancellableTask.getCancellationToken ()
        let! order = OrderStore.loadAsTask orderId cancellationToken
        return! OrderStore.writeAudit order cancellationToken
    }

let handleTaskRequest (ctx: RequestContext) (orderId: OrderId) =
    task { return! loadOrderWithTask orderId ctx.RequestAborted }

(**
## Use `cancellableValueTask` when the work might complete synchronously

Use `cancellableValueTask` when the same cancellation model should return `ValueTask<'T>`.
This is useful for APIs that often complete synchronously, such as cache hits, but sometimes need asynchronous work.

*)

let loadOrderWithValueTask (orderId: OrderId) : CancellableValueTask<Order> =
    cancellableValueTask {
        let! cancellationToken = CancellableValueTask.getCancellationToken ()
        let! order = OrderStore.loadAsValueTask orderId cancellationToken
        return order
    }

let handleValueTaskRequest (ctx: RequestContext) (orderId: OrderId) =
    task { return! loadOrderWithValueTask orderId ctx.RequestAborted }

(**
## Consider `cancellablePoolingValueTask` for allocation-sensitive .NET 6+ code

Use `cancellablePoolingValueTask` when you want the `CancellableValueTask<'T>` shape in allocation-sensitive .NET 6+ code.
It uses `PoolingAsyncValueTaskMethodBuilder`, so it is an advanced option for hot paths where `ValueTask` already makes sense.

*)

let loadOrderWithPoolingValueTask (orderId: OrderId) : CancellableValueTask<Order> =
    cancellablePoolingValueTask {
        let! order = fun ct -> OrderStore.loadAsValueTask orderId ct
        return order
    }

let handlePoolingValueTaskRequest (ctx: RequestContext) (orderId: OrderId) =
    task { return! loadOrderWithPoolingValueTask orderId ctx.RequestAborted }

(**
## Await from `Async` when needed

The `Async.AwaitCancellableTask` and `Async.AwaitCancellableValueTask` helpers preserve `Async`'s cancellation token.
Use them at interop boundaries where the surrounding workflow is still `Async<'T>`.

*)

let loadWithAsyncInterop orderId =
    async {
        let! order = loadOrderWithValueTask orderId
        return order.Total
    }

(**
## Run the examples

At the application boundary, pass the request token once.
Every nested bind can then retrieve or receive the same token.

*)

let request = {
    RequestAborted = CancellationToken.None
}

let loadedWithTask =
    handleTaskRequest request (OrderId 42)
    |> fun task -> task.GetAwaiter().GetResult()

let loadedWithValueTask =
    handleValueTaskRequest request (OrderId 0)
    |> fun task -> task.GetAwaiter().GetResult()

let loadedWithPoolingValueTask =
    handlePoolingValueTaskRequest request (OrderId 42)
    |> fun task -> task.GetAwaiter().GetResult()

(**
## Choose the shape

| Builder | Return shape | Use when |
|---|---|---|
| `cancellableTask` | `CancellationToken -> Task<'T>` | The work is actually asynchronous or the called APIs already return `Task<'T>`. |
| `cancellableValueTask` | `CancellationToken -> ValueTask<'T>` | The operation may complete synchronously, and you want to optimize that path. |
| `cancellablePoolingValueTask` | `CancellationToken -> ValueTask<'T>` | The code targets .NET 6+ and is allocation-sensitive enough that pooling is worth considering. |

All three are cold and multi-start: creating the value does not start the work, and each call with a token starts a fresh operation.

*)
