using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Pooling;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ResultDataReadBenchmarks
{
    private readonly ObjectResult _objectResult = new();
    private readonly object[] _values = new object[256];

    [Params(1, 2, 3, 4, 8, 16, 32, 64, 256)]
    public int Size { get; set; }

    [IterationSetup]
    public void Init()
    {
        var dummy = new object();

        _objectResult.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _objectResult.SetValueUnsafe(i, i.ToString(), dummy);
        }
    }

    [Benchmark]
    public void ObjectResult_Optimized_Read()
    {
        for (var x = 0; x < 1000; x++)
        {
            ObjectResult result = _objectResult;

            var i = 0;
            var ptr = (IntPtr)0;
            var length = Size;
            ref ObjectFieldResult searchSpace = ref result.GetReference();

            while (length > 0)
            {
                ObjectFieldResult field = Unsafe.Add(ref searchSpace, ptr);

                if (field.IsInitialized)
                {
                    _values[i++] = field.Value;
                }

                length--;
                ptr += 1;
            }
        }
    }

    [Benchmark]
    public void ObjectResult_Optimized2_Read()
    {
        for (var x = 0; x < 1000; x++)
        {
            ObjectResult result = _objectResult;

            if (result.Capacity > 4)
            {
                var i = 0;
                var ptr = (IntPtr)0;
                var length = Size;
                ref ObjectFieldResult searchSpace = ref result.GetReference();

                while (length > 0)
                {
                    ObjectFieldResult field = Unsafe.Add(ref searchSpace, ptr);

                    if (field.IsInitialized)
                    {
                        _values[i++] = field.Value;
                    }

                    length--;
                    ptr += 1;
                }
            }
            else
            {
                var j = 0;

                for (var i = 0; i < result.Capacity; i++)
                {
                    ObjectFieldResult field = result[i];

                    if (field.IsInitialized)
                    {
                        _values[j++] = field.Value;
                    }
                }
            }
        }
    }

    [Benchmark]
    public void ObjectResult_For_Read()
    {
        for (var x = 0; x < 1000; x++)
        {
            ObjectResult result = _objectResult;

            var j = 0;

            for (var i = 0; i < result.Capacity; i++)
            {
                ObjectFieldResult field = result[i];

                if (field.IsInitialized)
                {
                    _values[j++] = field.Value;
                }
            }
        }
    }
}
