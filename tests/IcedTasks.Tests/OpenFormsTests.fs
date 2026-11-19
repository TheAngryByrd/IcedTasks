module IcedTasksConsumer.OpenFormsTests

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open Expecto

module RootOpen =
    open IcedTasks

    let builderValues: obj list =
        [
            coldTask
            backgroundColdTask
            cancellableTask
            backgroundCancellableTask
            cancellableValueTask
            valueTask
            vTask
            valueTaskUnit
            vTaskUnit
            taskUnit
            backgroundTaskUnit
            asyncEx
            parallelAsync
            parallelAsyncUsingStartChild
            parallelAsyncUsingStartImmediateAsTask
#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
            poolingValueTask
            pvTask
            cancellablePoolingValueTask
            cancelablePVTask
#endif
        ]

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

    let valueTaskMap =
        <@ fun (source: ValueTask<int>) -> ValueTask.map ((+) 1) source @>

    let cancellableValueTaskMap =
        <@ fun (source: CancellableValueTask<int>) -> CancellableValueTask.map ((+) 1) source @>

#if !TEST_NETSTANDARD2_0 && !TEST_NETSTANDARD2_1
    let poolingValueTaskValue () = poolingValueTask { return 7 }
    let cancellablePoolingValue = cancellablePoolingValueTask { return 8 }
#endif

