using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Performance;

[MemoryDiagnoser]
[Config(typeof(SegmentTablesConfig))]
public class SegmentTablesBenchmark
{
    [Params(2048, 5000)]
    public int Records { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var assembly = typeof(MemoryArena).Assembly.Location;
        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly)));
        Console.WriteLine($"Measured buffers assembly: {assembly}; SHA256={hash}");
        if (hash != Environment.GetEnvironmentVariable("FUSION_PERF_ASSEMBLY_SHA256"))
        {
            throw ThrowHelper.InvalidFixture();
        }

        RentAndReturn();
    }

    [Benchmark]
    public void RentAndReturn()
    {
        using var arena = new MemoryArena();
        for (var i = 0; i < Records; i++)
        {
            var data = arena.RentSegmentTable(1);
            var metadata = arena.RentSegmentTable(9);
            if (data[0].Buffer is not null || metadata[0].Buffer is not null)
            {
                throw ThrowHelper.InvalidFixture();
            }
        }

        arena.Seal();
    }
}

public sealed class SegmentTablesConfig : ManualConfig
{
    public SegmentTablesConfig()
    {
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core10_0)
            .WithGcServer(true)
            .WithGcConcurrent(true)
            .WithEnvironmentVariable("DOTNET_GCDynamicAdaptationMode", "0")
            .WithLaunchCount(3)
            .WithWarmupCount(5)
            .WithIterationCount(15));
    }
}

internal static class ThrowHelper
{
    internal static InvalidOperationException InvalidFixture()
        => new("The segment-table benchmark must use the expected assembly and cleared tables.");
}
