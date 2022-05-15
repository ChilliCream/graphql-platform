using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ResultDataReadBenchmarks
{
    private readonly ResultMap _resultMap = new();
    private readonly ObjectResult _objectResult = new();
    private readonly object[] _values = new object[256];

    public ResultDataReadBenchmarks ()
    {
        _resultMap.EnsureCapacity(256);
        _objectResult.EnsureCapacity(256);

        for (var i = 0; i < Size; i++)
        {
            _resultMap.SetValue(i, i.ToString(), i);
            _objectResult.SetValueUnsafe(i, i.ToString(), i);
        }
    }

    [Params(1,2,3,4, 8, 256)]
    public int Size { get; set; }

    [Benchmark]
    public void ResultMap_Optimized_Read()
    {
        ResultMap result = _resultMap;

        for (var i = 0; i < result.Count; i++)
        {
            ResultValue value = result[i];
            if (value.IsInitialized)
            {
                _values[i] = value;
            }
        }
    }

    [Benchmark]
    public void ObjectResult_Optimized_Read()
    {
        ObjectResult result = _objectResult;
        ReadOnlySpan<ObjectFieldResult> fields = result.AsSpan();

        var i = 0;
        var ptr = (IntPtr)0;
        var length = fields.Length;
        ref ObjectFieldResult searchSpace = ref MemoryMarshal.GetReference(fields);

        while (length > 0)
        {
            ObjectFieldResult field = Unsafe.Add(ref searchSpace, ptr);

            if (field.IsInitialized)
            {
                _values[i++] = field;
            }

            length--;
            ptr += 1;
        }
    }
}
