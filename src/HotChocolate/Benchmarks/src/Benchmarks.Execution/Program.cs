using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using HotChocolate;
using HotChocolate.Benchmarks;
using HotChocolate.ConferencePlanner.DataLoader;

public static class Program
{
    static void Main(string[] args) =>
        // Run().Wait();
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

    private static async Task Run()
    {
        Console.WriteLine("Initialize");
        var queryBench = new QueryBenchmarks();

        Console.WriteLine("Warmup 1");
        await queryBench.Sessions_Large();

        Console.WriteLine("Warmup 2");
        await queryBench.Sessions_Large();

        Console.WriteLine("Run");

        var list = new ConcurrentBag<TimeSpan>();

        for (int i = 0; i < 50; i++)
        {
            Console.WriteLine("Start ---------------------");
            await RunItem(queryBench, list);
            Console.WriteLine("End -----------------------");
            Console.WriteLine();
        }

        Console.WriteLine(list.Sum(t => t.Milliseconds) / list.Count);
        Console.WriteLine($"{list.Count + 2} runs {Counter.Count} one item batches.");
    }

    private static async Task RunItem(QueryBenchmarks bench, ConcurrentBag<TimeSpan> list)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Restart();
        await bench.Sessions_Large();
        list.Add(stopwatch.Elapsed);
        Console.WriteLine(stopwatch.Elapsed);
    }

}
