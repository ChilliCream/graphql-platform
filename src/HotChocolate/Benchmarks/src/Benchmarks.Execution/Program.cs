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
        var bench = new IntrospectionBenchmarks();
        await bench.Query_Introspection();
    }
}
