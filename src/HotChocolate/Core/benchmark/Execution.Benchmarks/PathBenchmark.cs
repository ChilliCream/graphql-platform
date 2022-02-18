using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class PathBenchmark
    {
        private readonly List<NameString> _segments = new List<NameString>();

        public PathBenchmark()
        {
            for (var i = 0; i < 100; i++)
            {
                _segments.Add("_" + Guid.NewGuid().ToString("N"));
            }
        }

        [Benchmark]
        public Path[] NoCache()
        {
            var paths = new Path[200];

            for (var i = 0; i < 200; i++)
            {
                Path current = Path.Root;

                foreach (var s in _segments)
                {
                    current = current.Append(s);
                }

                paths[i] = current;
            }

            return paths;
        }

        [Benchmark]
        public Path[] WithCache()
        {
            var paths = new Path[200];

            for (var i = 0; i < 200; i++)
            {
                Path current = Path.Root;

                foreach (var s in _segments)
                {
                    current = current.AppendCache(s);
                }

                paths[i] = current;
            }

            return paths;
        }
    }
}
