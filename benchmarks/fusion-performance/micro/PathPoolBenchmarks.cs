using System.Reflection;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Performance;

[MemoryDiagnoser]
[Config(typeof(PathPoolConstructionConfig))]
public class PathPoolConstructionBenchmark
{
    [GlobalSetup]
    public void Setup() => PathPoolFixture.VerifyAssembly();

    [Benchmark]
    public void ConstructAndDispose()
    {
        using var pool = PathPoolFixture.Create();
        GC.KeepAlive(pool);
    }
}

[MemoryDiagnoser]
[Config(typeof(PathPoolGrowthConfig))]
public class PathPoolGrowthBenchmark
{
    private readonly int[]?[] _arrays = new int[]?[6145];
    private PathSegmentPool _pool = null!;
    private Action _trim = null!;

    [GlobalSetup]
    public void Setup()
    {
        PathPoolFixture.VerifyAssembly();
        _pool = PathPoolFixture.Create();
        var bucket = typeof(PathSegmentPool)
            .GetField("_bucket", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_pool)!;
        _trim = bucket.GetType()
            .GetMethod("Trim", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action>(bucket);
        GrowReturnAndTrim();
    }

    [Benchmark]
    public void GrowReturnAndTrim()
    {
        _pool.Rent(_arrays);
        _pool.Return(_arrays);
        _trim();
        _trim();
    }

    [GlobalCleanup]
    public void Cleanup() => _pool.Dispose();
}

internal static class PathPoolFixture
{
    private static readonly int[] s_levels =
    [
        4096, 6144, 9216, 13824, 20736, 31104, 46656, 69984, 104976, 157464,
        236196, 354294, 531441, 797162, 1195743, 1793614, 2690422, 4035632,
        6053449, 9080173, 13620260
    ];

    internal static PathSegmentPool Create()
        => new(64, s_levels, TimeSpan.FromDays(1), false);

    internal static void VerifyAssembly()
    {
        var path = typeof(PathSegmentPool).Assembly.Location;
        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Console.WriteLine($"Measured Fusion assembly: {path}; SHA256={hash}");
        if (hash != Environment.GetEnvironmentVariable("FUSION_PERF_ASSEMBLY_SHA256"))
        {
            throw ThrowHelper.AssemblyMismatch();
        }
    }
}

internal static class ThrowHelper
{
    internal static InvalidOperationException AssemblyMismatch()
        => new("The microbenchmark must use the frozen gateway's Fusion assembly.");
}

public sealed class PathPoolConstructionConfig : ManualConfig
{
    public PathPoolConstructionConfig()
    {
        AddJob(PathPoolJobs.Create().WithInvocationCount(1).WithUnrollFactor(1));
    }
}

public sealed class PathPoolGrowthConfig : ManualConfig
{
    public PathPoolGrowthConfig()
    {
        AddJob(PathPoolJobs.Create());
    }
}

internal static class PathPoolJobs
{
    internal static Job Create()
        => Job.Default
            .WithRuntime(CoreRuntime.Core10_0)
            .WithGcServer(true)
            .WithGcConcurrent(true)
            .WithEnvironmentVariable("DOTNET_GCDynamicAdaptationMode", "0")
            .WithLaunchCount(3)
            .WithWarmupCount(5)
            .WithIterationCount(15);
}
