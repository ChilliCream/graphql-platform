using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace HotChocolate.CostAnalysis;

public sealed class InProcessConfig : ManualConfig
{
    public InProcessConfig()
    {
        AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
        AddColumn(StatisticColumn.Median);
    }
}
