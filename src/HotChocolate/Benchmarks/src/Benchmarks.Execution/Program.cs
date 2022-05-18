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
    static async Task Main(string[] args) => await Run().ConfigureAwait(false);
    // static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

    private static async Task Run()
    {
        Console.WriteLine("Started");

        var bench = new IntrospectionBenchmarks();
        var list = new List<Task>();

        for (int i = 0; i < 400; i++)
        {
            list.Add(Run(bench));
        }

        await Task.WhenAll(list);

        Console.WriteLine("Done");
        Console.ReadLine();
    }

    private static async Task Run(IntrospectionBenchmarks bench)
    {
        await Task.Yield();

        for (int i = 0; i < 1000; i++)
        {
            await bench.Query_Introspection();
        }
    }
}
