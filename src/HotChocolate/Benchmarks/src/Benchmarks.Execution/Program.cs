using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using HotChocolate.Benchmarks;

public static class Program
{
    static void Main(string[] args) =>
        // Run().Wait();
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


    private static async Task Run()
    {
        var queryBench = new QueryBenchmarks();

        for (int i = 0; i < 1000; i++)
        {
            Console.WriteLine($"Executing {i} ...");
            await queryBench.Sessions_TitleAndAbstractAndTrackName();
        }
    }
}