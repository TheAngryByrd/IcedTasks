namespace IcedTasks


module ValueTask =
    open System.Threading.Tasks

    let ofUnit (vtask: ValueTask) : ValueTask<unit> =
        // this implementation follows Stephen Toub's advice, see:
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vtask.IsCompletedSuccessfully then
            ValueTask<unit>()
        else
            task { return! vtask }
            |> ValueTask<unit>

    let inline toUnit (vtask: ValueTask<'T>) : ValueTask =
        // this implementation follows Stephen Toub's advice, see:
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vtask.IsCompletedSuccessfully then
            // ensure any side effect executes
            vtask.Result
            |> ignore

            ValueTask()
        else
            ValueTask(vtask.AsTask())

[<AutoOpen>]
module ValueTaskExtensions =
    open System.Threading.Tasks

    type Microsoft.FSharp.Control.Async with

        static member inline AwaitValueTask(v: ValueTask<_>) : Async<_> = async {
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if v.IsCompletedSuccessfully then
                return v.Result
            else
                return! Async.AwaitTask(v.AsTask())
        }

        static member inline AwaitValueTask(v: ValueTask) : Async<unit> = async {
            // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
            if v.IsCompletedSuccessfully then
                return ()
            else
                return! Async.AwaitTask(v.AsTask())
        }
