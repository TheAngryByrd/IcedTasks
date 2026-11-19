module NoOpenConsumer.FSharpCoreTaskTests

open System.IO
open System.Threading.Tasks
open Expecto

let get tryCacheResponse blobName stream =
    task { do! tryCacheResponse blobName stream }

let addBoth getLeft getRight =
    task {
        let! left = getLeft ()
        let! right = getRight ()

        return
            left
            + right
    }

let bindUnannotated value =
    task {
        let! result = value
        return result
    }

[<Tests>]
let tests =
    testList "FSharp.Core task without an IcedTasks open" [
        testCaseTask "do! binds an unannotated function result"
        <| fun () ->
            task {
                let received = ResizeArray()

                let tryCacheResponse (name: string) (stream: Stream) =
                    received.Add(name, stream.Length)
                    Task.FromResult()

                use stream =
                    new MemoryStream(
                        [|
                            1uy
                            2uy
                        |]
                    )

                do! get tryCacheResponse "blob" stream

                Expect.equal
                    (List.ofSeq received)
                    [ ("blob", 2L) ]
                    "get passes its arguments to tryCacheResponse"
            }

        testCaseTask "let! binds unannotated values"
        <| fun () ->
            task {
                let! sum = addBoth (fun () -> Task.FromResult 1) (fun () -> Task.FromResult 2)
                Expect.equal sum 3 "let! binds each Task<int>"

                let! value = bindUnannotated (Task.FromResult "value")
                Expect.equal value "value" "let! binds the Task<string> argument"
            }
    ]
