namespace IcedTasks

/// Module exists to AutoOpen all relevant IcedTasks modules for ease of use when opening the main IcedTasks namespace.
[<AutoOpen>]
module AutoOpens =

    [<assembly: AutoOpen("IcedTasks")>]
    [<assembly: AutoOpen("IcedTasks.TaskLike")>]
    [<assembly: AutoOpen("IcedTasks.TasksUnit")>]
    [<assembly: AutoOpen("IcedTasks.AsyncEx")>]
    [<assembly: AutoOpen("IcedTasks.ParallelAsync")>]
    [<assembly: AutoOpen("IcedTasks.TaskBase")>]
    [<assembly: AutoOpen("IcedTasks.ValueTasks")>]
#if NET6_0_OR_GREATER
    [<assembly: AutoOpen("IcedTasks.PoolingValueTasks")>]
#endif
    [<assembly: AutoOpen("IcedTasks.ValueTasksUnit")>]
    [<assembly: AutoOpen("IcedTasks.ColdTasks")>]
    [<assembly: AutoOpen("IcedTasks.CancellableTaskBase")>]
    [<assembly: AutoOpen("IcedTasks.CancellableValueTasks")>]
#if NET6_0_OR_GREATER
    [<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks")>]
#endif
    [<assembly: AutoOpen("IcedTasks.CancellableTasks")>]
#if NET10_0_OR_GREATER
    [<assembly: AutoOpen("IcedTasks.TaskBase_Net10")>]
#endif
    do ()
