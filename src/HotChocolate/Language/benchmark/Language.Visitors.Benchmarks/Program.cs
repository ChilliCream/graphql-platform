using BenchmarkDotNet.Running;

namespace HotChocolate.Language.Visitors.Benchmarks;

class Program
{
    static void Main(string[] args) =>
        BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
}
