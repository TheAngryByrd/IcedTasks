namespace IcedTasks

open System

type private Async =
    static member inline map f x =
        async.Bind(x, (fun v -> async.Return(f v)))


    /// <summary>Creates an Async that runs computation. The action compensation is executed
    /// after computation completes, whether computation exits normally or by an exception. If compensation raises an exception itself
    /// the original exception is discarded and the new exception becomes the overall result of the computation.</summary>
    /// <param name="computation">The input computation.</param>
    /// <param name="compensation">The action to be run after computation completes or raises an
    /// exception (including cancellation).</param>
    /// <remarks> <see href="http://www.fssnip.net/ru/title/Async-workflow-with-asynchronous-finally-clause">See this F# gist</see></remarks>
    /// <returns>An async with the result of the computation.</returns>
    static member inline TryFinallyAsync
        (
            computation: Async<'T>,
            compensation: Async<unit>
        ) : Async<'T> =

        let finish (compResult, deferredResult) (onNext, (onError: exn -> unit), onCancel) =
            match (compResult, deferredResult) with
            | (Choice1Of3 x, Choice1Of3()) -> onNext x
            | (Choice2Of3 compExn, Choice1Of3()) -> onError compExn
            | (Choice3Of3 compExn, Choice1Of3()) -> onCancel compExn
            | (Choice1Of3 _, Choice2Of3 deferredExn) -> onError deferredExn
            | (Choice2Of3 compExn, Choice2Of3 deferredExn) ->
                onError
                <| new AggregateException(compExn, deferredExn)
            | (Choice3Of3 compExn, Choice2Of3 deferredExn) -> onError deferredExn
            | (_, Choice3Of3 deferredExn) ->
                onError
                <| new Exception("Unexpected cancellation.", deferredExn)

        let startDeferred compResult (onNext, onError, onCancel) =
            Async.StartWithContinuations(
                compensation,
                (fun () -> finish (compResult, Choice1Of3()) (onNext, onError, onCancel)),
                (fun exn -> finish (compResult, Choice2Of3 exn) (onNext, onError, onCancel)),
                (fun exn -> finish (compResult, Choice3Of3 exn) (onNext, onError, onCancel))
            )

        let startComp ct (onNext, onError, onCancel) =
            Async.StartWithContinuations(
                computation,
                (fun x -> startDeferred (Choice1Of3(x)) (onNext, onError, onCancel)),
                (fun exn -> startDeferred (Choice2Of3 exn) (onNext, onError, onCancel)),
                (fun exn -> startDeferred (Choice3Of3 exn) (onNext, onError, onCancel)),
                ct
            )

        async {
            let! ct = Async.CancellationToken
            return! Async.FromContinuations(startComp ct)
        }
