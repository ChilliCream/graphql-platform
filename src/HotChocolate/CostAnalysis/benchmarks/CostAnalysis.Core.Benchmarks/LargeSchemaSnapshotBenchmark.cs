using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
public class LargeSchemaSnapshotBenchmark
{
    private MutableSchemaDefinition _schema = null!;
    private CostEngineOptions _options = null!;

    [Params(1_024, 10_240)]
    public int ObjectTypeCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _schema = SchemaParser.Parse(CreateSchema(ObjectTypeCount));
        _options = new CostEngineOptions();
    }

    [Benchmark]
    public CostSchemaSnapshot LargeSchemaSnapshotBuild()
        => CostSchemaSnapshot.Create(_schema, _options);

    private static string CreateSchema(int objectTypeCount)
    {
        var schema = new StringBuilder("schema { query: Node0 }");

        for (var index = 0; index < objectTypeCount; index++)
        {
            schema
                .Append("\ntype Node")
                .Append(index)
                .Append(" { value: Int }");
        }

        return schema.ToString();
    }
}
