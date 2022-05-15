using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ResultDataBenchmarks
{
    private readonly string[] _data = new string[256];
    private readonly ResultMap _resultMap = new();
    private readonly ResultMap _resultMapExpanded = new();
    private readonly ObjectResult _objectResult = new();
    private readonly ObjectResult _objectResultExpanded = new();

    public ResultDataBenchmarks ()
    {
        for (var i = 0; i < _data.Length; i++)
        {
            _data[i] = i.ToString();
        }

        _resultMapExpanded.EnsureCapacity(256);
        _objectResultExpanded.EnsureCapacity(256);
    }

    [Params(1,2,3,4, 8, 256)]
    public int Size { get; set; }

    [Benchmark]
    public void ResultMap_Init_Set_Clear()
    {
        _resultMap.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap.SetValue(i, _data[i], i);
        }

        _resultMap.Clear();
    }

    [Benchmark]
    public void ResultMap_Expanded_Init_Set_Clear()
    {
        _resultMapExpanded.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMapExpanded.SetValue(i, _data[i], i);
        }

        _resultMapExpanded.Clear();
    }

    [Benchmark]
    public void ObjectResult_Init_Set_Clear()
    {
        _objectResult.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _objectResult.SetValueUnsafe(i, _data[i], i);
        }

        _objectResult.Reset();
    }

    [Benchmark]
    public void ObjectResult_Expanded_Init_Set_Clear()
    {
        _objectResultExpanded.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _objectResultExpanded.SetValueUnsafe(i, _data[i], i);
        }

        _objectResultExpanded.Reset();
    }
}
