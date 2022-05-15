using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ResultDataBenchmarks
{
    private readonly string[] _data = new string[256];
    private readonly ResultMap _resultMap = new();
    private readonly ResultMap _resultMapInit = new();
    private readonly ResultMap2 _resultMap2 = new();
    private readonly ResultMap2 _resultMap2Init = new();

    public ResultDataBenchmarks ()
    {
        for (var i = 0; i < _data.Length; i++)
        {
            _data[i] = i.ToString();
        }

        _resultMapInit.EnsureCapacity(256);
        _resultMap2Init.EnsureCapacity(256);
    }

    [Params(1,2,3,4, 8, 256)]
    public int Size { get; set; }

    // [Benchmark]
    public void Create_And_Fill_ResultMap()
    {
        _resultMapInit.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMapInit.SetValue(i, _data[i], i);
        }

        _resultMapInit.Clear();
    }

    // [Benchmark]
    public void Create_And_Fill_ResultMap_Init()
    {
        _resultMap.Clear();
        _resultMap.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap.SetValue(i, _data[i], i);
        }
    }

    [Benchmark]
    public void Create_And_Fill_ResultMap_2()
    {
        _resultMap2.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap2.SetValue(i, _data[i], i);
        }

        _resultMap2.Clear();
    }

    [Benchmark]
    public void Create_And_Fill_ResultMap_2_2()
    {
        _resultMap2.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap2.SetValue(i, _data[i], i);
        }

        _resultMap2.Clear2();
    }

    [Benchmark]
    public void Create_And_Fill_ResultMap_2_3()
    {
        _resultMap2.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap2.SetValue(i, _data[i], i);
        }

        _resultMap2.Clear3();
    }


    // [Benchmark]
    public void Create_And_Fill_ResultMap_2_Init()
    {
        _resultMap2Init.Clear();
        _resultMap2Init.EnsureCapacity(Size);

        for (var i = 0; i < Size; i++)
        {
            _resultMap2Init.SetValue(i, _data[i], i);
        }
    }
}

public sealed class ResultMap2
{
    private ResultValue2[] _buffer = { new(), new(), new(), new(), };
    private int _capacity;

    public void EnsureCapacity(int capacity)
    {
        if (_buffer.Length < capacity)
        {
            var oldCapacity = _buffer.Length;
            var newCapacity = _buffer.Length * 2;

            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            Array.Resize(ref _buffer, newCapacity);

            for (var i = oldCapacity; i < _buffer.Length; i++)
            {
                _buffer[i] = new();
            }
        }

        _capacity = capacity;
    }

    internal void SetValue(int index, string name, object value, bool isNullable = true)
    {
        _buffer[index].SetValue(name, value, isNullable);
    }

    public void Clear()
    {
        for (var i = 0; i < _capacity; i++)
        {
            _buffer[i].Reset();
        }
    }

    public void Clear2()
    {
        for (var j = _capacity - 1; j >= 0; j--)
        {
            _buffer[j].Reset();
        }
    }

    public void Clear3()
    {
        var i = (IntPtr)0;
        var length = _capacity;
        ref ResultValue2 searchSpace = ref MemoryMarshal.GetReference(_buffer.AsSpan());

        while (length > 0)
        {
            length--;

            Unsafe.Add(ref searchSpace, i).Reset();
            i += 1;
        }
    }

    private class ResultValue2
    {
        /// <summary>
        /// Gets the name of this <see cref="IResultMap"/> entry.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the value of this <see cref="IResultMap"/> entry.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Specifies if <see cref="Value"/> is allowed to be empty.
        /// </summary>
        public bool IsNullable { get; private set; }

        public void SetValue(string name, object value, bool isNullable)
        {
            Name = name;
            Value = value;
            IsNullable = isNullable;
        }

        public void Reset()
        {
            Name = null;
            Value = null;
            IsNullable = false;
        }
    }
}
