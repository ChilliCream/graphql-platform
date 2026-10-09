using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Base for benchmark configurations that run their single job in process. BenchmarkDotNet
/// 0.15.8 has no RuntimeMoniker for the net11.0 preview host and this project pins
/// TargetFramework to net11.0, so out-of-process toolchains can neither validate nor build
/// a child process here.
/// </summary>
public abstract class InProcessBenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Adds <paramref name="job"/> to the configuration, running it with the in-process
    /// emit toolchain.
    /// </summary>
    protected InProcessBenchmarkConfig(Job job)
        => AddJob(job.WithToolchain(InProcessEmitToolchain.Instance));
}

/// <summary>
/// Runs a BenchmarkDotNet short run job in process.
/// </summary>
public sealed class InProcessShortRunConfig : InProcessBenchmarkConfig
{
    public InProcessShortRunConfig() : base(Job.ShortRun)
    {
    }
}
