using BenchmarkDotNet.Running;

namespace HotChocolate.Execution.Benchmarks
{
    class Program
    {
        static void Main(string[] args) =>
            BenchmarkRunner.Run<FieldCollectorBenchmarks>();
    }
}
