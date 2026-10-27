# 0005. Start both `and!` operands before the builder awaits either

- Status: accepted
- Date: 2022-12-09
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/16, https://github.com/TheAngryByrd/IcedTasks/pull/41
- Reason source: PR description

F# lets a builder's `MergeSources` run the `and!` operands one after the other or at the same time.

We start each cold or token-dependent operand, and then await the left operand and the right operand.
In the cancellable builders, each operand that is a `CancellationToken -> 'Awaiter` gets the same token. An operand that is already a task or an awaiter is not changed.
Most task builders define `MergeSources` on the builder type, for example in `src/IcedTasks/CancellableTask.fs`.

#16 says "Adds BindReturn/MergeSources to allow for parallel computations using `and!`."
#41 fixed a defect in that behavior: "Merge Sources wasn't running things in parallel when it should have been. So some `and!` combinations may not have been scheduled in parallel."

## Consequences

`and!` does not work like `let!` followed by `let!`. Side effects in the two operands can interleave.
When one operand faults, the other operand keeps running, and the builder does not cancel it.
