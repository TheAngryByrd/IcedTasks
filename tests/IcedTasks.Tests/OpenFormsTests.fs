module IcedTasksConsumer.OpenFormsTests

open System.Threading
open System.Threading.Tasks
open Expecto

module RootOpen =
    open IcedTasks

    let coldValue = coldTask { return 1 }
    let cancellableValue = cancellableTask { return 2 }
    let cancellableValueTaskValue = cancellableValueTask { return 3 }
    let valueTaskValue () = valueTask { return 4 }
    let valueTaskUnitValue () = valueTaskUnit { return () }
    let taskUnitValue () = taskUnit { return () }
    let asyncExValue = asyncEx { return 5 }
    let parallelAsyncValue = parallelAsync { return 6 }

    let taskBindsColdTask () =
        task {
            let! value = coldValue
            return value
        }

#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
    let poolingValueTaskValue () = poolingValueTask { return 7 }
    let cancellablePoolingValue = cancellablePoolingValueTask { return 8 }
#endif

module ColdTasksOpen =
    open IcedTasks.ColdTasks

    let value =
        coldTask { return 1 }
        |> ColdTask.map ((+) 1)

    let taskBindsColdTask () =
        task {
            let! result = value
            return result
        }

module CancellableTasksOpen =
    open IcedTasks.CancellableTasks

    let value =
        cancellableTask { return 2 }
        |> CancellableTask.map ((+) 1)

module CancellableValueTasksOpen =
    open IcedTasks.CancellableValueTasks

    let value =
        cancellableValueTask { return 3 }
        |> CancellableValueTask.map ((+) 1)

module ValueTasksOpen =
    open IcedTasks.ValueTasks

    let value () = valueTask { return 4 }

module ValueTasksUnitOpen =
    open IcedTasks.ValueTasksUnit

    let value () = valueTaskUnit { return () }

module TasksUnitOpen =
    open IcedTasks.TasksUnit

    let value () = taskUnit { return () }

module AsyncExOpen =
    open IcedTasks.AsyncExExtensionsLowPriority

    let value = asyncEx { return 5 }

module ParallelAsyncsOpen =
    open IcedTasks.ParallelAsyncs

    let value = parallelAsync { return 6 }

#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
module PoolingValueTasksOpen =
    open IcedTasks.PoolingValueTasks

    let value () = poolingValueTask { return 7 }

module CancellablePoolingValueTasksOpen =
    open IcedTasks.CancellablePoolingValueTasks

    let value = cancellablePoolingValueTask { return 8 }
#endif

module PolyfillTaskOpen =
    open IcedTasks.Polyfill.Task

    let value () = task { return 9 }

module PolyfillAsyncOpen =
    open IcedTasks.Polyfill.Async

    let value = async { return 10 }

module QualifiedNames =
    let coldValue =
        IcedTasks.ColdTasks.ColdTaskBuilder.coldTask { return 1 }
        |> IcedTasks.ColdTasks.ColdTask.map ((+) 1)

    let cancellableValue =
        IcedTasks.CancellableTasks.CancellableTaskBuilder.cancellableTask { return 2 }
        |> IcedTasks.CancellableTasks.CancellableTask.map ((+) 1)

    let valueTaskValue () =
        IcedTasks.ValueTasks.ValueTask.ofUnit (
            IcedTasks.ValueTasksUnit.ValueTaskBuilder.valueTaskUnit { return () }
        )

    let taskUnitValue () =
        IcedTasks.TasksUnit.TaskUnitBuilder.taskUnit { return () }

    let asyncExValue = IcedTasks.AsyncExExtensionsLowPriority.asyncEx { return 5 }

    let parallelAsyncValue = IcedTasks.ParallelAsyncs.parallelAsync { return 6 }

    let polyfillTaskValue () =
        IcedTasks.Polyfill.Task.Tasks.TaskBuilder.task { return 9 }

