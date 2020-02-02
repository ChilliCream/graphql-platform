using BenchmarkDotNet.Running;

namespace GreenDonut.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<CompoundKeyBenchmarks>();
            BenchmarkRunner.Run<CompoundKeyEqualBenchmarks>();
        }
    }
}
