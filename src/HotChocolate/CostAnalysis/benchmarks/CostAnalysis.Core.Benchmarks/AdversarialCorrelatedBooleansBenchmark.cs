using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
public class AdversarialCorrelatedBooleansBenchmark
{
    private CostSchemaSnapshot _snapshot = null!;
    private DocumentNode _document = null!;
    private OperationDefinitionNode _operation = null!;
    private BenchmarkVariableValues _variables = null!;

    [Params(4, 8, 12, 13, 20)]
    public int VariableCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schema = BenchmarkFixture.ParseSchema("adversarial-schema.graphql");
        _snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        (_document, _operation) =
            BenchmarkFixture.ParseOperationSource(CreateOperation(VariableCount));
        _variables = BenchmarkFixture.Variables(
            Enumerable.Range(0, VariableCount)
                .Select(index => ($"branch{index}", (IValueNode)BooleanValueNode.True))
                .ToArray());
    }

    [Benchmark]
    public CostEstimate AdversarialCorrelatedBooleans()
        => CostPlanCompiler
            .Compile(_snapshot, _document, _operation, CostAnalyses.Cost)
            .Evaluate(_variables);

    private static string CreateOperation(int variableCount)
    {
        var operation = new StringBuilder("query Benchmark(");

        for (var index = 0; index < variableCount; index++)
        {
            if (index > 0)
            {
                operation.Append(',');
            }

            operation.Append("$branch").Append(index).Append(": Boolean!");
        }

        operation.Append(") { subject { ... on Left {");

        for (var index = 0; index < variableCount; index++)
        {
            operation
                .Append(" left")
                .Append(index)
                .Append(": value @include(if: $branch")
                .Append(index)
                .Append(')');
        }

        operation.Append(" } ... on Right {");

        for (var index = 0; index < variableCount; index++)
        {
            operation
                .Append(" right")
                .Append(index)
                .Append(": value @skip(if: $branch")
                .Append(index)
                .Append(')');
        }

        return operation.Append(" } } }").ToString();
    }
}
