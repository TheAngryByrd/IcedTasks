namespace IcedTasks.Benchmarks

open System
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs
open System.Threading
open System.Threading.Tasks

open IcedTasks

open System.IO

module Helpers =
    let dele = cancellableTask { do! Task.Yield() }

[<GcServer(true)>]
[<MemoryDiagnoser>]
type WhileLoopBenchmarks() =

    let dele2 = cancellableTask { do! Task.Yield() }

    // [<Params(100, 10000)>]
    [<Params(10000)>]
    member val Length = 0 with get, set


    [<Benchmark(Baseline = true)>]
    member x.CSharp_Whileloop_Tasks() =
        TaskPerfCSharp.Csharp_WhileLoop(x.Length)

    [<Benchmark>]
    member x.CSharp_Whileloop_Tasks_async() =
        TaskPerfCSharp.Csharp_WhileLoop_Async(x.Length)

    // [<Benchmark>]
    // member x.Tasks() = task {
    //     let mutable i = 0

    //     while i < x.Length do
    //         i <- i + 1

    //     return i
    // }

    // [<Benchmark>]
    // member x.Tasks_syncDoBlockTrick() = task {
    //     let mutable i = 0

    //     do
    //         while i < x.Length do
    //             i <- i + 1

    //     return i
    // }


    // [<Benchmark>]
    // member x.Tasks_async() = task {
    //     let mutable i = 0

    //     while i < x.Length do
    //         do! Task.Yield()
    //         i <- i + 1

    //     return i
    // }

    // [<Benchmark>]
    // member x.ValueTask() = valueTask {
    //     let mutable i = 0

    //     while i < x.Length do
    //         i <- i + 1

    //     return i
    // }


    // [<Benchmark>]
    // member x.ValueTask_syncDoBlockTrick() = valueTask {
    //     let mutable i = 0

    //     do
    //         while i < x.Length do
    //             i <- i + 1

    //     return i
    // }


    // [<Benchmark>]
    // member x.ValueTask_async() = valueTask {
    //     let mutable i = 0

    //     while i < x.Length do
    //         do! Task.Yield()
    //         i <- i + 1

    //     return i
    // }

    // [<Benchmark>]
    // member x.Async() =
    //     async {
    //         let mutable i = 0

    //         while i < x.Length do
    //             i <- i + 1

    //         return i
    //     }
    //     |> Async.StartImmediateAsTask

    // [<Benchmark>]
    // member x.Async_syncDoBlockTrick() =
    //     async {
    //         let mutable i = 0

    //         do
    //             while i < x.Length do
    //                 i <- i + 1

    //         return i
    //     }
    //     |> Async.StartImmediateAsTask


    // [<Benchmark>]
    // member x.Async_async() =
    //     async {
    //         let mutable i = 0

    //         while i < x.Length do
    //             do!
    //                 Task.Yield()
    //                 |> AsyncEx.AwaitAwaitable

    //             i <- i + 1

    //         return i
    //     }
    //     |> Async.StartImmediateAsTask


    [<Benchmark>]
    member x.CancellableTask() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask2() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None


        cTask ct


    [<Benchmark>]
    member x.CancellableTask3() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        cTask ct

    [<Benchmark>]
    member x.CancellableTask_syncDoBlockTrick() =
        let cTask = cancellableTask {
            let mutable i = 0

            do
                while i < x.Length do
                    i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_async() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                do! Task.Yield()
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_asyncCancellableLambda() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                do! fun (ct: CancellationToken) -> Task.Yield()
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_asyncCancellableLambda2() =
        let cTask = cancellableTask {
            let mutable i = 0
            let ctYield (cancellationToken: CancellationToken) = Task.Yield()

            while i < x.Length do
                do! ctYield
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    // [<Benchmark>]
    // member x.CancellableTask_asyncCancellableDelegate() =
    //     let cTask = cancellableTask {
    //         let mutable i = 0

    //         while i < x.Length do
    //             do! CancellableTask<unit>(fun ct -> task { do! Task.Yield() })
    //             i <- i + 1

    //         return i
    //     }

    //     let mutable ct = CancellationToken.None

    //     cTask |> CancellableTask.startAsTask &ct


    // [<Benchmark>]
    // member x.CancellableTask_asyncCancellableDelegate2() =
    //     let cTask = cancellableTask {
    //         let mutable i = 0

    //         let dele = CancellableTask<unit>(fun ct -> task { do! Task.Yield() })

    //         while i < x.Length do
    //             do! dele
    //             i <- i + 1

    //         return i
    //     }

    //     let mutable ct = CancellationToken.None

    //     cTask |> CancellableTask.startAsTask &ct

    [<Benchmark>]
    member x.CancellableTask_asyncCancellableCE() =
        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                do! cancellableTask { do! Task.Yield() }
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_asyncCancellableCE2() =
        let cTask = cancellableTask {
            let mutable i = 0

            let dele = cancellableTask { do! Task.Yield() }

            while i < x.Length do
                do! dele
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_asyncCancellableCE3() =
        let dele = cancellableTask { do! Task.Yield() }

        let cTask = cancellableTask {
            let mutable i = 0


            while i < x.Length do
                do! dele
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    member x.dele = dele2

    [<Benchmark>]
    member x.CancellableTask_asyncCancellableCE4() =


        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                do! x.dele
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


    [<Benchmark>]
    member x.CancellableTask_asyncCancellableCE5() =


        let cTask = cancellableTask {
            let mutable i = 0

            while i < x.Length do
                do! Helpers.dele
                i <- i + 1

            return i
        }

        let mutable ct = CancellationToken.None

        CancellableTask.startAsTask &ct cTask


// [<GcServer(true)>]
// [<MemoryDiagnoser>]
// type ForInLoopBenchmarks() =

//     [<Params(100, 10000)>]
//     member val Length = 0 with get, set

//     [<Benchmark(Baseline = true)>]
//     member x.Tasks() = task {
//         let mutable sum = 0
//         let items = [ 0 .. x.Length ]

//         for i in items do
//             sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.Tasks_syncDoBlockTrick() = task {
//         let mutable sum = 0
//         let items = [ 0 .. x.Length ]

//         do
//             for i in items do
//                 sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.ValueTask() = valueTask {
//         let mutable sum = 0
//         let items = [ 0 .. x.Length ]

//         for i in items do
//             sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.ValueTask_syncDoBlockTrick() = valueTask {
//         let mutable sum = 0
//         let items = [ 0 .. x.Length ]

//         do
//             for i in items do
//                 sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.Async() =
//         async {
//             let mutable sum = 0
//             let items = [ 0 .. x.Length ]

//             for i in items do
//                 sum <- sum + i

//             return sum
//         }
//         |> Async.StartAsTask

//     [<Benchmark>]
//     member x.Async_syncDoBlockTrick() =
//         async {
//             let mutable sum = 0
//             let items = [ 0 .. x.Length ]

//             do
//                 for i in items do
//                     sum <- sum + i

//             return sum
//         }
//         |> Async.StartAsTask


//     [<Benchmark>]
//     member x.CancellableTask() =
//         let cTask = cancellableTask {
//             let mutable sum = 0
//             let items = [ 0 .. x.Length ]

//             for i in items do
//                 sum <- sum + i

//             return sum
//         }

//         cTask CancellationToken.None

//     [<Benchmark>]
//     member x.CancellableTask_syncDoBlockTrick() =
//         let cTask = cancellableTask {
//             let mutable sum = 0
//             let items = [ 0 .. x.Length ]

//             do
//                 for i in items do
//                     sum <- sum + i

//             return sum
//         }

//         cTask CancellationToken.None

// [<GcServer(true)>]
// [<MemoryDiagnoser>]
// type ForToLoopBenchmarks() =

//     [<Params(100, 10000)>]
//     member val Length = 0 with get, set

//     [<Benchmark(Baseline = true)>]
//     member x.Tasks() = task {
//         let mutable sum = 0

//         for i = 0 to x.Length do
//             sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.Tasks_syncDoBlockTrick() = task {
//         let mutable sum = 0

//         do
//             for i = 0 to x.Length do
//                 sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.ValueTask() = valueTask {
//         let mutable sum = 0

//         for i = 0 to x.Length do
//             sum <- sum + i

//         return sum
//     }


//     [<Benchmark>]
//     member x.ValueTask_syncDoBlockTrick() = valueTask {
//         let mutable sum = 0

//         do
//             for i = 0 to x.Length do
//                 sum <- sum + i

//         return sum
//     }

//     [<Benchmark>]
//     member x.Async() =
//         async {
//             let mutable sum = 0

//             for i = 0 to x.Length do
//                 sum <- sum + i

//             return sum
//         }
//         |> Async.StartAsTask

//     [<Benchmark>]
//     member x.Async_syncDoBlockTrick() =
//         async {
//             let mutable sum = 0

//             do
//                 for i = 0 to x.Length do
//                     sum <- sum + i

//             return sum
//         }
//         |> Async.StartAsTask


//     [<Benchmark>]
//     member x.CancellableTask() =
//         let cTask = cancellableTask {
//             let mutable sum = 0

//             for i = 0 to x.Length do
//                 sum <- sum + i

//             return sum
//         }

//         cTask CancellationToken.None

//     [<Benchmark>]
//     member x.CancellableTask_syncDoBlockTrick() =
//         let cTask = cancellableTask {
//             let mutable sum = 0

//             do
//                 for i = 0 to x.Length do
//                     sum <- sum + i

//             return sum
//         }

//         cTask CancellationToken.None
