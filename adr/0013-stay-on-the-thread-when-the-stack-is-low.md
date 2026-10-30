# 0013. Stay on the current thread when the stack is low

- Status: accepted
- Date: 2026-10-30
- Source: https://github.com/TheAngryByrd/IcedTasks/pull/54

A bound cold value can bounce to a trampoline, but a start from user code cannot.
The caller can block on the task, so the task must be complete or truly pending when the call returns.
Recursion through a start from user code, such as `return! f (n - 1) ct`, therefore uses the stack at each level.

An earlier version of the trampoline moved such a start to a new thread when the stack was low, and the caller waited with `Join`.
All recursion tests passed with this design.

We do not move a start to another thread.
A start from user code stays on the current thread and stack.
Recursion through it can overflow the stack, as it does without the trampoline.
`CancellableTask.toColdTask` lets `coldTask` bind a `CancellableTask`, so that this recursion can use a bind.

The purpose of a trampoline is to continue on the same thread after the stack unwinds.
The stack check also fails before the stack is full, so the move changed code that worked without it.
A `lock` that the caller held and the body took again became a permanent deadlock.
A hang gives less information than a stack overflow.

## Consequences

The rule is simple: a bind of a cold value is stack safe, and other calls behave as they do without the trampoline.
Code that blocks, uses `[<ThreadStatic>]` values, or holds a `lock` keeps its thread.
Recursion through a hot task overflows the stack, so users must know which call they write.
An analyzer could find these calls.
