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
using HotChocolate.ConferencePlanner;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.Types.Descriptors.Definitions;

public static class Program
{
    // static async Task Main(string[] args) => await Run().ConfigureAwait(false);
    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

    private static async Task Run()
    {
        Console.WriteLine("Initialize");
        var queryBench = new QueryBenchmarks();

        Console.WriteLine("Warmup 1");
        await queryBench.Sessions_DataLoader_Large().ConfigureAwait(false);
        ;

        Console.WriteLine("Warmup 2");
        await queryBench.Sessions_DataLoader_Large().ConfigureAwait(false);
        ;

        Console.WriteLine("Run");

        var list = new ConcurrentBag<TimeSpan>();
        TimeSpan longest = TimeSpan.Zero;
        int longestStarts = 0;
        int longestBatches = 0;
        TimeSpan shortest = TimeSpan.MaxValue;
        int shortestStarts = 0;
        int shortestBatches = 0;
        int awesome = 0;
        int good = 0;
        int bad = 0;

        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine("Start ---------------------");
            var time = await RunItem(queryBench, list).ConfigureAwait(false);
            Console.WriteLine("End -----------------------");
            Console.WriteLine(i);
            Console.WriteLine();

            if (longest < time)
            {
                longest = time;
                longestStarts = BatchExecutionDiagnostics.Starts;
                longestBatches = BatchDataLoaderDiagnostics.Batches;
            }

            if (shortest > time)
            {
                shortest = time;
                shortestStarts = BatchExecutionDiagnostics.Starts;
                shortestBatches = BatchDataLoaderDiagnostics.Batches;
            }

            if (BatchExecutionDiagnostics.Starts < 11)
            {
                awesome++;
            }
            else if (BatchExecutionDiagnostics.Starts < 15)
            {
                good++;
            }
            else
            {
                bad++;
            }
        }

        Console.WriteLine(list.Sum(t => t.Milliseconds) / list.Count);
        Console.WriteLine($"Shortest: {shortest}/{shortestStarts}/{shortestBatches}");
        Console.WriteLine($"Longest: {longest}/{longestStarts}/{longestBatches}");
        Console.WriteLine($"{awesome}/{good}/{bad}");
    }

    private static async Task<TimeSpan> RunItem(QueryBenchmarks bench, ConcurrentBag<TimeSpan> list)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Restart();
        await bench.Sessions_DataLoader_Large().ConfigureAwait(false);
        list.Add(stopwatch.Elapsed);
        Console.WriteLine(stopwatch.Elapsed);
        return stopwatch.Elapsed;
    }

}
