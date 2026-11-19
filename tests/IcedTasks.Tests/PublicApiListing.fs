module IcedTasksConsumer.PublicApiListing

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions

let private memberFlags =
    BindingFlags.Public
    ||| BindingFlags.Static
    ||| BindingFlags.Instance
    ||| BindingFlags.DeclaredOnly

let private sourceAttributes =
    set [
        "Microsoft.FSharp.Core.AutoOpenAttribute"
        "Microsoft.FSharp.Core.RequireQualifiedAccessAttribute"
        "Microsoft.FSharp.Core.CompilationRepresentationAttribute"
        "System.Runtime.CompilerServices.ExtensionAttribute"
        "Microsoft.FSharp.Core.CompilationArgumentCountsAttribute"
    ]

let rec private typeName (t: Type) =
    if t.IsGenericParameter then
        t.Name
    elif t.IsArray then
        let rank = t.GetArrayRank()

        typeName (t.GetElementType())
        + "["
        + String(',', rank - 1)
        + "]"
    elif t.IsByRef then
        typeName (t.GetElementType())
        + "&"
    elif t.IsGenericType then
        let name = Regex.Replace(t.GetGenericTypeDefinition().FullName, "`[0-9]+", "")

        let arguments =
            t.GetGenericArguments()
            |> Array.map typeName
            |> String.concat ","

        $"{name}<{arguments}>"
    else
        t.FullName

let private genericParameter (t: Type) =
    let constraints =
        t.GetGenericParameterConstraints()
        |> Array.map typeName
        |> Array.sort
        |> String.concat ","

    let attributes =
        t.GenericParameterAttributes
        &&& GenericParameterAttributes.SpecialConstraintMask

    $"{t.Name}:{int attributes}:{constraints}"

let private genericParameters (types: Type array) =
    if Array.isEmpty types then
        ""
    else
        types
        |> Array.map genericParameter
        |> String.concat ";"
        |> sprintf "<%s>"

let private parameterList (parameters: ParameterInfo array) =
    parameters
    |> Array.map (fun p -> typeName p.ParameterType)
    |> String.concat ","

let rec private argumentText (value: obj) =
    match value with
    | :? Collections.Generic.IEnumerable<CustomAttributeTypedArgument> as items ->
        items
        |> Seq.map (fun item -> argumentText item.Value)
        |> String.concat ","
        |> sprintf "[%s]"
    | other -> string other

let private attributeList (attributes: CustomAttributeData seq) =
    attributes
    |> Seq.filter (fun a -> sourceAttributes.Contains a.AttributeType.FullName)
    |> Seq.map (fun a ->
        let arguments =
            a.ConstructorArguments
            |> Seq.map (fun argument -> argumentText argument.Value)
            |> String.concat ","

        $"{a.AttributeType.Name}({arguments})"
    )
    |> Seq.sort
    |> String.concat ";"

let private staticPrefix isStatic = if isStatic then "static " else ""

let private typeKind (t: Type) =
    if t.IsInterface then
        "interface"
    elif t.IsValueType then
        "struct"
    elif
        t.IsAbstract
        && t.IsSealed
    then
        "static class"
    elif t.IsAbstract then
        "abstract class"
    elif t.IsSealed then
        "sealed class"
    else
        "class"

let private baseTypeName (t: Type) =
    match t.BaseType with
    | null -> ""
    | baseType -> typeName baseType

let describe (assembly: Assembly) =
    [
        for t in assembly.GetExportedTypes() do
            yield
                $"type {t.FullName}{genericParameters (t.GetGenericArguments())} {typeKind t} : {baseTypeName t} [{attributeList (t.GetCustomAttributesData())}]"

            // Debug builds emit public compiler-generated methods such as <GetAwaiter>__debug@328-22.
            for m in
                t.GetMembers memberFlags
                |> Array.filter (fun m -> not (m.Name.StartsWith "<")) do
                match m with
                | :? MethodInfo as mi when not mi.IsSpecialName ->
                    yield
                        $"method {t.FullName}::{staticPrefix mi.IsStatic}{mi.Name}{genericParameters (mi.GetGenericArguments())}({parameterList (mi.GetParameters())}) : {typeName mi.ReturnType} [{attributeList (mi.GetCustomAttributesData())}]"
                | :? PropertyInfo as pi ->
                    let accessor =
                        match pi.GetMethod with
                        | null -> pi.SetMethod
                        | getter -> getter

                    let accessors =
                        [
                            if not (isNull pi.GetMethod) then
                                "get"
                            if not (isNull pi.SetMethod) then
                                "set"
                        ]
                        |> String.concat ","

                    yield
                        $"property {t.FullName}::{staticPrefix accessor.IsStatic}{pi.Name}({parameterList (pi.GetIndexParameters())}): {typeName pi.PropertyType} {{{accessors}}}"
                | :? FieldInfo as fi ->
                    yield
                        $"field {t.FullName}::{staticPrefix fi.IsStatic}{fi.Name}: {typeName fi.FieldType}"
                | :? ConstructorInfo as ci ->
                    yield $"ctor {t.FullName}({parameterList (ci.GetParameters())})"
                | _ -> ()
    ]
    |> List.distinct
    |> List.sort

// Runtime reflection throws TypeLoadException on some IcedTasks signatures.
let withMetadata (assemblyPath: string) (dependencyDirectories: string seq) (read: Assembly -> 'T) =
    let runtimeDirectory =
        Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()

    let paths =
        Seq.concat [
            Directory.GetFiles(runtimeDirectory, "*.dll")
            |> Seq.ofArray
            dependencyDirectories
            |> Seq.collect (fun directory -> Directory.GetFiles(directory, "*.dll"))
            Seq.singleton assemblyPath
        ]

    use context = new MetadataLoadContext(PathAssemblyResolver(paths))
    read (context.LoadFromAssemblyPath assemblyPath)
