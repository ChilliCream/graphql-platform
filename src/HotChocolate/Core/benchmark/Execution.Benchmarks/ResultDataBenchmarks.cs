using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Pooling;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ResultDataBenchmarks
{
    private readonly string[] _keys = new string[256];
    private readonly object _value = new();
    private readonly ObjectResult _objectResult = new();
    private readonly ObjectResult _objectResultExpanded = new();

    public ResultDataBenchmarks ()
    {
        for (var i = 0; i < _keys.Length; i++)
        {
            _keys[i] = i.ToString();
        }

        _objectResultExpanded.EnsureCapacity(256);
    }

    [Params(1,2,3,4, 8, 256)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public void ObjectResult_Init_Set_Clear()
    {
        _objectResult.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _objectResult.SetValueUnsafe(i, _keys[i], _value);
        }

        _objectResult.Reset();
    }

    [Benchmark]
    public void ObjectResult_Expanded_Init_Set_Clear()
    {
        _objectResultExpanded.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _objectResultExpanded.SetValueUnsafe(i, _keys[i], _value);
        }

        _objectResultExpanded.Reset();
    }
}



[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class TaskPoolBenchmarks
{
    private readonly string _fileNameA = System.IO.Path.GetTempFileName();
    private readonly string _fileNameB = System.IO.Path.GetTempFileName();
    private readonly string _fileNameC = System.IO.Path.GetTempFileName();
    private readonly string _fileNameD = System.IO.Path.GetTempFileName();
    private readonly Foo _foo = new();

    [Benchmark(Baseline = true)]
    public async Task Default_Start()
    {
        var a = Task.Factory.StartNew(
            async () => await Work(_fileNameA),
            default,
            TaskCreationOptions.None,
            TaskScheduler.Default).Unwrap();
        var b = Task.Factory.StartNew(
            async () => await Work(_fileNameB),
            default,
            TaskCreationOptions.None,
            TaskScheduler.Default).Unwrap();
        var c = Task.Factory.StartNew(
            async () => await Work(_fileNameC),
            default,
            TaskCreationOptions.None,
            TaskScheduler.Default).Unwrap();
        var d = Task.Factory.StartNew(
            async () => await Work(_fileNameD),
            default,
            TaskCreationOptions.None,
            TaskScheduler.Default).Unwrap();
        await a;
        await b;
        await c;
        await d;
    }

    [Benchmark]
    public async Task Foo_Start()
    {
        var a = Task.Factory.StartNew(
            async () => await Work(_fileNameA),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var b = Task.Factory.StartNew(
            async () => await Work(_fileNameB),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var c = Task.Factory.StartNew(
            async () => await Work(_fileNameC),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var d = Task.Factory.StartNew(
            async () => await Work(_fileNameD),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        await a;
        await b;
        await c;
        await d;
    }

    [Benchmark]
    public async Task Foo_Start_2()
    {
        var a = Task.Factory.StartNew(
            async () => await Work(_fileNameA),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var b = Task.Factory.StartNew(
            async () => await Work(_fileNameB),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var c = Task.Factory.StartNew(
            async () => await Work(_fileNameC),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        var d = Task.Factory.StartNew(
            async () => await Work(_fileNameD),
            default,
            TaskCreationOptions.None,
            _foo).Unwrap();
        await a;
        await b;
        await c;
        await d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task Work(string fileName)
    {
        await File.WriteAllTextAsync(fileName, "hello");
        await Task.Delay(10);
        await File.ReadAllTextAsync(fileName);
        await Task.Delay(10);
    }
}

// Provides a task scheduler that ensures a maximum concurrency level while
// running on top of the thread pool.
public class Foo : TaskScheduler
{
    public int Running;

    // Queues a task to the scheduler.
    protected sealed override void QueueTask(Task task)
    {
        ThreadPool.UnsafeQueueUserWorkItem(
            t =>
            {
                Interlocked.Increment(ref Running);

                try
                {
                    base.TryExecuteTask(task);
                }
                finally
                {
                    if (Interlocked.Decrement(ref Running) == 0)
                    {
                        // Console.WriteLine("dispatch");
                    }
                }
            }, null);
    }

    // Attempts to execute the specified task on the current thread.
    protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        Interlocked.Increment(ref Running);

        try
        {
            return base.TryExecuteTask(task);
        }
        finally
        {
            if (Interlocked.Decrement(ref Running) == 0)
            {
                // Console.WriteLine("dispatch");
            }
        }
    }

    // Attempt to remove a previously scheduled task from the scheduler.
    protected sealed override bool TryDequeue(Task task)
        => true;

    // Gets an enumerable of the tasks currently scheduled on this scheduler.
    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        throw new NotImplementedException();
    }
}
