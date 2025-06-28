namespace IcedTasks

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
