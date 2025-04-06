open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions

// This script is used to generate the .fsx files that are used to load the .NET SDK.
// It will generate a .fsx file for each version of the .NET SDK that is installed.
// It will also generate a .fsx file for the latest major version of the .NET SDK to make referencing less brittle.

#r "nuget: semver"

open Semver
open System.Linq

type Runtime = {
    Name: string
    Version: SemVersion
    Path: DirectoryInfo
}

let getRuntimeList () =
    // You can see which versions of the .NET runtime are currently installed with the following command.

    let psi =
        ProcessStartInfo("dotnet", "--list-runtimes", RedirectStandardOutput = true)

    use proc = Process.Start(psi)

    let output =
        seq {
            while not proc.StandardOutput.EndOfStream do
                proc.StandardOutput.ReadLine()
        }

    /// Regex for output like: Microsoft.AspNetCore.App 5.0.13 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
    let listRuntimesRegex = Regex("([^\s]+) ([^\s]+) \[(.*?)\\]")

    let runtimes =
        output
        |> Seq.map (fun x ->
            let matches = listRuntimesRegex.Match(x)
            let (version: string) = matches.Groups.[2].Value

            {
                Name = matches.Groups.[1].Value
                Version = SemVersion.Parse(version, SemVersionStyles.Any)
                Path = DirectoryInfo(Path.Join(matches.Groups[3].Value, version))
            }
        )
        |> Seq.toArray

    proc.WaitForExit(3000)
    |> ignore

    runtimes


module Seq =
    let filterOut predicate =
        Seq.filter (
            predicate
            >> not
        )

    let filterOutAny predicates xs =
        xs
        |> filterOut (fun x ->
            predicates
            |> Seq.exists (fun f -> f x)
        )

module Array =
    open System.Collections.Generic

    [<CompiledName("MaxBy")>]
    let inline maxByC (comparer: IComparer<'U>) (projection: 'T -> 'U) (source: seq<'T>) : 'T =
        // checkNonNull "source" source
        use e = source.GetEnumerator()

        if not (e.MoveNext()) then
            invalidArg "source" ""

        let first = e.Current
        let mutable acc = projection first
        let mutable accv = first

        while e.MoveNext() do
            let currv = e.Current
            let curr = projection currv

            if comparer.Compare(acc, curr) > 0 then
                acc <- curr
                accv <- currv

        accv

let createRuntimeLoadScript blockedDlls (r: Runtime) =
    let dir = r.Path

    let isDLL (f: FileInfo) = f.Extension = ".dll"

    let tripleQuoted (s: string) = $"\"\"\"{s}\"\"\""

    let packageSource (source: string) = $"#I {tripleQuoted source}"

    let reference (ref: string) = $"#r \"{ref}\""

    let fileReferences =
        dir.GetFiles()
        |> Seq.filter isDLL
        |> Seq.filterOutAny blockedDlls
        |> Seq.map (fun f -> reference f.Name)

    let referenceContents = [|
        packageSource dir.FullName
        yield! fileReferences
    |]

    referenceContents

let writeReferencesToFile outputPath outputFileName referenceContents =
    Directory.CreateDirectory(outputPath)
    |> ignore

    let outputPath = Path.Join(outputPath, outputFileName)
    printfn "Writing to %s" outputPath

    File.WriteAllLines(outputPath, referenceContents)

let runtimeOutputNameByVersion r = $"{r.Name}-{r.Version.ToString()}.fsx"

let runtimeOuputNameByMajorVersion r =
    $"{r.Name}-latest-{r.Version.Major}.fsx"

let contains (x: string) (y: FileInfo) = y.Name.Contains x

// List of DLLs that FSI can't load
let blockedDlls = [
    contains "aspnetcorev2_inprocess"
    contains "api-ms-win"
    contains "clrjit"
    contains "clrgc"
    contains "clretwrc"
    contains "coreclr"
    contains "hostpolicy"
    contains "Microsoft.DiaSymReader.Native.amd64"
    contains "mscordaccore_amd64_amd64_7"
    contains "mscordaccore"
    contains "msquic"
    contains "mscordbi"
    contains "mscorrc"
    contains "System.IO.Compression.Native"
]


let runTimeLoadScripts =
    getRuntimeList ()
    |> Array.map (fun runtime -> runtime, createRuntimeLoadScript blockedDlls runtime)

let outputFolder = "runtime-scripts"

// print all by version
runTimeLoadScripts
|> Seq.iter (fun (r, referenceContents) ->
    writeReferencesToFile outputFolder (runtimeOutputNameByVersion r) referenceContents
)


// print all by major version
runTimeLoadScripts
|> Array.groupBy (fun (r, _) -> r.Name, r.Version.Major)
|> Array.map (fun (_, values) ->
    values
    |> Array.maxByC SemVersion.SortOrderComparer (fun (r, _) -> r.Version)
)
|> Array.iter (fun (r, referenceContents) ->
    writeReferencesToFile outputFolder (runtimeOuputNameByMajorVersion r) referenceContents
)
