namespace ILSpySamples

open System.Threading.Tasks
open System.Collections.Generic


module Task =

    let forLoopEnumerable (x: IEnumerable<_>) =
        task {
            for i in x do
                do! Task.Yield()
                printfn "%A" i
        }

module IcedTasks =
    open IcedTasks

    module Task =
        open IcedTasks.Polyfill.Task

        let forLoopEnumerable (x: IEnumerable<_>) =
            task {
                for i in x do
                    do! Task.Yield()
                    printfn "%A" i
            }


        let forLoopAsyncEnmerable (x: IAsyncEnumerable<_>) =
            task {
                for i in x do
                    do! Task.Yield()
                    printfn "%A" i
            }

        let tryFinally () =
            task {
                try
                    do! Task.Yield()
                finally
                    printfn "finally2"
            }


    module CT =
        open IcedTasks.Polyfill.Task

        let forLoopEnumerable (x: IEnumerable<_>) =
            cancellableTask {
                for i in x do
                    do! Task.Yield()
                    printfn "%A" i
            }

        let forLoopAsyncEnmerable (x: IAsyncEnumerable<_>) =
            cancellableTask {
                for i in x do
                    do! Task.Yield()
                    printfn "%A" i
            }
