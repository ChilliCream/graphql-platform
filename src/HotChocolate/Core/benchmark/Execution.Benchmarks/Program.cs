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
            var bench = new ResultDataBenchmarks();

            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine(i);
                bench.Create_And_Fill_ResultMap_2_Init();
            }
        }
        */
        {
            BenchmarkRunner.Run<ResultDataReadBenchmarks>();
        }
    }
}
