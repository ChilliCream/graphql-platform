using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Buffers;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ResultDataBenchmarks
    {
        [Params(1, 8, 16, 50, 100, 200)]
        public int Size { get; set; }

        [Benchmark]
        public Dictionary<string, object> Create_And_Fill_Dictionary()
        {
            var dictionary = new Dictionary<string, object>();

            for (int i = 0; i < Size; i++)
            {
                dictionary[i.ToString()] = i;
            }

            return dictionary;
        }

        [Benchmark]
        public ResultMap Create_And_Fill_ResultMap()
        {
            ResultValue[] buffer = ArrayPool<ResultValue>.Shared.Rent(Size);
            var resultMap = new ResultMap(Size, buffer);

            for (int i = 0; i < Size; i++)
            {
                resultMap.SetValue(i, i.ToString(), i);
            }

            ArrayPool<ResultValue>.Shared.Return(buffer);
            return resultMap;
        }
    }
}