module PublicAbbreviations =
    open IcedTasks

    let exceptionValue: ExceptionNull = Exception "error"

    let disposableValue: IDisposableNull =
        { new IDisposable with
            member _.Dispose() = ()
        }

    let asyncDisposableValue: IAsyncDisposableNull =
        { new IAsyncDisposable with
            member _.DisposeAsync() = ValueTask()
        }

    let coldValue: ColdTask<int> = fun () -> Task.FromResult 1
    let coldUnit: ColdTask = fun () -> Task.CompletedTask
    let cancellableValue: CancellableTask<int> = fun _ -> Task.FromResult 2
    let cancellableUnit: CancellableTask = fun _ -> Task.CompletedTask
    let cancellableValueTaskValue: CancellableValueTask<int> = fun _ -> ValueTask<int> 3
    let cancellableValueTaskUnit: CancellableValueTask = fun _ -> ValueTask()

    let stateMachineTypes: Type list = [
        typeof<TaskBaseStateMachine<int, AsyncTaskMethodBuilder<int>>>
        typeof<TaskBaseResumptionFunc<int, AsyncTaskMethodBuilder<int>>>
        typeof<TaskBaseResumptionDynamicInfo<int, AsyncTaskMethodBuilder<int>>>
        typeof<TaskBaseCode<int, int, AsyncTaskMethodBuilder<int>>>
        typeof<ColdTaskStateMachine<int>>
        typeof<ColdTaskResumptionFunc<int>>
        typeof<ColdTaskResumptionDynamicInfo<int>>
        typeof<ColdTaskCode<int, int>>
        typeof<CancellableTaskBaseStateMachine<int, AsyncTaskMethodBuilder<int>>>
        typeof<CancellableTaskBaseResumptionFunc<int, AsyncTaskMethodBuilder<int>>>
        typeof<CancellableTaskBaseResumptionDynamicInfo<int, AsyncTaskMethodBuilder<int>>>
        typeof<CancellableTaskBaseCode<int, int, AsyncTaskMethodBuilder<int>>>
    ]

    let inline awaiterResult<'Awaiter, 'T when Awaiter<'Awaiter, 'T>> (awaiter: 'Awaiter) =
        Awaiter.GetResult awaiter

    let inline awaitableResult<'Awaitable, 'Awaiter, 'T when Awaitable<'Awaitable, 'Awaiter, 'T>>
        (awaitable: 'Awaitable)
        =
        awaiterResult (Awaitable.GetAwaiter awaitable)

    let awaitedValue () = awaitableResult (ValueTask<int> 4)

module ColdTasksOpen =
    open IcedTasks.ColdTasks

    let value =
        coldTask { return 1 }
        |> ColdTask.map ((+) 1)

    let bindsTask =
        coldTask {
            let! result = Task.FromResult 2
            return result
        }

    let taskBindsColdTask () =
        task {
            let! result = value
            return result
        }

module CancellableTasksOpen =
    open IcedTasks.CancellableTaskBase
    open IcedTasks.CancellableTasks

    let value =
        cancellableTask { return 2 }
        |> CancellableTask.map ((+) 1)

    let bindsTask =
        cancellableTask {
            let! result = Task.FromResult 3
            return result
        }

module CancellableValueTasksOpen =
    open IcedTasks.CancellableTaskBase
    open IcedTasks.CancellableValueTasks

    let value =
        cancellableValueTask { return 3 }
        |> CancellableValueTask.map ((+) 1)

    let bindsTask =
        cancellableValueTask {
            let! result = Task.FromResult 4
            return result
        }

module ValueTasksOpen =
    open IcedTasks.TaskBase
    open IcedTasks.ValueTasks

    let value () = valueTask { return 4 }

    let bindsTask () =
        valueTask {
            let! result = Task.FromResult 4
            return result
        }

module ValueTasksUnitOpen =
    open IcedTasks.ValueTasksUnit

    let value () = valueTaskUnit { return () }

module TasksUnitOpen =
    open IcedTasks.TasksUnit

    let value () = taskUnit { return () }

module AsyncExOpen =
    open IcedTasks.AsyncExExtensionsLowPriority
    open IcedTasks.AsyncExExtensionsHighPriority

    let value = asyncEx { return 5 }

    let bindsTask =
        asyncEx {
            let! result = Task.FromResult 5
            return result
        }

module AsyncExtensionsOpen =
    open IcedTasks.AsyncExtensions

    let value = Async.TryFinallyAsync(async { return 12 }, async { return () })

module ValueTaskExtensionsOpen =
    open IcedTasks.ValueTaskExtensions

    let value = Async.AwaitValueTask(ValueTask<int> 11)

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

    let taskBuilder: IcedTasks.Polyfill.Task.Tasks.TaskBuilder = task

    let backgroundTaskBuilder: IcedTasks.Polyfill.Task.Tasks.BackgroundTaskBuilder =
        backgroundTask

    let value () = task { return 9 }

module PolyfillAsyncOpen =
    open IcedTasks.Polyfill.Async

    let asyncBuilder: IcedTasks.AsyncExBuilder = async

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

let private calledFunction (quotation: Quotations.Expr) =
    match quotation with
    | Quotations.Patterns.Lambda(_, Quotations.Patterns.Call(_, methodInfo, _)) ->
        $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}"
    | other -> failwith $"The quotation is not a lambda with one call: {other}"

[<Tests>]
let tests =
    testList "0.11.9 open forms" [
        testCase "open IcedTasks brings each builder value"
        <| fun () ->
            let token = CancellationToken.None

            Expect.allEqual
                (RootOpen.builderValues
                 |> List.map isNull)
                false
                "Each builder value exists"

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

        testCase "the module functions that open IcedTasks selects are the 0.11.9 ones"
        <| fun () ->
            Expect.equal
                (calledFunction RootOpen.valueTaskMap)
                "IcedTasks.ValueTasks+ValueTask.map"
                "ValueTask.map is the non-pooling function, as in 0.11.9"

            Expect.equal
                (calledFunction RootOpen.cancellableValueTaskMap)
                "IcedTasks.CancellableValueTasks+CancellableValueTaskModule.map"
                "CancellableValueTask.map is the non-pooling function, as in 0.11.9"

        testCase "the public type abbreviations of 0.11.9 resolve after open IcedTasks"
        <| fun () ->
            Expect.equal PublicAbbreviations.exceptionValue.Message "error" "ExceptionNull"
            PublicAbbreviations.disposableValue.Dispose()
            PublicAbbreviations.asyncDisposableValue.DisposeAsync().AsTask().Wait()
            Expect.equal (PublicAbbreviations.coldValue().Result) 1 "ColdTask<'T>"
            PublicAbbreviations.coldUnit().Wait()

            Expect.equal
                (PublicAbbreviations.cancellableValue(CancellationToken.None).Result)
                2
                "CancellableTask<'T>"

            PublicAbbreviations.cancellableUnit(CancellationToken.None).Wait()

            Expect.equal
                (PublicAbbreviations
                    .cancellableValueTaskValue(CancellationToken.None)
                    .AsTask()
                    .Result)
                3
                "CancellableValueTask<'T>"

            PublicAbbreviations.cancellableValueTaskUnit(CancellationToken.None).AsTask().Wait()

            Expect.equal
                PublicAbbreviations.stateMachineTypes.Length
                12
                "Each state machine abbreviation resolves"

            Expect.equal (PublicAbbreviations.awaitedValue ()) 4 "Awaitable and Awaiter constraints"

        testCase "each 0.11.9 module open brings its builder or its extensions"
        <| fun () ->
            let token = CancellationToken.None
            Expect.equal (ColdTasksOpen.value().Result) 2 "open IcedTasks.ColdTasks"

            Expect.equal
                (ColdTasksOpen.bindsTask().Result)
                2
                "open IcedTasks.ColdTasks binds a Task"

            Expect.equal
                (ColdTasksOpen.taskBindsColdTask().Result)
                2
                "open IcedTasks.ColdTasks binds a ColdTask in task"

            Expect.equal
                (CancellableTasksOpen.value(token).Result)
                3
                "open IcedTasks.CancellableTasks"

            Expect.equal
                (CancellableTasksOpen.bindsTask(token).Result)
                3
                "open IcedTasks.CancellableTaskBase and IcedTasks.CancellableTasks bind a Task"

            Expect.equal
                (CancellableValueTasksOpen.value(token).AsTask().Result)
                4
                "open IcedTasks.CancellableValueTasks"

            Expect.equal
                (CancellableValueTasksOpen.bindsTask(token).AsTask().Result)
                4
                "open IcedTasks.CancellableTaskBase and IcedTasks.CancellableValueTasks bind a Task"

            Expect.equal (ValueTasksOpen.value().AsTask().Result) 4 "open IcedTasks.ValueTasks"

            Expect.equal
                (ValueTasksOpen.bindsTask().AsTask().Result)
                4
                "open IcedTasks.TaskBase and IcedTasks.ValueTasks bind a Task"

            ValueTasksUnitOpen.value().AsTask().Wait()
            TasksUnitOpen.value().Wait()

            Expect.equal
                (Async.RunSynchronously AsyncExOpen.value)
                5
                "open IcedTasks.AsyncExExtensionsLowPriority"

            Expect.equal
                (Async.RunSynchronously AsyncExOpen.bindsTask)
                5
                "open IcedTasks.AsyncExExtensionsHighPriority binds a Task"

            Expect.equal
                (Async.RunSynchronously AsyncExtensionsOpen.value)
                12
                "open IcedTasks.AsyncExtensions"

            Expect.equal
                (Async.RunSynchronously ValueTaskExtensionsOpen.value)
                11
                "open IcedTasks.ValueTaskExtensions"

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

        testCase "the polyfill opens shadow task, backgroundTask, and async"
        <| fun () ->
            Expect.isNotNull
                (box PolyfillTaskOpen.taskBuilder)
                "open IcedTasks.Polyfill.Task shadows task"

            Expect.isNotNull
                (box PolyfillTaskOpen.backgroundTaskBuilder)
                "open IcedTasks.Polyfill.Task shadows backgroundTask"

            Expect.isNotNull
                (box PolyfillAsyncOpen.asyncBuilder)
                "open IcedTasks.Polyfill.Async shadows async"

            Expect.equal (PolyfillTaskOpen.value().Result) 9 "open IcedTasks.Polyfill.Task"

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
                "IcedTasks.Polyfill.Task.Tasks.TaskBuilder.task"
    ]
