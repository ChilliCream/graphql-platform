using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace Fusion.Execution.Benchmarks;

internal sealed class PercentilesConfig : ManualConfig
{
    public PercentilesConfig()
    {
        AddColumn(StatisticColumn.P95);
        AddExporter(CsvMeasurementsExporter.Default);
        AddExporter(RPlotExporter.Default);
    }
}
