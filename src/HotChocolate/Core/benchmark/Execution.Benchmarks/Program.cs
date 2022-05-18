using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HotChocolate.Execution.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
            /*
            {
                var bench = new DefaultExecutionPipelineBenchmark();

                for (int i = 0; i < 1000; i++)
                {
                    Console.WriteLine(i);
                    bench.GetHeroWithFriends();
                }
            }
            */
        {

            /*
            var b = new NamePathBenchmark();
            b.Size = 8;
            for (var i = 0; i < 1_000_000; i++)
            {
                b.Threadsafe_CreatePath();
            }

            b.Size = 256;
            for (var i = 0; i < 100_000; i++)
            {
                b.Threadsafe_CreatePath();
            }

            b.Size = 16384;
            for (var i = 0; i < 10_000; i++)
            {
                b.Threadsafe_CreatePath();
            }
            */

            BenchmarkRunner.Run<NamePathBenchmark>();
        }
    }
}
