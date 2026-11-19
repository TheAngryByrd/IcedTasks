module IcedTasksConsumer.PublicApiTests

open System
open System.IO
open System.Runtime.Versioning
open Expecto

let private icedTasksAssembly = typeof<IcedTasks.ColdTasks.ColdTaskBuilder>.Assembly

let private expectedLibraryTargetFramework =
#if TEST_NETSTANDARD2_0
    "netstandard2.0"
#else
#if TEST_NETSTANDARD2_1
    "netstandard2.1"
#else
#if NET11_0_OR_GREATER
    "net9.0"
#else
    "net6.0"
#endif
#endif
#endif

let private libraryTargetFramework () =
    match icedTasksAssembly.GetCustomAttributes(typeof<TargetFrameworkAttribute>, false) with
    | [| :? TargetFrameworkAttribute as attribute |] ->
        match attribute.FrameworkName with
        | ".NETCoreApp,Version=v9.0" -> "net9.0"
        | ".NETCoreApp,Version=v6.0" -> "net6.0"
        | ".NETStandard,Version=v2.1" -> "netstandard2.1"
        | ".NETStandard,Version=v2.0" -> "netstandard2.0"
        | other ->
            failwith $"No 0.11.9 API baseline exists for the IcedTasks build that targets {other}."
    | _ -> failwith "The IcedTasks assembly has no TargetFrameworkAttribute."

let private publicApiFile name =
    Path.Combine(AppContext.BaseDirectory, "PublicApi", name)

let private readEntries path =
    if File.Exists path then
        File.ReadAllLines path
        |> Array.filter (
            String.IsNullOrWhiteSpace
            >> not
        )
        |> Set.ofArray
    else
        Set.empty

let private lines prefix (items: string Set) =
    items
    |> Seq.map (fun item -> $"{prefix} {item}")
    |> String.concat Environment.NewLine

[<Tests>]
let tests =
    testList "Public API" [
        testCase "the assembly has no assembly-level AutoOpenAttribute"
        <| fun () ->
            let autoOpens =
                icedTasksAssembly.GetCustomAttributesData()
                |> Seq.filter (fun a -> a.AttributeType = typeof<AutoOpenAttribute>)
                |> Seq.map string
                |> List.ofSeq

            Expect.isEmpty
                autoOpens
                "An assembly-level AutoOpen opens its namespace in each file of each referencing project."

        testCase "the test loads the expected IcedTasks build"
        <| fun () ->
            Expect.equal
                (libraryTargetFramework ())
                expectedLibraryTargetFramework
                "Each test project must test its own IcedTasks target framework."

        testCase "the public API keeps each IcedTasks 0.11.9 entry and adds only listed entries"
        <| fun () ->
            let targetFramework = libraryTargetFramework ()

            let baseline = readEntries (publicApiFile $"IcedTasks-0.11.9-{targetFramework}.txt")

            Expect.isNonEmpty baseline $"The 0.11.9 {targetFramework} baseline file must exist."

            let additions = readEntries (publicApiFile $"additions-{targetFramework}.txt")

            let actual =
                PublicApiListing.withMetadata
                    icedTasksAssembly.Location
                    [ AppContext.BaseDirectory ]
                    PublicApiListing.describe
                |> Set.ofList

            let missing = Set.difference baseline actual
            let added = Set.difference actual baseline
            let unlisted = Set.difference added additions
            let staleAdditions = Set.difference additions added

            let report = [
                if not missing.IsEmpty then
                    $"""Entries of the 0.11.9 {targetFramework} API that the build does not have:{Environment.NewLine}{lines "-" missing}"""
                if not unlisted.IsEmpty then
                    $"""Entries that the {targetFramework} build adds and that additions-{targetFramework}.txt does not list:{Environment.NewLine}{lines "+" unlisted}"""
                if not staleAdditions.IsEmpty then
                    $"""Entries of additions-{targetFramework}.txt that the build does not add:{Environment.NewLine}{lines "?" staleAdditions}"""
            ]

            Expect.isEmpty report (String.concat Environment.NewLine report)
    ]
