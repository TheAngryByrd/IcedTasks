(**
---
title: Compose task values with helper functions
category: How To Guides
categoryindex: 4
index: 6
---

# How to compose task values with helper functions

Use the helper modules when you want to build a pipeline without writing every step as a computation expression.
This is useful when you already have functions that return task-like values and want to combine them with normal pipeline operators.

This page uses `CancellableTask` because it has the richest helper set. The same naming pattern is available on the other helper modules where the operation exists.

*)

(*** hide ***)
#r "../../src/IcedTasks/bin/Release/net9.0/IcedTasks.dll"

open System.Threading
open System.Threading.Tasks
open IcedTasks

(**
## Start with functions that return `CancellableTask`

Each operation is cold and cancellable: it does not start until a caller supplies a `CancellationToken`.

*)

type CustomerId = CustomerId of int

type Customer = { Id: CustomerId; Name: string }

type Order = {
    Id: int
    CustomerId: CustomerId
    Total: decimal
}

type CustomerSummary = {
    Customer: Customer
    Orders: Order array
    Total: decimal
}

let loadCustomer (customerId: CustomerId) : CancellableTask<Customer> =
    cancellableTask {
        let! cancellationToken = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, cancellationToken)

        let (CustomerId id) = customerId

        return {
            Id = customerId
            Name = $"Customer {id}"
        }
    }

let loadOrders (customerId: CustomerId) : CancellableTask<Order array> =
    cancellableTask {
        let! cancellationToken = CancellableTask.getCancellationToken ()
        do! Task.Delay(1, cancellationToken)

        return [|
            {
                Id = 1
                CustomerId = customerId
                Total = 12.50M
            }
            {
                Id = 2
                CustomerId = customerId
                Total = 7.25M
            }
        |]
    }

let summarize customer orders = {
    Customer = customer
    Orders = orders
    Total =
        orders
        |> Array.sumBy _.Total
}

(**
## Transform one result with `map`

`map` means "transform the successful result without starting another async operation".
Use it when your next step is a plain function.

*)

let loadCustomerName customerId =
    customerId
    |> loadCustomer
    |> CancellableTask.map _.Name

(**
## Continue with another async operation using `bind`

`bind` means "wait for this result, then choose the next task-like operation".
Use it when the next step also returns a `CancellableTask`.

*)

let loadCustomerSummary customerId =
    customerId
    |> loadCustomer
    |> CancellableTask.bind (fun customer ->
        customer.Id
        |> loadOrders
        |> CancellableTask.map (summarize customer)
    )

(**
## Apply a task-wrapped function with `apply`

`apply` means "combine a task-like function with a task-like value".
In pipeline terms, it lets you keep both halves in the `CancellableTask` world.

*)

let loadCustomerSummaryWithApply customerId =
    let summaryBuilder =
        customerId
        |> loadCustomer
        |> CancellableTask.map summarize

    customerId
    |> loadOrders
    |> CancellableTask.apply summaryBuilder

(**
## Combine two operations with `zip` or `parallelZip`

`zip` starts the left operation, waits for it, then starts the right operation.
`parallelZip` starts both operations with the same cancellation token before awaiting either result.

Use `zip` when the order or side effects matter. Use `parallelZip` when the operations are independent.

*)

let loadCustomerAndOrdersSerially customerId =
    CancellableTask.zip (loadCustomer customerId) (loadOrders customerId)

let loadCustomerAndOrdersConcurrently customerId =
    CancellableTask.parallelZip (loadCustomer customerId) (loadOrders customerId)

(**
## Run a collection of operations

`whenAll` starts every operation and waits for all results.
`whenAllThrottled` does the same with a maximum degree of parallelism.
`sequential` runs one operation at a time.

*)

let loadSummaries customerIds =
    customerIds
    |> Seq.map loadCustomerSummary
    |> CancellableTask.whenAllThrottled 4

let loadSummariesSequentially customerIds =
    customerIds
    |> Seq.map loadCustomerSummary
    |> CancellableTask.sequential

(**
## Convert between generic and unit-returning tasks

`ofUnit` turns a non-generic `CancellableTask` into `CancellableTask<unit>`.
`toUnit` discards the result of `CancellableTask<'T>` and returns a non-generic `CancellableTask`.

*)

let writeAuditEvent (summary: CustomerSummary) : CancellableTask = fun _ -> Task.CompletedTask

let saveSummary (summary: CustomerSummary) : CancellableTask<int> =
    cancellableTask {
        do! writeAuditEvent summary
        return summary.Orders.Length
    }

let saveSummaryWithoutResult summary =
    summary
    |> saveSummary
    |> CancellableTask.toUnit

let auditAsGenericTask summary =
    summary
    |> writeAuditEvent
    |> CancellableTask.ofUnit

(**
## Execute the pipeline

Call the composed `CancellableTask` with a token at the boundary of your application.

*)

let sampleIds = [
    CustomerId 1
    CustomerId 2
    CustomerId 3
]

let sampleSummaries =
    loadSummaries sampleIds CancellationToken.None
    |> Async.AwaitTask
    |> Async.RunSynchronously

(**
## Helper availability by module

| Helper module | `singleton` | `bind` | `map` | `apply` | `zip` | `parallelZip` | `whenAll` | `whenAllThrottled` | `sequential` | `ofUnit` / `toUnit` |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `CancellableTask` | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| `ColdTask` | Yes | Yes | Yes | Yes | Yes | Yes | No | No | No | Yes |
| `ValueTask` | Yes | Yes | Yes | Yes | Yes | No | No | No | No | Yes |
| `CancellableValueTask` | Yes | Yes | Yes | Yes | Yes | Yes | No | No | No | Yes |

The .NET 6+ pooling builders expose the same `ValueTask` and `CancellableValueTask` helper names for their task shapes.

*)
