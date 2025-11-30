namespace IcedTasks.Nullness

// This file exists to define nullable and non-nullable types for compatibility with older FSharp.Core libraries.
//  Nullable/Nullness only works for FSharp.Core 9+. For older versions, we define types without nullability annotations and assume reference types can be null.

open System

type ExceptionNull =
#if NULLABLE
    Exception | null
#else
    Exception
#endif

type IDisposableNull =
#if NULLABLE
    IDisposable | null
#else
    IDisposable
#endif

type IAsyncDisposableNull =
#if NULLABLE
    IAsyncDisposable | null
#else
    IAsyncDisposable
#endif
