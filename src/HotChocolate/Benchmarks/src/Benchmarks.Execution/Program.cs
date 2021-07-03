using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using HotChocolate.Benchmarks;

public static class Program
{
    static void Main(string[] args) =>
        Run().Wait();
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


    private static async Task Run()
    {
        var queryBench = new QueryBenchmarks();

        await queryBench.Sessions_Medium();
        await queryBench.Sessions_Medium();

        Console.WriteLine("Ready");
        Console.ReadLine();

        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(2000);
            await queryBench.Sessions_Medium();
        }
    }
}
