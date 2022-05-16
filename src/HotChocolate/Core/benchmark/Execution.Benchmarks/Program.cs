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
            var bench = new ResultDataReadBenchmarks();

            for (var i = 0; i < 1000; i++)
            {
                Console.WriteLine(i);
                bench.Size = 8;
                bench.Init();
                bench.ObjectResult_Optimized_Read();
            }
        }
        */
        {
            BenchmarkRunner.Run<ResultDataBenchmarks>();
        }
    }
}
