namespace IcedTasks

[<assembly: AutoOpen("IcedTasks.TaskBase")>]
[<assembly: AutoOpen("IcedTasks.TaskBase.LowPriority")>]
[<assembly: AutoOpen("IcedTasks.TaskBase.HighPriority")>]

[<assembly: AutoOpen("IcedTasks.ValueTaskExtensions")>]
[<assembly: AutoOpen("IcedTasks.ValueTasks")>]
[<assembly: AutoOpen("IcedTasks.ValueTasks.ValueTaskBuilderModule")>]

#if NET6_0_OR_GREATER
[<assembly: AutoOpen("IcedTasks.PoolingValueTasks")>]
[<assembly: AutoOpen("IcedTasks.PoolingValueTasks.ValueTaskBuilder")>]
#endif

[<assembly: AutoOpen("IcedTasks.ValueTasksUnit")>]
[<assembly: AutoOpen("IcedTasks.ValueTasksUnit.ValueTaskBuilder")>]

[<assembly: AutoOpen("IcedTasks.TasksUnit")>]
[<assembly: AutoOpen("IcedTasks.TasksUnit.TaskUnitBuilder")>]

[<assembly: AutoOpen("IcedTasks.AsyncExtensions")>]
[<assembly: AutoOpen("IcedTasks.AsyncExExtensionsLowPriority")>]
[<assembly: AutoOpen("IcedTasks.AsyncExExtensionsHighPriority")>]

[<assembly: AutoOpen("IcedTasks.ParallelAsyncs")>]

[<assembly: AutoOpen("IcedTasks.ColdTasks")>]
[<assembly: AutoOpen("IcedTasks.ColdTasks.ColdTaskBuilderModule")>]
[<assembly: AutoOpen("IcedTasks.ColdTasks.LowPriority")>]
[<assembly: AutoOpen("IcedTasks.ColdTasks.HighPriority")>]
[<assembly: AutoOpen("IcedTasks.ColdTasks.AsyncExtensions")>]
[<assembly: AutoOpen("IcedTasks.ColdTasks.MergeSourcesExtensions")>]

[<assembly: AutoOpen("IcedTasks.CancellableTaskBase")>]
[<assembly: AutoOpen("IcedTasks.CancellableTaskBase.LowPriority")>]
[<assembly: AutoOpen("IcedTasks.CancellableTaskBase.HighPriority")>]

[<assembly: AutoOpen("IcedTasks.CancellableValueTasks")>]
[<assembly: AutoOpen("IcedTasks.CancellableValueTasks.CancellableValueTaskBuilderModule")>]
[<assembly: AutoOpen("IcedTasks.CancellableValueTasks.HighPriority")>]
[<assembly: AutoOpen("IcedTasks.CancellableValueTasks.AsyncExtensions")>]

#if NET6_0_OR_GREATER
[<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks")>]
[<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks.CancellableValueTaskBuilder")>]
[<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks.HighPriority")>]
[<assembly: AutoOpen("IcedTasks.CancellablePoolingValueTasks.AsyncExtensions")>]
#endif

[<assembly: AutoOpen("IcedTasks.CancellableTasks")>]
[<assembly: AutoOpen("IcedTasks.CancellableTasks.CancellableTaskBuilderModule")>]
[<assembly: AutoOpen("IcedTasks.CancellableTasks.AsyncExtensions")>]

do ()


// namespace IcedTasks.Polyfill.Task

// [<assembly: AutoOpen("IcedTasks.Polyfill.Task.Tasks")>]
// [<assembly: AutoOpen("IcedTasks.Polyfill.Task.Tasks.TaskBuilderModule")>]
// do ()

// namespace IcedTasks.Polyfill.Async

// [<assembly: AutoOpen("IcedTasks.Polyfill.Async.PolyfillBuilders")>]
// do ()
