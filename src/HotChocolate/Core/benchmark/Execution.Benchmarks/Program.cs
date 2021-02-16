using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HotChocolate.Execution.Benchmarks
{
    class Program
    {
        static void Main(string[] args) =>
            BenchmarkRunner.Run(typeof(SchemaBuildingBenchmark));
    }
}
