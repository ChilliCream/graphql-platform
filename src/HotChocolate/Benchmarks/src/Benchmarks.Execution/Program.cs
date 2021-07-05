using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using HotChocolate;
using HotChocolate.Benchmarks;

public static class Program
{
    static void Main(string[] args) =>
        Run().Wait();
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


    private static async Task Run()
    {
        var queryBench = new QueryBenchmarks();

        var executor = await queryBench.ExecutorResolver.GetRequestExecutorAsync();
        
        File.WriteAllText(
            "schema.graphql",
            SchemaSerializer.SerializeSchema(executor.Schema, printResolverKind: true).ToString(true));
    }
}