﻿using System;
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
            BenchmarkRunner.Run<DefaultExecutionPipelineBenchmark>();
        }
    }
}
