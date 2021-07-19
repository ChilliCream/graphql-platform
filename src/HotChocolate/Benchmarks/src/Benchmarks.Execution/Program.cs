using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using HotChocolate;
using HotChocolate.Benchmarks;

public static class Program
{
    static void Main(string[] args) =>
        //Run().Wait();
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

    private static async Task Run()
    {
        var queryBench = new QueryBenchmarks();

        await queryBench.Sessions_Medium();
        await queryBench.Sessions_Medium();

        Console.WriteLine("Ready");
        Console.ReadLine();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 50; i++)
        {
            stopwatch.Restart();
            await queryBench.Sessions_Medium();
            Console.WriteLine(stopwatch.Elapsed);
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
