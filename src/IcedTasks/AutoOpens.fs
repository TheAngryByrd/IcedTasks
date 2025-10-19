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
    [<assembly: AutoOpen("IcedTasks.PoolingValueTasks")>]
    [<assembly: AutoOpen("IcedTasks.ValueTasksUnit")>]
    [<assembly: AutoOpen("IcedTasks.ColdTasks")>]
    [<assembly: AutoOpen("IcedTasks.CancellableTaskBase")>]
    [<assembly: AutoOpen("IcedTasks.CancellableValueTasks")>]
    [<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks")>]
    [<assembly: AutoOpen("IcedTasks.CancellableTasks")>]
    do ()