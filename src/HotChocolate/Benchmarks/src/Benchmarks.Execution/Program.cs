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

        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(2000);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Executing {i} ...");
            await queryBench.Sessions_Medium();
        }
    }
}