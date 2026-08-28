(**
---
title: Build a cancellable pipeline
category: Tutorials
categoryindex: 1
index: 1
---

# Build a cancellable pipeline

This tutorial builds a small request-style pipeline with `cancellableTask`.

You will:

- define operations that receive cancellation automatically
- compose those operations into a larger workflow
- run independent operations with `and!`
- start the pipeline by passing a `CancellationToken` at the boundary

Use `cancellableTask` when cancellation is part of the operation's contract.
The computation expression represents a function:

```fsharp
CancellationToken -> Task<'T>
```

That means the work does not start until the caller supplies a token.
*)

#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System
open System.Threading
open System.Threading.Tasks
open IcedTasks

(**
## Model a small request

The examples below use in-memory stand-ins for database or HTTP calls.
The important part is that each operation can receive a `CancellationToken`.
*)

type Customer = {
    Id: int
    Name: string
    IsActive: bool
}

type OrderSummary = { CustomerId: int; OpenOrders: int }

type Dashboard = {
    CustomerName: string
    OpenOrders: int
    CreditLimit: decimal
}

module Store =
    let loadCustomer customerId =
        cancellableTask {
            let! cancellationToken = CancellableTask.getCancellationToken ()
            do! Task.Delay(10, cancellationToken)

            return {
                Id = customerId
                Name = "Ada"
                IsActive = true
            }
        }

    let loadOpenOrders customerId =
        cancellableTask {
            do! fun cancellationToken -> Task.Delay(10, cancellationToken)

            return {
                CustomerId = customerId
                OpenOrders = 3
            }
        }

    let loadCreditLimit customerId =
        task {
            do! Task.Delay 10
            return if customerId > 0 then 2500.00M else 0.00M
        }

(**
`loadCustomer` and `loadOpenOrders` are cancellable operations. They do not
need a token argument in their public parameter list because `cancellableTask`
will carry the token once the caller starts the pipeline.

`loadCreditLimit` returns a normal `Task<decimal>`. You can still bind it inside
`cancellableTask`; use the cancellable shape for work that needs the ambient
token and bind ordinary task-shaped APIs when you need to interoperate with
them.

## Compose the pipeline

The pipeline first loads a customer. Once that result is available, it can start
the independent order and credit-limit lookups together with `and!`.
*)

let buildDashboard customerId =
    cancellableTask {
        let! customer = Store.loadCustomer customerId

        if not customer.IsActive then
            return Error "The customer is inactive."
        else
            let! orders = Store.loadOpenOrders customer.Id
            and! creditLimit = Store.loadCreditLimit customer.Id

            return
                Ok {
                    CustomerName = customer.Name
                    OpenOrders = orders.OpenOrders
                    CreditLimit = creditLimit
                }
    }

(**
Use `and!` only when the operands are independent. The order lookup and
credit-limit lookup can both start after the customer is loaded, and neither
needs the other's result.

If a later step needs the previous result, use another sequential `let!`.

## Start the work at the boundary

A `CancellableTask<'T>` is started by calling it with a `CancellationToken`.
In an ASP.NET app this token is usually `HttpContext.RequestAborted`. In a
console app or test, create a token source explicitly.
*)

let runTutorial () =
    task {
        use cancellation = new CancellationTokenSource(TimeSpan.FromSeconds 2.0)
        let! result = buildDashboard 42 cancellation.Token

        match result with
        | Ok dashboard ->
            return
                $"%s{dashboard.CustomerName}: %d{dashboard.OpenOrders} open orders, credit limit %M{dashboard.CreditLimit}"
        | Error message -> return message
    }

runTutorial().GetAwaiter().GetResult()

(**
## Next steps

For more details, continue with:

- [Choosing a builder](../Explanations/Choosing-a-builder.html)
- [Use the cancellable task family for request cancellation](../How-To-Guides/Use-cancellable-task-family-for-request-cancellation.html)
- [Use `and!` with independent operations](../How-To-Guides/Use-and-bang-with-independent-operations.html)
*)
