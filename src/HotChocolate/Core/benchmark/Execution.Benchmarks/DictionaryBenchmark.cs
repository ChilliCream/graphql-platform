using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class DictionaryBenchmark
    {
        [Params(0, 1, 2, 3, 4)]
        public int Size { get; set; }

        [Benchmark]
        public Dictionary<string, object> Dictionary()
        {
            var list = new Dictionary<string, object>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }

        [Benchmark]
        public ExtensionData ExtensionData()
        {
            var list = new ExtensionData();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }

        [Benchmark]
        public ExtensionData2 ExtensionData2()
        {
            var list = new ExtensionData2();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }

        [Benchmark]
        public ExtensionData3 ExtensionData3()
        {
            var list = new ExtensionData3();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }

        [Benchmark]
        public ExtensionData4 ExtensionData4()
        {
            var list = new ExtensionData4();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }

        [Benchmark]
        public ExtensionData5 ExtensionData5()
        {
            var list = new ExtensionData5();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i.ToString(), i);
            }

            return list;
        }
    }

    public class ExtensionData2
    {
        private Dictionary<string, object?> _data;

        public ExtensionData2()
        {
        }

        public void Add(string key, object value)
        {
            _data ??= new Dictionary<string, object?>();
            _data.Add(key, value);
        }
    }

    public class ExtensionData3
    {
        private Dictionary<string, object?> _data;
        private string _key;
        private object _value;
        private int _count = 0;

        public ExtensionData3()
        {
        }

        public void Add(string key, object value)
        {
            _count++;

            if (_count == 1)
            {
                _key = key;
                _value = value;
                return;
            }

            if (_count == 2)
            {
                _data ??= new Dictionary<string, object?>();
                _data.Add(_key, _value);
                _key = null;
                _value = null;
            }

            _data.Add(key, value);
        }
    }

    public class ExtensionData4
    {
        private Dictionary<string, object?> _data;
        private string _key1;
        private object _value1;
        private string _key2;
        private object _value2;
        private byte _state;

        public ExtensionData4()
        {
        }

        public void Add(string key, object value)
        {
            if (_state == 0)
            {
                _key1 = key;
                _value1 = value;
                _state = 1;
                return;
            }

            if (_state == 1)
            {
                _key1 = key;
                _value1 = value;
                _state = 2;
                return;
            }

            if (_state == 2)
            {
                _data ??= new Dictionary<string, object?>();
                _data.Add(_key1, _value1);
                _data.Add(_key2, _value2);
                _key1 = null;
                _value1 = null;
                _key2 = null;
                _value2 = null;
                _state = 3;
            }

            _data.Add(key, value);
        }
    }

    public class ExtensionData5
    {
        private Dictionary<string, object?> _data;
        private KeyValuePair<string, object?>? _first;

        public ExtensionData5()
        {
        }

        public void Add(string key, object value)
        {
            if (_data is not null)
            {
                _data.Add(key, value);
            }
            else if (_first is not null)
            {
                _data = new Dictionary<string, object?>
                {
                    { _first.Value.Key, _first.Value.Value },
                    { key, value } 
                };
                _first = null;
            }
            else
            {
                _first = new KeyValuePair<string, object?>(key, value);
            }
        }
    }
}
