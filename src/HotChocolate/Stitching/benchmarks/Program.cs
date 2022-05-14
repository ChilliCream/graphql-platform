using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using HotChocolate.Stitching.Types;

namespace HotChocolate.Execution.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //var renameTest = new RenameTest();
            //Console.WriteLine("Ready");
            //Console.ReadLine();
            //renameTest.Test();
            //Console.WriteLine("Done");
            //Console.ReadLine();

            BenchmarkRunner.Run<RenameBenchmark>();
        }
    }

    [CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class RenameBenchmark
    {
        private readonly RenameTest TestInstance = new();

        [Benchmark]
        public void SchemaLoad()
        {
            TestInstance.SchemaLoad();
        }

        [Benchmark]
        public void TestTypeRename()
        {
            TestInstance.TestTypeRename();
        }

        [Benchmark]
        public void TestFieldRename()
        {
            TestInstance.TestFieldRename();
        }

        [Benchmark]
        public void TestFull()
        {
            TestInstance.TestFull();
        }
    }
}
