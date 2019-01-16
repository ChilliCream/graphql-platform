using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;

namespace HotChocolate.Benchmark.Tests.Misc
{
    [CoreJob]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Roman)]
    public class TimestampBenchmarks
    {
        [Benchmark]
        public long DateTimeUtcNowTicks()
        {
            return DateTime.UtcNow.Ticks;
        }

        [Benchmark]
        public long DateTimeUtcNowTicksToNanoseconds()
        {
            return DateTime.UtcNow.Ticks * 100L;
        }

        [Benchmark]
        public long StopwatchGetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        [Benchmark]
        public long StopwatchGetTimestampToNanoseconds()
        {
            return Stopwatch.GetTimestamp() *
                (1000000000L / Stopwatch.Frequency);
        }
    }
}
