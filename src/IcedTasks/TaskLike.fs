namespace IcedTasks

open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.FSharp.Core.CompilerServices
open System.Threading
open System

/// <namespacedoc>
///   <summary>
///     Contains core functionality for
///     <see cref='T:IcedTasks.ValueTasks'/>, <see cref='T:IcedTasks.ParallelAsync'/>,
///     <see cref='T:IcedTasks.ColdTasks'/>, <see cref='T:IcedTasks.CancellableTasks'/>,
///     <see cref='T:IcedTasks.CancellableValueTasks'/>.
///     </summary>
/// </namespacedoc>
///
/// A structure that looks like an Awaiter
type Awaiter<'Awaiter, 'TResult
    when 'Awaiter :> ICriticalNotifyCompletion
    and 'Awaiter: (member get_IsCompleted: unit -> bool)
    and 'Awaiter: (member GetResult: unit -> 'TResult)> = 'Awaiter

/// Functions for Awaiters
type Awaiter =
    /// Gets a value that indicates whether the asynchronous task has completed
    static member inline IsCompleted<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter)
        =
        awaiter.get_IsCompleted ()

    /// Ends the wait for the completion of the asynchronous task.
    static member inline GetResult<'Awaiter, 'TResult when Awaiter<'Awaiter, 'TResult>>
        (awaiter: 'Awaiter)
        =
        awaiter.GetResult()


    /// Schedules the continuation action that's invoked when the instance completes
    static member inline OnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (
            awaiter: 'Awaiter,
            continuation: System.Action
        ) =
        awaiter.OnCompleted(continuation)

    /// Schedules the continuation action that's invoked when the instance completes.
    static member inline UnsafeOnCompleted<'Awaiter, 'TResult, 'Continuation
        when Awaiter<'Awaiter, 'TResult>>
        (
            awaiter: 'Awaiter,
            continuation: System.Action
        ) =
        awaiter.UnsafeOnCompleted(continuation)

/// A structure looks like an Awaitable
type Awaitable<'Awaitable, 'Awaiter, 'TResult
    when 'Awaitable: (member GetAwaiter: unit -> Awaiter<'Awaiter, 'TResult>)> = 'Awaitable

/// <summary>Functions for Awaitables</summary>
type Awaitable =

    /// <summary>Creates an awaiter for this value.</summary>
    static member inline GetAwaiter(x: Task<'T>) = x.GetAwaiter()


    /// <summary>Creates an awaiter for this value.</summary>
    static member inline GetAwaiter([<InlineIfLambda>] x: unit -> Task<'T>) =
        (fun () -> (x ()).GetAwaiter())

    /// <summary>Creates an awaiter for this value.</summary>
    static member inline GetAwaiter([<InlineIfLambda>] x: CancellationToken -> Task<'T>) =
        (fun ct -> (x ct).GetAwaiter())

[<AutoOpen>]
module LowerPriorityAwaitable =
    type Awaitable with

        /// <summary>Creates an awaiter for this value.</summary>
        [<NoEagerConstraintApplication>]
        static member inline GetAwaiter<'Awaitable, 'Awaiter, 'TResult
            when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
            (x: 'Awaitable)
            =
            x.GetAwaiter()

        /// <summary>Creates an awaiter for this value.</summary>
        [<NoEagerConstraintApplication>]
        static member inline GetAwaiter<'Awaitable, 'Awaiter, 'TResult
            when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
            (x: unit -> 'Awaitable)
            =
            fun () -> (x ()).GetAwaiter()

        /// <summary>Creates an awaiter for this value.</summary>
        [<NoEagerConstraintApplication>]
        static member inline GetAwaiter<'Awaitable, 'Awaiter, 'TResult
            when Awaitable<'Awaitable, 'Awaiter, 'TResult>>
            (x: CancellationToken -> 'Awaitable)
            =
            fun ct -> (x ct).GetAwaiter()


type DelegateShape<'this, 'Input, 'Output when 'this: (member Invoke: 'Input -> 'Output)> = 'this

// type DelegateAwaitable<'T, 'Awaiter, 'TResult, 'Input when DelegateShape<Awaitable<'T, 'Awaiter, 'TResult>, 'Input, 'TResult>> = 'T

// type DelegateAwaitable<'T, 'Input, 'Output, 'Awaiter, 'TResult
//     when DelegateShape<'T, 'Input, 'Output>
//     and 'Output: (member GetAwaiter: unit -> Awaiter<'Awaiter, 'TResult>)> = 'T


type DelegateAwaiter<'T, 'Input, 'Output, 'TResult
    when DelegateShape<'T, 'Input, 'Output> and Awaiter<'Output, 'TResult>> = 'T

type DelegateAwaiter2<'T, 'Input, 'Output, 'TResult
    when DelegateShape<'T, 'Input, 'Output> and Awaiter<'Output, 'TResult>> = 'T


type DelegateAwaitable<'T, 'Input, 'Output, 'Awaiter, 'TResult
    when DelegateShape<'T, 'Input, 'Output> and Awaitable<'Output, 'Awaiter, 'TResult>> = 'T


type MyFooDelegate = delegate of int -> int

type SystemAction<'T> = delegate of byref<int> -> unit

type CancellableTask = CancellationToken -> Task
type CancellableTask<'T> = CancellationToken -> Task<'T>

type CancellableAwaitable<'Awaitable, 'Awaiter, 'TResult
    when Awaitable<'Awaitable, 'Awaiter, 'TResult>> = CancellationToken -> 'Awaitable

type CancellableTaskDelegate = delegate of CancellationToken -> Task
type CancellableTaskDelegate<'T> = delegate of CancellationToken -> Task<'T>
// type CancellableAwaitableDelegate<'Awaitable, 'Awaiter, 'TResult when Awaitable<'Awaitable, 'Awaiter, 'TResult>> =  delegate of CancellationToken -> 'Awaitable

// type CancellableAwaitableDelegate<'Awaitable, 'Awaiter, 'TResult
//     when Awaitable<'Awaitable, 'Awaiter, 'TResult>> = System.Func<CancellationToken * 'Awaitable>

// [<Struct; IsByRefLike>]
// type CancellableAwaitableDelegateByRef<'T, 'Awaitable, 'Awaiter, 'TResult
//     when 'T: (member Invoke: CancellationToken -> Awaitable<'Awaitable, 'Awaiter, 'TResult>)>
//     (item: 'T) =
//     member inline x.Invoke(ct: CancellationToken) = item.Invoke(ct)

[<Struct>]
type CancellableAwaitableDelegate<'T, 'Awaitable, 'Awaiter, 'TResult
    when 'T: (member Invoke: CancellationToken -> Awaitable<'Awaitable, 'Awaiter, 'TResult>)> =
    struct
        [<DefaultValue(false)>]
        val mutable _Value: 'T

        member inline x.Invoke(ct: CancellationToken) = x._Value.Invoke(ct)

        static member inline Create<'T, 'Awaitable, 'Awaiter, 'TResult>(value: 'T) =
            CancellableAwaitableDelegate<'T, 'Awaitable, 'Awaiter, 'TResult>(_Value = value)

    // static member inline Create2([<InlineIfLambda>] value: CancellationToken -> 'Awaitable) =
    //     CancellableAwaitableDelegate<'T, 'Awaitable, 'Awaiter, 'TResult>(_Value = Func<_,_>(value))


    // static member inline Create3([<InlineIfLambda>] value: Converter<CancellationToken,'Awaitable>) =
    //     CancellableAwaitableDelegate<'T, 'Awaitable, 'Awaiter, 'TResult>(_Value = (value))


    end


// [<Struct>]
// type Class5<'T when 'T: (member Method1: 'T -> int)> =
//     struct
//         [<DefaultValue(false)>]
//         val mutable Value: 'T
//         // new(value) = { Value = value }
//         member inline x.Method2(y: 'T) = x.Value.Method1(y)
//         static member inline Create(value: 'T) = Class5<'T>(Value = value)
//     end

module Delegators =
    open System

    let inline invoke<'Delegate, 'Input, 'Output when DelegateShape<'Delegate, 'Input, 'Output>>
        (d: 'Delegate)
        (x: 'Input)
        =
        d.Invoke(x)

    let foo = MyFooDelegate(fun x -> x + 1)

    // let answer = invoke (foo) 41

    let inline invokeAwaitableByRef<'Delegate, 'Input, 'Output, 'Awaiter, 'TResult
        when DelegateAwaitable<'Delegate, 'Input, 'Output, 'Awaiter, 'TResult>>
        (d: byref<'Delegate>)
        (x: 'Input)
        =
        d.Invoke(x).GetAwaiter()

    let inline invokeAwaitable<'Delegate, 'Input, 'Output, 'Awaiter, 'TResult
        when DelegateAwaitable<'Delegate, 'Input, 'Output, 'Awaiter, 'TResult>>
        (d: 'Delegate)
        (x: 'Input)
        =
        d.Invoke(x).GetAwaiter()


    let inline doThing ([<InlineIfLambda>] x: int -> int) = x

    let ctVt: CancellationToken -> ValueTask<int> =
        fun (x: CancellationToken) -> ValueTask<int>(1 + 1)

    let tryIt () =
        let mutable foo2 = CancellableAwaitableDelegate.Create(Func<_, _>(ctVt))

        let foo3 =
            CancellableAwaitableDelegate.Create2(fun (x: CancellationToken) ->
                ValueTask<int>(1 + 1)
            )

        let answer2 = invokeAwaitableByRef (&foo2) CancellationToken.None

        answer2
        |> Awaiter.GetResult


// let tryIt2 () =
//     let foo2 = CancellableAwaitableDelegate(Func<_, _>(fun x -> ValueTask<int>(1 + 1)))

//     let answer2 = invokeAwaitable (foo2) CancellationToken.None
//     answer2

// let inline DoThing<'T when 'T: delegate<int, int>> ([<InlineIfLambda>]x: 'T) = ()

// type MyFoo<'Input, 'Output>() =
//     inherit FSharpFunc<'Input, Task<'Output>>()

//     override x.Invoke(input: 'Input) = task { return Unchecked.defaultof<'Output> }

// module LOL =
//     let inline executeFoo (foo: MyFoo<int, int>) =

//         Delegators.invoke foo 42

// type CancellableTask<'T> = CancellationToken -> Task<'T>
// type CancellableTaskDelegate<'T> = delegate of CancellationToken -> Task<'T>
type CancellableTaskFunc<'T> = Func<CancellationToken, Task<'T>>


module Invokers =

    let inline invokeFSharpFunc x (f: CancellableTask<'T>) = f x
    let inline invokeDelegate x (f: CancellableTaskDelegate<'T>) = f.Invoke x
    let inline invokeFunc x (f: CancellableTaskFunc<'T>) = f.Invoke x


module Examples =

    let ex1 (x: int) =

        let result = x + 42
        let doThing = fun (ct: CancellationToken) -> Task.FromResult(result)
        Invokers.invokeFSharpFunc CancellationToken.None doThing


    let ex2 (x: int) =
        let result = x + 42

        let doThing =
            CancellableTaskDelegate<_>(fun (ct: CancellationToken) -> Task.FromResult(result))

        Invokers.invokeDelegate CancellationToken.None doThing


    let ex3 (x: int) =
        let result = x + 42

        let doThing =
            CancellableTaskFunc<_>(fun (ct: CancellationToken) -> Task.FromResult(result))

        Invokers.invokeFunc CancellationToken.None doThing
