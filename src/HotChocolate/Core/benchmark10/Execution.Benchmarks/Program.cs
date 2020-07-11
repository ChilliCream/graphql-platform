using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HotChocolate.Execution.Benchmarks
{
    class Program
    {

        static void Main(string[] args) =>
           BenchmarkRunner.Run<DefaultExecutionPipelineBenchmark>();



        /*
        static async Task Main(string[] args)
        {
            var bench = new DefaultExecutionPipelineBenchmark();

            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine("starting " + i);
                var time = Stopwatch.StartNew();
                await bench.SchemaIntrospection();
                Console.WriteLine("finished " + i + " " + time.Elapsed);
            }
        }
                         */
    }
}
