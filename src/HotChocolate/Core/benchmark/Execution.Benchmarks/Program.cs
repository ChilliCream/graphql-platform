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
                await bench.SchemaIntrospection();
            }
        }

                 */

    }
}
