using System;
using BenchmarkDotNet.Running;

namespace HotChocolate.Benchmark.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParserBenchmarks>();
            BenchmarkRunner.Run<LexerBenchmarks>();
        }
    }
}
