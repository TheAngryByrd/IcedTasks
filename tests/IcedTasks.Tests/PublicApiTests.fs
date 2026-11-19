module IcedTasksConsumer.PublicApiTests

open System
open System.IO
open System.Reflection
open System.Runtime.Versioning
open Expecto

let private icedTasksAssembly = typeof<IcedTasks.ColdTasks.ColdTaskBuilder>.Assembly

let private libraryTargetFramework () =
    let attribute = icedTasksAssembly.GetCustomAttribute<TargetFrameworkAttribute>()

    match attribute.FrameworkName with
    | ".NETCoreApp,Version=v9.0" -> "net9.0"
    | ".NETCoreApp,Version=v6.0" -> "net6.0"
    | ".NETStandard,Version=v2.1" -> "netstandard2.1"
    | ".NETStandard,Version=v2.0" -> "netstandard2.0"
    | other ->
        failwith $"No 0.11.9 API baseline exists for the IcedTasks build that targets {other}."

let private memberFlags =
    BindingFlags.Public
    ||| BindingFlags.Static
    ||| BindingFlags.Instance
    ||| BindingFlags.DeclaredOnly

let rec private typeName (t: Type) =
    if t.IsGenericParameter then
        t.Name
    elif t.IsArray then
        typeName (t.GetElementType())
        + "[]"
    elif t.IsByRef then
        typeName (t.GetElementType())
        + "&"
    elif t.IsGenericType then
        let definition = t.GetGenericTypeDefinition().FullName
        let name = definition.Substring(0, definition.IndexOf '`')

        let arguments =
            t.GetGenericArguments()
            |> Array.map typeName
            |> String.concat ","

        $"{name}<{arguments}>"
    else
        t.FullName

let private parameterList (parameters: ParameterInfo array) =
    parameters
    |> Array.map (fun p -> typeName p.ParameterType)
    |> String.concat ","

let private describe (assembly: Assembly) =
    [
        for t in assembly.GetExportedTypes() do
            yield $"type {t.FullName}"

            // Debug builds emit public compiler-generated methods such as <GetAwaiter>__debug@328-22.
            for m in
                t.GetMembers memberFlags
                |> Array.filter (fun m -> not (m.Name.StartsWith "<")) do
                match m with
                | :? MethodInfo as mi when not mi.IsSpecialName ->
                    yield $"method {t.FullName}::{mi.Name}({parameterList (mi.GetParameters())})"
                | :? PropertyInfo as pi ->
                    yield $"property {t.FullName}::{pi.Name}: {typeName pi.PropertyType}"
                | :? FieldInfo as fi -> yield $"field {t.FullName}::{fi.Name}"
                | :? ConstructorInfo as ci ->
                    yield $"ctor {t.FullName}({parameterList (ci.GetParameters())})"
                | _ -> ()
    ]
    |> List.distinct
    |> List.sort

// Runtime reflection throws TypeLoadException on some IcedTasks signatures, so the listing reads metadata only.
let private currentApi () =
    let runtimeDirectory =
        Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()

    let paths =
        Seq.concat [
            Directory.GetFiles(runtimeDirectory, "*.dll")
            Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
        ]

    use context = new MetadataLoadContext(PathAssemblyResolver(paths))

    context.LoadFromAssemblyPath icedTasksAssembly.Location
    |> describe

let private baseline targetFramework =
    Path.Combine(AppContext.BaseDirectory, "PublicApi", $"IcedTasks-0.11.9-{targetFramework}.txt")
    |> File.ReadAllLines
    |> Array.filter (
        String.IsNullOrWhiteSpace
        >> not
    )
    |> List.ofArray

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

        testCase "the public API is the same as IcedTasks 0.11.9"
        <| fun () ->
            let targetFramework = libraryTargetFramework ()
            let expected = baseline targetFramework
            let actual = currentApi ()

            let missing =
                Set.difference (Set.ofList expected) (Set.ofList actual)
                |> Set.toList

            let added =
                Set.difference (Set.ofList actual) (Set.ofList expected)
                |> Set.toList

            let lines prefix (items: string list) =
                items
                |> List.map (fun item -> $"{prefix} {item}")
                |> String.concat Environment.NewLine

            let missingLines = lines "-" missing
            let addedLines = lines "+" added

            Expect.isEmpty
                missing
                $"Entries of the 0.11.9 {targetFramework} API that the build does not have:{Environment.NewLine}{missingLines}"

            Expect.isEmpty
                added
                $"Entries that the {targetFramework} build adds to the 0.11.9 API:{Environment.NewLine}{addedLines}"
    ]
