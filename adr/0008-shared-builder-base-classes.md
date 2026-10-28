# 0008. Share one resumable implementation through builder base classes

- Status: accepted
- Date: 2023-11-06
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/34
- Reason source: PR description

Each builder file held its own copy of the resumable members, such as `Delay`, `Combine`, `While`, `TryWith`, `Using`, `For`, and `Bind`.
Two builders that differed only in the .NET method builder still needed two full copies.

We put the shared members in `TaskBuilderBase` and `CancellableTaskBuilderBase`.
Each concrete builder inherits a base class and adds its own `Run` and `RunDynamic`.
The state machine data `TaskBaseStateMachineData<'T, 'Builder>` is generic over the method builder.
The `MethodBuilder` SRTP shim in `src/IcedTasks/TaskLike.fs` calls the method builder.
The same PR removed the `backgroundValueTask` builders.

#34 says "I was getting annoyed having to create extremely large files for small variants in MethodBuilders" and "With this large refactor we've been able to only need to implement the `Run` methods and inherit from a base class."
For `backgroundValueTask`, #34 says "Removed `backgroundValueTask` variants as they made no sense to actually have."

## Consequences

A change to a base member changes all the builders that inherit it.
`ColdTaskBuilderBase` in `src/IcedTasks/ColdTask.fs` is a separate base with its own resumable members and state machine data. A fix to a shared member must also go there.
A reader must follow the generic `'Builder` and the SRTP shim to find the method builder call.
