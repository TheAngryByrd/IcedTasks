# 0007. Test the netstandard builds in their own test projects

- Status: accepted
- Date: 2023-07-18
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/26, https://github.com/TheAngryByrd/IcedTasks/pull/33
- Reason source: PR description

A test project that targets .NET references the nearest .NET build of the library, never a netstandard build.

We test the `netstandard2.0` build in `tests/IcedTasks.Tests.NS20` (#26) and the `netstandard2.1` build in `tests/IcedTasks.Tests.NS21` (#33).
Each project links the test files of `tests/IcedTasks.Tests`.
Each project references the library with `AdditionalProperties="TargetFramework=netstandard2.0"` or `netstandard2.1`.

#26 says "This will make it easier to deal with ifdefs on net6+ features later."
