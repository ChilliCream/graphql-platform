using System.Buffers;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ResultDataBenchmarks
    {
        private readonly ResultMap _resultMap = new ResultMap();

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
        public ResultValue[] Create_And_Fill_ArrayPool()
        {
            ResultValue[] buffer = ArrayPool<ResultValue>.Shared.Rent(Size);

            for (int i = 0; i < Size; i++)
            {
                buffer[i] = new ResultValue(i.ToString(), i, true);
            }

            ArrayPool<ResultValue>.Shared.Return(buffer);
            return buffer;
        }

        [Benchmark]
        public ResultMap Create_And_Fill_ResultMap()
        {
            _resultMap.EnsureCapacity(Size);

            for (int i = 0; i < Size; i++)
            {
                _resultMap.SetValue(i, i.ToString(), i);
            }

            _resultMap.Reset();
            return _resultMap;
        }
    }
}
