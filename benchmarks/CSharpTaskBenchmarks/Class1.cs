using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

#pragma warning disable 1998

public static class TaskPerfCSharp
{
    // public const int BufferSize = 128;
    //public const int ManyIterations = 10000;

    public static async Task ManyWriteFileAsync(int ManyIterations, int BufferSize)
    {
        const string path = "tmp";
        var junk = new byte[BufferSize];
        using (var file = File.Create(path))
        {
            for (var i = 1; i <= ManyIterations; i++)
            {
                await file.WriteAsync(junk, 0, junk.Length);
            }
        }
        File.Delete(path);
    }


    public static async ValueTask ManyWriteFileAsync_ValueTask(int ManyIterations, int BufferSize)
    {
        const string path = "tmp";
        var junk = new byte[BufferSize];
        using (var file = File.Create(path))
        {
            for (var i = 1; i <= ManyIterations; i++)
            {
                await file.WriteAsync(junk, 0, junk.Length);
            }
        }
        File.Delete(path);
    }


    public static System.Runtime.CompilerServices.YieldAwaitable AsyncTask()
    {
        return Task.Yield();
    }

    public static Task<int> SyncTask()
    {
        return Task.FromResult(100);
    }

    public static ValueTask<int> SyncValueTask()
    {
        return new ValueTask<int>(100);
    }

    public static async Task<int> CSharp_TenBindsSync_TaskBuilder_BindTask()
    {
        var x1 = await SyncTask();
        var x2 = await SyncTask();
        var x3 = await SyncTask();
        var x4 = await SyncTask();
        var x5 = await SyncTask();
        var x6 = await SyncTask();
        var x7 = await SyncTask();
        var x8 = await SyncTask();
        var x9 = await SyncTask();
        var x10 = await SyncTask();
        return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10;
    }

    public static async Task<int> CSharp_TenBindsSync_TaskBuilder_BindValueTask()
    {
        var x1 = await SyncValueTask();
        var x2 = await SyncValueTask();
        var x3 = await SyncValueTask();
        var x4 = await SyncValueTask();
        var x5 = await SyncValueTask();
        var x6 = await SyncValueTask();
        var x7 = await SyncValueTask();
        var x8 = await SyncValueTask();
        var x9 = await SyncValueTask();
        var x10 = await SyncValueTask();
        return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10;
    }

    public static async ValueTask<int> CSharp_TenBindsSync_ValueTaskBuilder_BindTask()
    {
        var x1 = await SyncTask();
        var x2 = await SyncTask();
        var x3 = await SyncTask();
        var x4 = await SyncTask();
        var x5 = await SyncTask();
        var x6 = await SyncTask();
        var x7 = await SyncTask();
        var x8 = await SyncTask();
        var x9 = await SyncTask();
        var x10 = await SyncTask();
        return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10;
    }


    public static async ValueTask<int> CSharp_TenBindsSync_ValueTaskBuilder_BindValueTask()
    {
        var x1 = await SyncValueTask();
        var x2 = await SyncValueTask();
        var x3 = await SyncValueTask();
        var x4 = await SyncValueTask();
        var x5 = await SyncValueTask();
        var x6 = await SyncValueTask();
        var x7 = await SyncValueTask();
        var x8 = await SyncValueTask();
        var x9 = await SyncValueTask();
        var x10 = await SyncValueTask();
        return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + x10;
    }

    public static async Task<int> CSharp_TenBindsAsync_TaskBuilder()
    {
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        return 100;
    }


    public static async ValueTask<int> CSharp_TenBindsAsync_ValueTaskBuilder()
    {
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        await AsyncTask();
        return 100;
    }

    public static async Task<int> SingleSyncTask_CSharp()
    {
        return 1;
    }

    public static async Task<int> SingleSyncExceptionTask_CSharp()
    {
        throw (new System.Exception("fail"));
    }


    public static async IAsyncEnumerable<int> perf1_AsyncEnumerable(int x)
    {
        yield return 1;
        yield return 2;
        if (x >= 2)
        {
            yield return 3;
            yield return 4;
        }
    }

    public static async IAsyncEnumerable<int> perf2_AsyncEnumerable()
    {
        await foreach (var i1 in perf1_AsyncEnumerable(3))
        {
            await foreach (var i2 in perf1_AsyncEnumerable(3))
            {
                await foreach (var i3 in perf1_AsyncEnumerable(3))
                {
                    await foreach (var i4 in perf1_AsyncEnumerable(3))
                    {
                        await foreach (var i5 in perf1_AsyncEnumerable(3))
                        {
                            await foreach (var i6 in perf1_AsyncEnumerable(i5))
                            {
                                yield return i6;

                            }
                        }

                    }

                }

            }

        }
    }

    public static async Task<int> Csharp_WhileLoop(int length)
    {
        var i = 0;
        while (i < length)
        {
            i++;
        }
        return i;
    }
    public static async Task<int> Csharp_WhileLoop_Async(int length)
    {
        var i = 0;
        while (i < length)
        {
            await Task.Yield();
            i++;
        }
        return i;
    }
}
