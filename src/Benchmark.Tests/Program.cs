using System;
using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Running;
using HotChocolate.Benchmark.Tests.Execution;
using HotChocolate.Benchmark.Tests.Language;

namespace HotChocolate.Benchmark.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParserBenchmarks>();
            BenchmarkRunner.Run<LexerBenchmarks>();
            // BenchmarkRunner.Run<QueryExecuterWithCacheBenchmarks>();
            // BenchmarkRunner.Run<QueryExecuterBenchmarks>();
        }
    }
}
