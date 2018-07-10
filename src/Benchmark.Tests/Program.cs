using System;
using System.IO;
using System.Reflection;
using System.Text;
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