[<Tests>]
let tests =
    testList "0.11.9 open forms" [
        testCase "open IcedTasks brings each builder"
        <| fun () ->
            let token = CancellationToken.None
            Expect.equal (RootOpen.coldValue().Result) 1 "coldTask"
            Expect.equal (RootOpen.cancellableValue(token).Result) 2 "cancellableTask"

            Expect.equal
                (RootOpen.cancellableValueTaskValue(token).AsTask().Result)
                3
                "cancellableValueTask"

            Expect.equal (RootOpen.valueTaskValue().AsTask().Result) 4 "valueTask"
            RootOpen.valueTaskUnitValue().AsTask().Wait()
            RootOpen.taskUnitValue().Wait()
            Expect.equal (Async.RunSynchronously RootOpen.asyncExValue) 5 "asyncEx"
            Expect.equal (Async.RunSynchronously RootOpen.parallelAsyncValue) 6 "parallelAsync"
            Expect.equal (RootOpen.taskBindsColdTask().Result) 1 "task binds a ColdTask"
#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
            Expect.equal (RootOpen.poolingValueTaskValue().AsTask().Result) 7 "poolingValueTask"

            Expect.equal
                (RootOpen.cancellablePoolingValue(token).AsTask().Result)
                8
                "cancellablePoolingValueTask"
#endif

        testCase "each 0.11.9 module open brings its builder"
        <| fun () ->
            let token = CancellationToken.None
            Expect.equal (ColdTasksOpen.value().Result) 2 "open IcedTasks.ColdTasks"

            Expect.equal
                (ColdTasksOpen.taskBindsColdTask().Result)
                2
                "open IcedTasks.ColdTasks binds a ColdTask in task"

            Expect.equal
                (CancellableTasksOpen.value(token).Result)
                3
                "open IcedTasks.CancellableTasks"

            Expect.equal
                (CancellableValueTasksOpen.value(token).AsTask().Result)
                4
                "open IcedTasks.CancellableValueTasks"

            Expect.equal (ValueTasksOpen.value().AsTask().Result) 4 "open IcedTasks.ValueTasks"
            ValueTasksUnitOpen.value().AsTask().Wait()
            TasksUnitOpen.value().Wait()

            Expect.equal
                (Async.RunSynchronously AsyncExOpen.value)
                5
                "open IcedTasks.AsyncExExtensionsLowPriority"

            Expect.equal
                (Async.RunSynchronously ParallelAsyncsOpen.value)
                6
                "open IcedTasks.ParallelAsyncs"
#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
            Expect.equal
                (PoolingValueTasksOpen.value().AsTask().Result)
                7
                "open IcedTasks.PoolingValueTasks"

            Expect.equal
                (CancellablePoolingValueTasksOpen.value(token).AsTask().Result)
                8
                "open IcedTasks.CancellablePoolingValueTasks"
#endif

        testCase "the polyfill opens shadow task and async"
        <| fun () ->
            let polyfillTask: Task<int> = PolyfillTaskOpen.value ()
            Expect.equal polyfillTask.Result 9 "open IcedTasks.Polyfill.Task"

            Expect.equal
                (Async.RunSynchronously PolyfillAsyncOpen.value)
                10
                "open IcedTasks.Polyfill.Async"

        testCase "fully qualified 0.11.9 names resolve"
        <| fun () ->
            Expect.equal (QualifiedNames.coldValue().Result) 2 "IcedTasks.ColdTasks.ColdTask.map"

            Expect.equal
                (QualifiedNames.cancellableValue(CancellationToken.None).Result)
                3
                "IcedTasks.CancellableTasks.CancellableTask.map"

            QualifiedNames.valueTaskValue().AsTask().Wait()
            QualifiedNames.taskUnitValue().Wait()

            Expect.equal
                (Async.RunSynchronously QualifiedNames.asyncExValue)
                5
                "IcedTasks.AsyncExExtensionsLowPriority.asyncEx"

            Expect.equal
                (Async.RunSynchronously QualifiedNames.parallelAsyncValue)
                6
                "IcedTasks.ParallelAsyncs.parallelAsync"

            Expect.equal
                (QualifiedNames.polyfillTaskValue().Result)
                9
                "IcedTasks.Polyfill.Task.Tasks.task"
    ]
