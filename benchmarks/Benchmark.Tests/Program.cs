using BenchmarkDotNet.Running;
using HotChocolate.Benchmark.Tests.Execution;
using HotChocolate.Benchmark.Tests.Language;
using HotChocolate.Benchmark.Tests.Misc;

namespace HotChocolate.Benchmark.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParserBenchmarks>();
            BenchmarkRunner.Run<LexerBenchmarks>();
            BenchmarkRunner.Run<QueryExecutorWithCacheBenchmarks>();
            BenchmarkRunner.Run<QueryExecutorBenchmarks>();
            BenchmarkRunner.Run<TimestampBenchmarks>();
        }
    }
}
