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
    "net11.0"
#else
    "net9.0"
#endif
#endif
#endif

let private libraryTargetFramework () =
    match icedTasksAssembly.GetCustomAttributes(typeof<TargetFrameworkAttribute>, false) with
    | [| :? TargetFrameworkAttribute as attribute |] ->
        match attribute.FrameworkName with
        | ".NETCoreApp,Version=v11.0" -> "net11.0"
        | ".NETCoreApp,Version=v9.0" -> "net9.0"
        | ".NETStandard,Version=v2.1" -> "netstandard2.1"
        | ".NETStandard,Version=v2.0" -> "netstandard2.0"
        | other ->
            failwith $"No 0.11.9 API baseline exists for the IcedTasks build that targets {other}."
    | _ -> failwith "The IcedTasks assembly has no TargetFrameworkAttribute."

// IcedTasks 0.11.9 has no net11.0 build, so a net11.0 consumer of 0.11.9 used the net9.0 build.
let private baselineTargetFramework targetFramework =
    match targetFramework with
    | "net11.0" -> "net9.0"
    | other -> other

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

let private metadataTargetFramework (assembly: Reflection.Assembly) =
    assembly.GetCustomAttributesData()
    |> Seq.tryFind (fun a -> a.AttributeType.FullName = typeof<TargetFrameworkAttribute>.FullName)
    |> Option.map (fun a -> string a.ConstructorArguments[0].Value)

let private frameworkName (targetFramework: string) =
    if targetFramework.StartsWith "netstandard" then
        $".NETStandard,Version=v{targetFramework.Substring 11}"
    else
        $".NETCoreApp,Version=v{targetFramework.Substring 3}"

#if TEST_NET6_0_OR_GREATER
let private shippedFrameworks =
    readEntries (publicApiFile "shipped-target-frameworks.txt")
    |> List.ofSeq

let private shippedBuild targetFramework =
    publicApiFile (Path.Combine(targetFramework, "IcedTasks.dll"))

let private shippedDependencies targetFramework = [
    AppContext.BaseDirectory
    Path.GetDirectoryName(shippedBuild targetFramework)
]
#endif

let private compareWithBaseline
    (assemblyPath: string)
    (dependencyDirectories: string list)
    (targetFramework: string)
    =
    let baselineFramework = baselineTargetFramework targetFramework

    let baseline =
        readEntries (publicApiFile $"IcedTasks-0.11.9-{baselineFramework}.txt")

    Expect.isNonEmpty baseline $"The 0.11.9 {baselineFramework} baseline file must exist."

    let additions = readEntries (publicApiFile $"additions-{targetFramework}.txt")

    let actual =
        PublicApiListing.withMetadata assemblyPath dependencyDirectories PublicApiListing.describe
        |> Set.ofList

    let missing = Set.difference baseline actual
    let added = Set.difference actual baseline
    let unlisted = Set.difference added additions
    let staleAdditions = Set.difference additions added

    let report = [
        if not missing.IsEmpty then
            $"""Entries of the 0.11.9 {baselineFramework} API that the {targetFramework} build does not have:{Environment.NewLine}{lines "-" missing}"""
        if not unlisted.IsEmpty then
            $"""Entries that the {targetFramework} build adds and that additions-{targetFramework}.txt does not list:{Environment.NewLine}{lines "+" unlisted}"""
        if not staleAdditions.IsEmpty then
            $"""Entries of additions-{targetFramework}.txt that the build does not add:{Environment.NewLine}{lines "?" staleAdditions}"""
    ]

    Expect.isEmpty report (String.concat Environment.NewLine report)

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
            compareWithBaseline
                icedTasksAssembly.Location
                [ AppContext.BaseDirectory ]
                (libraryTargetFramework ())

#if TEST_NET6_0_OR_GREATER
        testCase "the test output has each build that IcedTasks.fsproj ships"
        <| fun () ->
            Expect.isNonEmpty
                shippedFrameworks
                "IcedTasks.Tests.fsproj must write PublicApi/shipped-target-frameworks.txt from IcedTasks.fsproj."

            for targetFramework in shippedFrameworks do
                let path = shippedBuild targetFramework

                Expect.isTrue
                    (File.Exists path)
                    $"IcedTasks.Tests.fsproj must copy the {targetFramework} build of IcedTasks to PublicApi/{targetFramework}."

                Expect.equal
                    (PublicApiListing.withMetadata
                        path
                        (shippedDependencies targetFramework)
                        metadataTargetFramework)
                    (Some(frameworkName targetFramework))
                    $"PublicApi/{targetFramework}/IcedTasks.dll must be the {targetFramework} build."

        testCase "each target framework of IcedTasks 0.11.9 is still shipped"
        <| fun () ->
            let released =
                Directory.GetFiles(
                    Path.Combine(AppContext.BaseDirectory, "PublicApi"),
                    "IcedTasks-0.11.9-*.txt"
                )
                |> Array.map (fun path ->
                    Path.GetFileNameWithoutExtension(path).Substring("IcedTasks-0.11.9-".Length)
                )
                |> Set.ofArray

            Expect.isNonEmpty released "The 0.11.9 API lists must be in the test output."

            Expect.isEmpty
                (Set.difference released (Set.ofList shippedFrameworks))
                "IcedTasks.fsproj must ship each target framework of IcedTasks 0.11.9."

        testList "each shipped build keeps each IcedTasks 0.11.9 entry and adds only listed entries" [
            for targetFramework in shippedFrameworks ->
                testCase targetFramework
                <| fun () ->
                    compareWithBaseline
                        (shippedBuild targetFramework)
                        (shippedDependencies targetFramework)
                        targetFramework
        ]
#endif
    ]
