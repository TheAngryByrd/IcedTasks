# 0013. Add runtime-async task builders in an opt-in net11.0 namespace

- Status: accepted
- Date: 2026-11-18
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/62
- Reason source: PR description, and measurements on the .NET 11 SDK 11.0.100

.NET 11 adds runtime async: the runtime, not a compiler-generated state machine, suspends and resumes a method that has `MethodImplOptions.Async`.
F# 11 (SDK 11.0.100, FSharp.Core 11.0.101) has preview support. The marker `__runtimeAsyncReturn` in `StateMachineHelpers` makes the enclosing method runtime async, and `AsyncHelpers.Await` suspends it.
A prototype of #62 used a private compiler build and an unsafe IL cast. The F# 11 SDK makes both unnecessary.

We add runtime-async versions of `task`, `backgroundTask`, `valueTask`, `coldTask`, `backgroundColdTask`, `cancellableTask`, `backgroundCancellableTask`, and `cancellableValueTask` in `IcedTasks.Polyfill.RuntimeTask` (`src/IcedTasks/RuntimeTask.fs`, `src/IcedTasks/RuntimeCancellableTask.fs`).

- The two files contain code only under `#if NET11_0_OR_GREATER`. The library adds a `net11.0` target with `LangVersion` `preview`, `Nullable`, and FSharp.Core 11.0.101. `Directory.Build.props` sets `LangVersion` `preview` for each `net11.0` inner build of the multi-target F# projects in this repository, because the tests also use the builders. A project that sets one `TargetFramework` in its own file gets the TFM after the props import, so the condition does not apply to it.
- The namespace is not auto-opened. It shadows the eight builder names, as the other polyfills do ([0009](0009-opt-in-polyfill-namespaces.md)).
- The builder shape follows the sample `RuntimeTaskBuilder` in the dotnet/fsharp component tests: the body returns `'T` directly, `Source` gives a `Started<'T>` delegate, and `Run` applies the return marker.
- The cancellable builders follow the sample `async2` builder: the body is `CancellationToken -> 'T`, and a `WithToken<'Awaiter>` delegate gives the ambient token to token-dependent and cold sources. A `WithToken` source starts the operation and returns its awaiter, so that `and!` can start all operands before it awaits them. The builders keep the token checks, the start order, and the `Task.FromCanceled` result of `CancellableTaskBuilderBase` and `CancellableTask.fs`.
- The result types are the existing aliases, so the companion modules work with the runtime builders.
- Each member has `NoDynamicInvocation`.

## Why NoDynamicInvocation

The compiler can lower the builder only when it inlines every member into the consumer method.
Without `NoDynamicInvocation`, a consumer in Debug with `LangVersion` 11.0 compiled with no error. It called the compiled library members, ran `AsyncHelpers.Await` outside a runtime-async method, and returned `1045044550` in place of `42`.
With `NoDynamicInvocation`, the same consumer fails to compile with FS3350 and FS3918. The compiler defect is [dotnet/fsharp#21001](https://github.com/dotnet/fsharp/issues/21001). In Release, the consumer fails with FS3350 in both cases.

## Considered options

- One `Source` that returns the awaiter, as in `TaskBuilderBase`. This awaits each `Task` through its `TaskAwaiter` with `UnsafeAwaitAwaiter`. The `Started` delegate lets `Task` and `ValueTask` sources use `AsyncHelpers.Await`, which is the call shape that the runtime specification optimizes. The cancellable builders use the awaiter form for token-dependent, cold, and `Async` sources only. `and!` must start all of these operands before it awaits the first one, and a delegate that starts and awaits in one call cannot do that.
- Auto-open the namespace. This would change `task` in each net11.0 consumer and require `LangVersion` `preview` from all of them.

## Consequences

- A consumer must target `net11.0` and set `LangVersion` `preview` to use the builders.
- Each `net11.0` consumer of IcedTasks depends on FSharp.Core 11.0.101 or later.
- In the builds for older TFMs, the namespace is empty, so `task` resolves to the FSharp.Core builder.
