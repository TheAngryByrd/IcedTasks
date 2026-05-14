namespace ILSpySamples

open System.Threading.Tasks
open System.Collections.Generic
open System.Runtime.CompilerServices

#nowarn "42"

module internal Unsafe =
    let inline cast<'a, 'b> (a: 'a) : 'b =

        (# "" a : 'b #)

type SimpleTaskRuntimeBuilder() =

    member this.Return(x: 'T) : Task<'T> = Task.FromResult x

    member this.Bind(awaiter: Task<'T1>, continuation: 'T1 -> Task<'T2>) : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    member this.Bind(awaiter: Task, continuation: unit -> Task<'T2>) : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    member this.Delay(f: unit -> Task<'T>) : unit -> Task<'T> = f

    [<MethodImpl(MethodImplOptions.Async)>]
    member this.Run(f: unit -> Task<'T>) : Task<'T> =
        AsyncHelpers.Await(f ())
        |> Unsafe.cast

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Run(f: Task<'T>) : Task<'T> =
        AsyncHelpers.Await(f)
        |> Unsafe.cast


type SimpleTaskRuntimeBuilderInlined() =

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Return(x: 'T) : Task<'T> =
        x
        |> Task.FromResult
    // |> Unsafe.cast

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Bind
        (awaiter: Task<'T1>, [<InlineIfLambda>] continuation: 'T1 -> Task<'T2>)
        : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Bind
        (awaiter: Task, [<InlineIfLambda>] continuation: unit -> Task<'T2>)
        : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    // [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Delay([<InlineIfLambda>] f: unit -> Task<'T>) : unit -> Task<'T> = f

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Run([<InlineIfLambda>] f: unit -> Task<'T>) : Task<'T> =
        AsyncHelpers.Await(f ())
        |> Unsafe.cast

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Run(f: Task<'T>) : Task<'T> =
        AsyncHelpers.Await(f)
        |> Unsafe.cast


type SimpleTaskRuntimeBuilderInlined2() =

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Return(x: 'T) : Task<'T> =
        x
        |> Task.FromResult
    // |> Unsafe.cast

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Bind
        (awaiter: Task<'T1>, [<InlineIfLambda>] continuation: 'T1 -> Task<'T2>)
        : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Bind
        (awaiter: Task, [<InlineIfLambda>] continuation: unit -> Task<'T2>)
        : Task<'T2> =
        AsyncHelpers.Await awaiter
        |> continuation

    // [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Delay([<InlineIfLambda>] f: unit -> Task<'T>) : unit -> Task<'T> = f

    [<MethodImpl(MethodImplOptions.Async)>]
    member inline this.Run([<InlineIfLambda>] f: unit -> Task<'T>) : Task<'T> =
        AsyncHelpers.Await(f ())
        |> Unsafe.cast

// [<MethodImpl(MethodImplOptions.Async)>]
// member inline this.Run(f: Task<'T>) : Task<'T> =
//     AsyncHelpers.Await(f)
//     |> Unsafe.cast

module TaskRuntime =
    open IcedTasks.Polyfill.TasksRuntime

    let stask = SimpleTaskRuntimeBuilder()
    let sitask = SimpleTaskRuntimeBuilderInlined2()

    [<MethodImpl(MethodImplOptions.Async)>]
    let doThing () =
        sitask {
            do! Task.Delay(100)
            let! x = Task.FromResult 42
            let! y = Task.Run(fun () -> 50)
            let x = x + y
            return x
        }

// module Task =

//     let forLoopEnumerable (x: IEnumerable<_>) =
//         task {
//             for i in x do
//                 do! Task.Yield()
//                 printfn "%A" i
//         }

// module IcedTasks =
//     open IcedTasks

//     module Task =
//         open IcedTasks.Polyfill.Task

//         let forLoopEnumerable (x: IEnumerable<_>) =
//             task {
//                 for i in x do
//                     do! Task.Yield()
//                     printfn "%A" i
//             }


//         let forLoopAsyncEnmerable (x: IAsyncEnumerable<_>) =
//             task {
//                 for i in x do
//                     do! Task.Yield()
//                     printfn "%A" i
//             }

//         let tryFinally () =
//             task {
//                 try
//                     do! Task.Yield()
//                 finally
//                     printfn "finally2"
//             }


//     module CT =
//         open IcedTasks.Polyfill.Task

//         let forLoopEnumerable (x: IEnumerable<_>) =
//             cancellableTask {
//                 for i in x do
//                     do! Task.Yield()
//                     printfn "%A" i
//             }

//         let forLoopAsyncEnmerable (x: IAsyncEnumerable<_>) =
//             cancellableTask {
//                 for i in x do
//                     do! Task.Yield()
//                     printfn "%A" i
//             }


module Main =
    [<EntryPoint>]

    let main _argv =
        TaskRuntime.doThing().GetAwaiter().GetResult()
        |> printfn "Result: %A"

        0
