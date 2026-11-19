# Test projects and TFMs

Four test projects test the library builds. Each project targets `net10.0;net11.0`, so `dotnet test` runs each project two times.

| Project | Library TFM | Defines | Source files |
|---|---|---|---|
| `tests/IcedTasks.Tests` | `net6.0` on the `net10.0` run, `net9.0` on the `net11.0` run | `TEST_NET6_0_OR_GREATER` | its own 18 files, and `PublicApi/*.txt` |
| `tests/IcedTasks.Tests.NS20` | `netstandard2.0` | `NETSTANDARD2_0;TEST_NETSTANDARD2_0` | links `../IcedTasks.Tests/*.fs`, no Pooling test files |
| `tests/IcedTasks.Tests.NS21` | `netstandard2.1` | `NETSTANDARD2_1;TEST_NETSTANDARD2_1` | links `../IcedTasks.Tests/*.fs`, no Pooling test files |
| `tests/IcedTasks.Tests.NoOpen` | same as `IcedTasks.Tests` | none | its own 2 files, with no `open` of an IcedTasks namespace |

The NS projects select the library TFM with `AdditionalProperties`:

```xml
<!-- tests/IcedTasks.Tests.NS20/IcedTasks.Tests.NS20.fsproj -->
    <TargetFrameworks>net10.0;net11.0</TargetFrameworks>
    <GenerateProgramFile>false</GenerateProgramFile>
    <DefineConstants>$(DefineConstants);NETSTANDARD2_0;TEST_NETSTANDARD2_0</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/IcedTasks/IcedTasks.fsproj" AdditionalProperties="TargetFramework=netstandard2.0" />
```

Each shipped library build needs one test run. A test TFM selects the nearest library TFM, so `IcedTasks.Tests` sets `AdditionalProperties="TargetFramework=net6.0"` for its `net10.0` run. Without it, both runs load the `net9.0` build. The restore still uses the `net9.0` dependency graph, so the `net10.0` run loads FSharp.Core 9.0.300, not the 6.0.4 floor of the `net6.0` build. Expecto needs FSharp.Core 7.0.200 or later, so a test run cannot use 6.0.4.

## Public surface tests

- `tests/IcedTasks.Tests.NoOpen` uses the FSharp.Core `task` builder with no IcedTasks `open`. It compiles only if a project reference alone brings no IcedTasks extension members. Its files must not declare a namespace under `IcedTasks`, because that declaration opens `IcedTasks`.
- `OpenFormsTests.fs` uses each 0.11.9 `open` form and fully qualified 0.11.9 names. It is a top-level module outside the `IcedTasks` namespace for the same reason.
- `PublicApiTests.fs` checks that the assembly has no assembly-level `AutoOpenAttribute`. It also lists the public metadata with `System.Reflection.MetadataLoadContext` and compares it with `PublicApi/IcedTasks-0.11.9-<tfm>.txt`. The NS projects link the baseline files. Runtime reflection throws `TypeLoadException` on some IcedTasks signatures, so the listing reads metadata only.

## Rules for new tests

- Put a new test file in `tests/IcedTasks.Tests` and add it to the compile list of that project.
- Link the file in the NS20 and NS21 projects too, unless it tests a pooling builder.
- Guard TaskSeq tests with `#if TEST_NETSTANDARD2_1 || TEST_NET6_0_OR_GREATER`. The NS20 build runs the `IAsyncEnumerable` tests through `Microsoft.Bcl.AsyncInterfaces`, and it skips the TaskSeq tests.

```mermaid
flowchart TD
    file["tests/IcedTasks.Tests/XTests.fs"] --> main["IcedTasks.Tests"]
    file -->|"Compile Include link"| ns20["NS20"]
    file -->|"Compile Include link"| ns21["NS21"]
    pool["Pooling*Tests.fs"] --> main
    main -->|"net10.0 run"| lib6["lib net6.0 build, AdditionalProperties"]
    main -->|"net11.0 run"| lib9["lib net9.0 build, nearest TFM"]
    ns20 --> lib20["lib netstandard2.0 build"]
    ns21 --> lib21["lib netstandard2.1 build"]
```

## Configuration

CI runs the tests in Debug and in Release. Debug takes the dynamic path of each builder. See [run and start paths](../builders/run-and-start-paths.md).

Decision: [ADR 0007, separate netstandard test projects](../../adr/0007-separate-netstandard-test-projects.md).

Related: [tests summary](summary.md), [namespaces and TFMs](../builders/namespaces-and-tfms.md).
