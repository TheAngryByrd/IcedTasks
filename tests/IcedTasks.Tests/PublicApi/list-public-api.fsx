// Usage: dotnet fsi list-public-api.fsx <IcedTasks.dll> <output file> [dependency directory]...
// The 0.11.9 baselines come from lib/<tfm>/IcedTasks.dll of the IcedTasks 0.11.9 package, with the
// directory of a matching FSharp.Core.dll (and Microsoft.Bcl.AsyncInterfaces.dll for netstandard2.0).
#r "nuget: System.Reflection.MetadataLoadContext, 10.0.0"
#load "../PublicApiListing.fs"

open System.IO
open IcedTasksConsumer

match List.ofArray fsi.CommandLineArgs with
| _ :: assemblyPath :: outputPath :: dependencyDirectories ->
    let entries =
        PublicApiListing.withMetadata
            (Path.GetFullPath assemblyPath)
            (dependencyDirectories
             |> List.map Path.GetFullPath)
            PublicApiListing.describe

    File.WriteAllText(
        outputPath,
        String.concat "\n" entries
        + "\n"
    )

    printfn "%d entries written to %s" entries.Length outputPath
| _ ->
    eprintfn
        "Usage: dotnet fsi list-public-api.fsx <IcedTasks.dll> <output file> [dependency directory]..."

    exit 2
