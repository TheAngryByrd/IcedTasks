open System

// Replace with your type
type MyType<'a> = Async<'a>
type Internal<'a> = MyType<'a>

// Replace with types that feel similar and can be converted to `MyType`
type External1<'a> = System.Threading.Tasks.Task<'a>
type External2<'a> = System.Threading.Tasks.ValueTask<'a>

/// https://fsharpforfunandprofit.com/posts/computation-expressions-builder-part3/
type Delayed<'a> = unit -> MyType<'a>

// What is InlineIfLambda? This allows generation of high performance code which Computation Expressions have had in the past.
// See https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1098-inline-if-lambda.md
// We also mark everything an inline to try to squeeze as much performance out of the implementation as possible.


/// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions#creating-a-new-type-of-computation-expression
type StubBuilder() =

    /// Called for let! and do! in computation expressions.
    member inline this.Bind
        (
            input: Internal<'T>,
            [<InlineIfLambda>] f: ('T -> Internal<'U>)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for efficient let! and and! in computation expressions without merging inputs.
    member inline this.Bind2
        (
            input: Internal<'T1>,
            input2: Internal<'T2>,
            [<InlineIfLambda>] f: ('T1 * 'T2 -> Internal<'U>)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for efficient let! and and! in computation expressions without merging inputs.
    member inline this.Bind3
        (
            input: Internal<'T1>,
            input2: Internal<'T2>,
            input3: Internal<'T3>,
            [<InlineIfLambda>] f: ('T1 * 'T2 * 'T3 -> Internal<'U>)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for return in computation expressions.
    member inline this.Return(input: 'T) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for return! in computation expressions.
    member inline this.ReturnFrom(input: Internal<'T>) : Internal<'T> = input

    /// Called for an efficient let! ... return in computation expressions.
    member inline this.BindReturn
        (
            input: Internal<'T>,
            [<InlineIfLambda>] f: ('T -> 'U)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for efficient let! ... and! ... return in computation expressions without merging inputs.
    member inline this.Bind2Return
        (
            input: Internal<'T1>,
            input2: Internal<'T2>,
            [<InlineIfLambda>] f: ('T1 * 'T2 -> 'U)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for efficient let! ... and! ... return in computation expressions without merging inputs.
    member inline this.Bind3Return
        (
            input: Internal<'T1>,
            input2: Internal<'T2>,
            input3: Internal<'T3>,
            [<InlineIfLambda>] f: ('T1 * 'T2 * 'T3 -> 'U)
        ) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for and! in computation expressions.
    member inline this.MergeSources
        (
            input: Internal<'T1>,
            input2: Internal<'T2>
        ) : Internal<'T1 * 'T2> =
        raise
        <| NotImplementedException()

    /// Called for and! in computation expressions, but improves efficiency by reducing the number of tupling nodes.
    member inline this.MergeSources3
        (
            input: Internal<'T1>,
            input2: Internal<'T2>,
            input3: Internal<'T3>
        ) : Internal<'T1 * 'T2 * 'T3> =
        raise
        <| NotImplementedException()

    /// Wraps a computation expression as a function. Delayed<'T> can be any type, commonly Internal<'T> or unit -> Internal<'T> are used.
    /// Many functions use the result of Delay as an argument: Run, While, TryWith, TryFinally, and Combine
    member inline this.Delay([<InlineIfLambda>] f: unit -> Internal<'T>) : Delayed<'T> =
        raise
        <| NotImplementedException()

    /// Executes a computation expression.
    member inline this.Run([<InlineIfLambda>] f: Delayed<'T>) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for sequencing in computation expressions.
    member inline this.Combine
        (
            input: Internal<'T>,
            [<InlineIfLambda>] f: Delayed<'T>
        ) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for sequencing in computation expressions.
    member inline this.Combine(input: Internal<unit>, f: Internal<'T>) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for while...do expressions in computation expressions.
    member inline this.While
        (
            [<InlineIfLambda>] guard: unit -> bool,
            [<InlineIfLambda>] body: Delayed<'T>
        ) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for while...do expressions in computation expressions.
    member inline this.While
        (
            [<InlineIfLambda>] guard: unit -> bool,
            [<InlineIfLambda>] body: Delayed<unit>
        ) : Internal<unit> =
        raise
        <| NotImplementedException()

    /// Called for for...do expressions in computation expressions.
    member inline this.For(xs: #seq<'T>, [<InlineIfLambda>] f: 'T -> Internal<'U>) : Internal<'U> =
        raise
        <| NotImplementedException()

    /// Called for for...do expressions in computation expressions.
    // Only need one of these For implementations
    // member inline this.For(xs : #seq<'T>, [<InlineIfLambda>] f : 'T -> Internal<'U> ) : seq<Internal<'U>> =
    //     raise <| NotImplementedException()

    /// Called for try...finally expressions in computation expressions.
    member inline this.TryFinally
        (
            [<InlineIfLambda>] body: Delayed<'T>,
            [<InlineIfLambda>] final: unit -> unit
        ) : Internal<'T> =
        raise
        <| NotImplementedException()

    /// Called for try...finally expressions in computation expressions.
    member inline this.TryWith
        (
            [<InlineIfLambda>] body: Delayed<'T>,
            [<InlineIfLambda>] failure: exn -> Internal<'T>
        ) : Internal<'T> =
        raise
        <| NotImplementedException()


    /// Called for use bindings in computation expressions.
    member inline this.Using
        (
            resource: 'T :> IDisposable,
            [<InlineIfLambda>] f: 'T -> Internal<'U>
        ) : Internal<'U> =
        // In extensions because of the priority resolution discussed above
        // Otherwise you get a "Duplicate method. The method 'Using' has the same name and signature as another method in type 'StubBuilder'"
        // Reason is, .NET cannot have overloaded methods based on constraints alone
        // This can be moved to main builder if IAsyncDisposable is not required
        raise
        <| NotImplementedException()


    /// Called for empty else branches of if...then expressions in computation expressions.
    member inline _.Zero() : Internal<'a> =
        raise
        <| NotImplementedException()

    /// Called to convert an External to Internal type before any others calls in computation expression.
    /// This is the identity function for the internal type
    member inline _.Source(identity: Internal<'a>) : Internal<'a> = identity

    /// Called to convert an External to Internal type before any others calls in computation expression.
    /// This is the identity function for for loops.
    member inline _.Source(identity: #seq<'a>) : #seq<'a> = identity

    /// Called to convert an External to Internal type before any others calls in computation expression.
    member inline _.Source(other: External1<Option<'a>>) : Internal<'a> =
        raise
        <| NotImplementedException()

    /// Called to convert an External to Internal type before any others calls in computation expression.
    member inline _.Source(other: External2<'a>) : Internal<'a> =
        raise
        <| NotImplementedException()

/// F#'s overloading resolution prioritizes extension methods lower
/// so if you have a more Derived type like `Async<Option<'T>>` vs `Async<'T>`, it won't know
/// which one to choose. You'll want to have the more specific in the Builder itself
/// and the less specific in an extension method.
///
/// See: https://github.com/fsharp/fslang-suggestions/issues/905
[<AutoOpen>]
module StubBuilderExtension =

    let stub = StubBuilder()

    type StubBuilder with

        /// Called to convert an External to Internal type before any others calls in computation expression.
        member inline _.Source(other: External1<'a>) : Internal<'a> =
            raise
            <| NotImplementedException()

        /// Called for use bindings in computation expressions.
        member inline this.Using
            (
                resource: 'T :> IAsyncDisposable,
                [<InlineIfLambda>] f: 'T -> Internal<'U>
            ) : Internal<'U> =
            // This may not need to be implemented for non async-like CEs and can be replaced by the Using in the Extensions module
            raise
            <| NotImplementedException()

module Example =
    open System.Threading.Tasks

    let bindExamples () = stub {
        let! result = async { return "lol" } // string -> Used Normal Bind
        let! result = task { return Some "lol" } // string -> Used Source member External1<Option<'a>>
        let! result = task { return "lol" } // string -> Used Source member External1<'a> that is an extension member for lower binding resolution.
        and! result4 = ValueTask<string>("lol") // string -> Used Source member Extenrnal2<'a>
        return "lol"
    }

    type DisposableExample() =

        interface IDisposable with
            member this.Dispose() : unit = printfn "disposed"

    let disposeExample () = stub {
        use d = new DisposableExample()
        return "lol"
    }

    type AsyncDisposableExample() =
        interface IAsyncDisposable with
            member this.DisposeAsync() : ValueTask =
                printfn "disposed"
                ValueTask()

    let asyncDisposeExample () = stub {
        use d = new AsyncDisposableExample()
        return "lol"
    }


    type BothDispose() =

        interface IDisposable with
            member this.Dispose() : unit = printfn "disposed"

        interface IAsyncDisposable with
            member this.DisposeAsync() : ValueTask =
                printfn "disposed"
                ValueTask()

    let bothDisposeExample () = stub {
        use d = new BothDispose()
        return "lol"
    }
