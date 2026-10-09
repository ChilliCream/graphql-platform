using BenchmarkDotNet.Jobs;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Runs the BenchmarkDotNet default job in process, with automatic warmup and at least
/// fifteen measurement iterations.
/// </summary>
public class InProcessDefaultRunConfig : InProcessBenchmarkConfig
{
    public InProcessDefaultRunConfig() : base(Job.Default.WithMinIterationCount(15))
    {
    }
}
