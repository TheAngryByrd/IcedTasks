#!/usr/bin/env dotnet fsi

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes

/// Combines multiple BenchmarkDotNet JSON result files into a single file
/// This is useful when you have multiple benchmark projects/classes and want
/// to create a single file for the github-action-benchmark to process
let combineBenchmarkResults (resultsDir: string) (outputFileName: string) (searchPattern: string) =

    let resultsPath =
        Path.Combine(
            resultsDir,
            outputFileName
            + ".json"
        )

    printfn "Looking for benchmark files in: %s" resultsDir
    printfn "Search pattern: %s" searchPattern
    printfn "Output file: %s" resultsPath

    // Ensure results directory exists
    if not (Directory.Exists(resultsDir)) then
        failwithf "Directory not found: '%s'" resultsDir

    // Delete existing output file if it exists
    if File.Exists(resultsPath) then
        File.Delete(resultsPath)
        printfn "Deleted existing output file"

    // Find all matching benchmark result files
    let reportFiles =
        Directory.GetFiles(resultsDir, searchPattern, SearchOption.TopDirectoryOnly)
        |> Array.filter (fun f ->
            not (
                f.EndsWith(
                    outputFileName
                    + ".json"
                )
            )
        ) // Don't include our output file

    printfn "Found %d benchmark files:" reportFiles.Length

    reportFiles
    |> Array.iter (printfn "  - %s")

    if reportFiles.Length = 0 then
        failwithf "No benchmark files found matching pattern '%s' in '%s'" searchPattern resultsDir

    // Read the first file as the base
    let firstFile = reportFiles.[0]
    let firstContent = File.ReadAllText(firstFile)
    let combinedReport = JsonNode.Parse(firstContent)

    if combinedReport = null then
        failwithf "Failed to parse JSON from first file: %s" firstFile

    printfn "Using '%s' as base file" (Path.GetFileName(firstFile))

    // Get the benchmarks array from the first file
    let benchmarksArray = combinedReport.["Benchmarks"].AsArray()

    // Update the title to indicate this is a combined report
    let originalTitle = combinedReport.["Title"].GetValue<string>()

    let timestamp =
        if
            originalTitle.Length
            >= 16
        then
            originalTitle.Substring(
                originalTitle.Length
                - 16
            )
        else
            DateTime.Now.ToString("yyyyMMdd-HHmmss")

    combinedReport.["Title"] <- JsonValue.Create($"Combined Benchmarks {timestamp}")

    // Process remaining files
    for reportFile in
        reportFiles
        |> Array.skip 1 do
        try
            printfn "Processing: %s" (Path.GetFileName(reportFile))
            let content = File.ReadAllText(reportFile)
            let report = JsonNode.Parse(content)

            if
                report
                <> null
            then
                let benchmarks = report.["Benchmarks"].AsArray()

                // Add each benchmark from this file to the combined array
                for benchmark in benchmarks do
                    // Parse and re-add to avoid "node already has parent" issues
                    let benchmarkCopy = JsonNode.Parse(benchmark.ToJsonString())
                    benchmarksArray.Add(benchmarkCopy)

                printfn "  Added %d benchmarks" benchmarks.Count
            else
                printfn "  Warning: Failed to parse JSON from %s" reportFile
        with ex ->
            printfn "  Error processing %s: %s" (Path.GetFileName(reportFile)) ex.Message

    // Write the combined results
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.WriteIndented <- true

    let combinedJson = combinedReport.ToJsonString(jsonOptions)
    File.WriteAllText(resultsPath, combinedJson)

    let totalBenchmarks = combinedReport.["Benchmarks"].AsArray().Count
    printfn ""
    printfn "✅ Successfully combined %d files into %s" reportFiles.Length resultsPath
    printfn "   Total benchmarks: %d" totalBenchmarks
    printfn ""

// Default configuration - can be overridden by command line arguments
let mutable resultsDir = "./BenchmarkDotNet.Artifacts/results"
let mutable outputFileName = "Combined.Benchmarks"
let mutable searchPattern = "*.json"

// Parse command line arguments
let args =
    System.Environment.GetCommandLineArgs()
    |> Array.skip 2 // Skip the first two args (fsi and script name)
    |> Array.toList

printfn "Arguments: %A" args

let rec parseArgs argList =
    match argList with
    | [] -> ()
    | [ "-d"; dir ] -> resultsDir <- dir
    | [ "--dir"; dir ] -> resultsDir <- dir
    | [ "-o"; output ] -> outputFileName <- output
    | [ "--output"; output ] -> outputFileName <- output
    | [ "-p"; pattern ] -> searchPattern <- pattern
    | [ "--pattern"; pattern ] -> searchPattern <- pattern
    | [ "-h" ]
    | [ "--help" ] ->
        printfn
            """
F# Benchmark Results Combiner

Usage: dotnet fsi combine-benchmarks.fsx [options]

Options:
  -d, --dir <directory>     Results directory (default: ./BenchmarkDotNet.Artifacts/results)
  -o, --output <filename>   Output filename without extension (default: Combined.Benchmarks)  
  -p, --pattern <pattern>   Search pattern for JSON files (default: *.json)
  -h, --help               Show this help message

Examples:
  dotnet fsi combine-benchmarks.fsx
  dotnet fsi combine-benchmarks.fsx -d "./results" -o "AllBenchmarks" -p "*-report-full-compressed.json"
  
This script combines multiple BenchmarkDotNet JSON files into a single file
that can be processed by github-action-benchmark.
"""

        Environment.Exit(0)
    | "-d" :: dir :: rest ->
        resultsDir <- dir
        parseArgs rest
    | "--dir" :: dir :: rest ->
        resultsDir <- dir
        parseArgs rest
    | "-o" :: output :: rest ->
        outputFileName <- output
        parseArgs rest
    | "--output" :: output :: rest ->
        outputFileName <- output
        parseArgs rest
    | "-p" :: pattern :: rest ->
        searchPattern <- pattern
        parseArgs rest
    | "--pattern" :: pattern :: rest ->
        searchPattern <- pattern
        parseArgs rest
    | [ dir; output; pattern ] ->
        resultsDir <- dir
        outputFileName <- output
        searchPattern <- pattern
    | [ dir; output ] ->
        resultsDir <- dir
        outputFileName <- output
    | [ dir ] -> resultsDir <- dir
    | unknown ->
        printfn "Unknown arguments: %A" unknown
        printfn "Use --help for usage information"
        Environment.Exit(1)

parseArgs (args)

// Run the combination
try
    combineBenchmarkResults resultsDir outputFileName searchPattern
with ex ->
    printfn "❌ Error: %A" ex
    Environment.Exit(1)
