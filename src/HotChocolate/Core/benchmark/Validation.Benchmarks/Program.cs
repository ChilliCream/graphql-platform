using BenchmarkDotNet.Running;

namespace HotChocolate.Validation.Benchmarks
{
    class Program
    {
        static void Main(string[] args) =>
            BenchmarkRunner.Run<ValidationBenchmarks>();
    }
}
