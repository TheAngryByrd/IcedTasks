namespace IcedTasks

open System
open System.Threading

type Async =
    static member inline map f x =
        async.Bind(x, (fun v -> async.Return(f v)))

type ParallelAsync =
    /// <summary>
    /// Executes two asyncs concurrently and returns a tuple of the values
    /// </summary>
    /// <param name="a1">An async to execute</param>
    /// <param name="a2">An async to execute</param>
    /// <returns>Tuple of computed values</returns>
    static member zipWithStartChild (a1: Async<'left>) (a2: Async<'right>) : Async<'left * 'right> =
        // it is not advised to use async {} blocks in the implementation because it can go recursive... see https://thinkbeforecoding.com/post/2020/10/07/applicative-computation-expressions
        // This is the same as:
        // async {
        //     let! c1 = a1 |> Async.StartChild
        //     let! c2 = a2 |> Async.StartChild
        //     let! r1 = c1
        //     let! r2 = c2
        //     return r1,r2
        // }
        async.Bind(
            Async.StartChild a1,
            fun c1 ->
                async.Bind(
                    Async.StartChild a2,
                    fun c2 ->
                        async.Bind(c1, (fun r1 -> async.Bind(c2, (fun r2 -> async.Return(r1, r2)))))
                )
        )


    /// <summary>
    /// Executes two asyncs concurrently and returns a tuple of the values
    /// </summary>
    /// <param name="a1">An async to execute</param>
    /// <param name="a2">An async to execute</param>
    /// <returns>Tuple of computed values</returns>
    static member inline zipUsingStartImmediateAsTask
        (a1: Async<'left>)
        (a2: Async<'right>)
        : Async<'left * 'right> =
        // async {
        //     let! ct = Async.CancellationToken
        //     let x = Async.StartImmediateAsTask (a1, cancellationToken=ct)
        //     let y = Async.StartImmediateAsTask (a2, cancellationToken=ct)
        //     let! x' = Async.AwaitTask x
        //     let! y' = Async.AwaitTask y
        //     return x', y'
        // }

        async.Bind(
            Async.CancellationToken,
            fun ct ->
                let t1 = Async.StartImmediateAsTask(a1, cancellationToken = ct)
                let t2 = Async.StartImmediateAsTask(a2, cancellationToken = ct)

                async.Bind(
                    Async.AwaitTask t1,
                    fun t1r -> async.Bind(Async.AwaitTask t2, (fun t2r -> async.Return(t1r, t2r)))
                )
        )

type ParallelAsyncBuilderBase() =

    member inline _.Zero() = async.Zero()

    member inline _.Delay generator = async.Delay generator

    member inline _.Return value = async.Return value

    member inline _.ReturnFrom(computation: Async<_>) = async.ReturnFrom computation

    member inline _.Bind(computation, binder) = async.Bind(computation, binder)

    member inline _.Using(resource, binder) = async.Using(resource, binder)

    member inline _.While(guard, computation) = async.While(guard, computation)

    member inline _.For(sequence, body) = async.For(sequence, body)

    member inline _.Combine(computation1, computation2) =
        async.Combine(computation1, computation2)

    member inline _.TryFinally(computation, compensation) =
        async.TryFinally(computation, compensation)

    member inline _.TryWith(computation, catchHandler) =
        async.TryWith(computation, catchHandler)

    member inline _.BindReturn(x: Async<'T>, f) = Async.map f x

type ParallelAsyncBuilderUsingStartChild() =
    inherit ParallelAsyncBuilderBase()

    member inline _.MergeSources(t1: Async<'T>, t2: Async<'T1>) =
        ParallelAsync.zipWithStartChild t1 t2


type ParallelAsyncBuilderUsingStartImmediateAsTask() =
    inherit ParallelAsyncBuilderBase()

    member inline _.MergeSources(t1: Async<'T>, t2: Async<'T1>) =
        ParallelAsync.zipUsingStartImmediateAsTask t1 t2

[<AutoOpen>]
module Asyncs =
    /// <summary>
    /// Async computation expression which allows for parallel execution of asyncs with the applicative (and!) syntax.  This uses <c>Async.StartChild</c> to start async computations in parallel.
    /// </summary>
    /// <returns></returns>
    let parallelAsyncUsingStartChild = ParallelAsyncBuilderUsingStartChild()

    /// <summary>
    /// Async computation expression which allows for parallel execution of asyncs with the applicative (and!) syntax.  This this <c>Async.StartImmediateAsTask</c> to start  async computations in parallel.
    /// </summary>
    /// <returns></returns>
    let parallelAsyncUsingStartImmediateAsTask =
        ParallelAsyncBuilderUsingStartImmediateAsTask()

    /// <summary>
    /// Async computation expression which allows for parallel execution of asyncs with the applicative (and!) syntax.  This this <c>Async.StartImmediateAsTask</c> to start async computations in parallel.
    /// </summary>
    /// <returns></returns>
    let parallelAsync = parallelAsyncUsingStartImmediateAsTask
