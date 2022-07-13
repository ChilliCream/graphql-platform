
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace HotChocolate.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ExtensionDataBenchmarks
{
    private readonly ExtensionData data1 = new ExtensionData { ["abc"] = "abc" };
    private readonly ExtensionData2 data2 = new ExtensionData2 { ["abc"] = "abc" };

    [Benchmark]
    public ExtensionData Create_ExtensionData()
        => new();

    [Benchmark]
    public ExtensionData2 Create_ExtensionData_2()
        => new();

    [Benchmark]
    public ExtensionData Create_ExtensionData_Set_1()
    {
        var data = new ExtensionData();
        data["1"] = "1";
        return data;
    }

    [Benchmark]
    public ExtensionData2 Create_ExtensionData_2_Set_1()
    {
        var data = new ExtensionData2();
        data["1"] = "1";
        return data;
    }

    [Benchmark]
    public ExtensionData Create_ExtensionData_Set_2()
    {
        var data = new ExtensionData();
        for (int i = 0; i < 2; i++)
        {
            data[i.ToString()] = i;
        }
        return data;
    }

    [Benchmark]
    public ExtensionData2 Create_ExtensionData_2_Set_2()
    {
        var data = new ExtensionData2();
        for (int i = 0; i < 2; i++)
        {
            data[i.ToString()] = i;
        }
        return data;
    }

    [Benchmark]
    public ExtensionData Create_ExtensionData_Set_10()
    {
        var data = new ExtensionData();
        for (int i = 0; i < 10; i++)
        {
            data[i.ToString()] = i;
        }
        return data;
    }

    [Benchmark]
    public ExtensionData2 Create_ExtensionData_2_Set_10()
    {
        var data = new ExtensionData2();
        for (int i = 0; i < 10; i++)
        {
            data[i.ToString()] = i;
        }
        return data;
    }

    [Benchmark]
    public object? Create_ExtensionData_Get_1()
    {
        return data1["abc"];
    }

    [Benchmark]
    public object? Create_ExtensionData_2_Get_1()
    {
        return data2["abc"];
    }
}


public class ExtensionData2 : IDictionary<string, object?>
{
    private IDictionary<string, object?>? _;

    private IDictionary<string, object?> Dict() 
        => _ ??= new Dictionary<string, object?>();

    public object? this[string key]
    {
        get => Dict()[key];
        set => Dict()[key] = value;
    }

    public ICollection<string> Keys => throw new NotImplementedException();

    public ICollection<object?> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(string key, object? value)
    {
        throw new NotImplementedException();
    }

    public void Add(KeyValuePair<string, object?> item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<string, object?> item)
    {
        throw new NotImplementedException();
    }

    public bool ContainsKey(string key)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public bool Remove(string key)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, object?> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
