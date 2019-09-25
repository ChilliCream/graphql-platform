using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;

namespace GreenDonut.Benchmarks
{
    [CoreJob]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Roman)]
    [RPlotExporter, MemoryDiagnoser]
    public class CompoundKeyBenchmarks
    {
        private CompoundKey _compoundKey;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _compoundKey = new CompoundKey
            {
                Integer = new Random(42).Next(),
                String = Guid.NewGuid().ToString("N")
            };
        }

        [Benchmark]
        public object CompoundKeyToObject()
        {
            return _compoundKey;
        }

        [Benchmark]
        public object IntegerToObject()
        {
            return _compoundKey.Integer;
        }

        [Benchmark]
        public object StringToObject()
        {
            return _compoundKey.String;
        }

        [Benchmark]
        public CacheKeyA<CompoundKey> CompoundKeyToCacheKeyA()
        {
            return _compoundKey;
        }

        [Benchmark]
        public CacheKeyA<CompoundKey> IntegerToCacheKeyA()
        {
            return _compoundKey.Integer;
        }

        [Benchmark]
        public CacheKeyA<CompoundKey> StringToCacheKeyA()
        {
            return new CacheKeyA<CompoundKey>(_compoundKey.String);
        }

        [Benchmark]
        public CacheKeyB<CompoundKey> CompoundKeyToCacheKeyB()
        {
            return _compoundKey;
        }

        [Benchmark]
        public CacheKeyB<CompoundKey> IntegerToCacheKeyB()
        {
            return _compoundKey.Integer;
        }

        [Benchmark]
        public CacheKeyB<CompoundKey> StringToCacheKeyB()
        {
            return (CacheKeyB<CompoundKey>)_compoundKey.String;
        }
    }
}
