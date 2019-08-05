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
    public class CompoundKeyEqualBenchmarks
    {
        private CompoundKey _compoundKey;
        private object _compoundKeyObject;
        private CacheKeyA<CompoundKey> _compoundKeyCacheKeyA;
        private CacheKeyB<CompoundKey> _compoundKeyCacheKeyB;
        private object _integerObject;
        private CacheKeyA<CompoundKey> _integerCacheKeyA;
        private CacheKeyB<CompoundKey> _integerCacheKeyB;
        private object _stringObject;
        private CacheKeyA<CompoundKey> _stringCacheKeyA;
        private CacheKeyB<CompoundKey> _stringCacheKeyB;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _compoundKey = new CompoundKey
            {
                Integer = new Random(42).Next(),
                String = Guid.NewGuid().ToString("N")
            };
            _compoundKeyObject = _compoundKey;
            _compoundKeyCacheKeyA = _compoundKey;
            _compoundKeyCacheKeyB = _compoundKey;
            _integerObject = _compoundKey.Integer;
            _integerCacheKeyA = _compoundKey.Integer;
            _integerCacheKeyB = _compoundKey.Integer;
            _stringObject = _compoundKey.String;
            _stringCacheKeyA = new CacheKeyA<CompoundKey>(_compoundKey.String);
            _stringCacheKeyB = _compoundKey.String;
        }

        [Benchmark]
        public bool CompoundKeyToObject()
        {
            return _compoundKeyObject.Equals(_compoundKey);
        }

        [Benchmark]
        public bool IntegerToObject()
        {
            return _integerObject.Equals(_compoundKey.Integer);
        }

        [Benchmark]
        public bool StringToObject()
        {
            return _stringObject.Equals(_compoundKey.String);
        }

        [Benchmark]
        public bool CompoundKeyToCacheKeyA()
        {
            return _compoundKeyCacheKeyA.Equals(
                (CacheKeyA<CompoundKey>)_compoundKey);
        }

        [Benchmark]
        public bool IntegerToCacheKeyA()
        {
            return _integerCacheKeyA.Equals(
                (CacheKeyA<CompoundKey>)_compoundKey.Integer);
        }

        [Benchmark]
        public bool StringToCacheKeyA()
        {
            return _stringCacheKeyA.Equals(
                new CacheKeyA<CompoundKey>(_compoundKey.String));
        }

        [Benchmark]
        public bool CompoundKeyToCacheKeyB()
        {
            return _compoundKeyCacheKeyB.Equals(
                (CacheKeyB<CompoundKey>)_compoundKey);
        }

        [Benchmark]
        public bool IntegerToCacheKeyB()
        {
            return _integerCacheKeyB.Equals(
                (CacheKeyB<CompoundKey>)_compoundKey.Integer);
        }

        [Benchmark]
        public bool StringToCacheKeyB()
        {
            return _stringCacheKeyB.Equals(
                (CacheKeyB<CompoundKey>)_compoundKey.String);
        }
    }
}
