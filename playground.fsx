type CollectionBuilderCode<'T> = delegate of byref<'T> -> unit

type CollectionBuilder() =
    member inline _.Combine
        (
            [<InlineIfLambda>] f1: CollectionBuilderCode<_>,
            [<InlineIfLambda>] f2: CollectionBuilderCode<_>
        ) =
        CollectionBuilderCode(fun sm ->
            f1.Invoke &sm
            f2.Invoke &sm
        )

    member inline _.Delay([<InlineIfLambda>] f: unit -> CollectionBuilderCode<_>) =
        CollectionBuilderCode(fun sm -> (f ()).Invoke &sm)

    member inline _.Zero() = CollectionBuilderCode(fun _ -> ())

    member inline _.Yield x =
        CollectionBuilderCode(fun sm -> ignore (^a: (member Add: ^b -> _) (sm, x)))

    member inline _.For((lo, hi), [<InlineIfLambda>] body: _ -> CollectionBuilderCode<_>) =
        CollectionBuilderCode(fun sm ->
            for i in lo..hi do
                (body i).Invoke &sm
        )

    member inline _.Run([<InlineIfLambda>] f: CollectionBuilderCode<_>) =
        let mutable sm = ResizeArray<'T>()
        f.Invoke &sm
        sm

    member inline _.Source((lo, hi)) = lo, hi
    member inline _.Source(range: System.Range) = range.Start.Value, range.End.Value

let builder = CollectionBuilder()

//let inline (..) lo hi = lo, hi

let asdf = builder { for x in 1..10 -> x }


let foo = [ 1..10 ]
