using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
public class CostPlanBenchmark
{
    private CostSchemaSnapshot _snapshot = null!;
    private DocumentNode _document = null!;
    private OperationDefinitionNode _operation = null!;
    private CostPlan _warmPlan = null!;
    private BenchmarkVariableValues _warmVariables = null!;
    private BenchmarkVariableValues _rejectionVariables = null!;
    private CostPlan _inputPlan = null!;
    private BenchmarkVariableValues _inputVariables = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schema = BenchmarkFixture.ParseSchema("typical-schema.graphql");
        _snapshot = CostSchemaSnapshot.Create(
            schema,
            new CostEngineOptions { DefaultListSize = 10.0 });
        (_document, _operation) = BenchmarkFixture.ParseOperation("typical-operation.graphql");
        _warmPlan = Compile(_snapshot, _document, _operation);
        _warmVariables = BenchmarkFixture.Variables(
            ("limit", new IntValueNode(10)),
            ("includeDetails", BooleanValueNode.True));
        _rejectionVariables = BenchmarkFixture.Variables(
            ("limit", new IntValueNode(10_000)),
            ("includeDetails", BooleanValueNode.True));

        var inputSchema = BenchmarkFixture.ParseSchema("input-shape-schema.graphql");
        var inputSnapshot = CostSchemaSnapshot.Create(inputSchema, new CostEngineOptions());
        var (inputDocument, inputOperation) =
            BenchmarkFixture.ParseOperation("input-shape-operation.graphql");
        _inputPlan = Compile(inputSnapshot, inputDocument, inputOperation);
        _inputVariables = BenchmarkFixture.Variables(
            ("filter", CreateInputShape()));

        _ = _warmPlan.Evaluate(_warmVariables);
        _ = _inputPlan.Evaluate(_inputVariables);
    }

    [Benchmark]
    public CostEstimate ColdCompileEvaluate()
        => Compile(_snapshot, _document, _operation).Evaluate(_warmVariables);

    [Benchmark]
    public CostEstimate WarmEvaluate()
        => _warmPlan.Evaluate(_warmVariables);

    [Benchmark]
    public CostEstimate InputShapeEvaluate()
        => _inputPlan.Evaluate(_inputVariables);

    [Benchmark]
    public bool RepeatedRejection()
        => _warmPlan.Evaluate(_rejectionVariables).FieldCost > 1_000.0;

    private static CostPlan Compile(
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation)
        => CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);

    private static ObjectValueNode CreateInputShape()
        => new(
            new ObjectFieldNode(
                "all",
                new ListValueNode(
                    new ObjectValueNode(
                        new ObjectFieldNode("field", "title"),
                        new ObjectFieldNode("contains", "graphql")),
                    new ObjectValueNode(
                        new ObjectFieldNode("field", "author"),
                        new ObjectFieldNode("contains", "lee")))),
            new ObjectFieldNode(
                "paging",
                new ObjectValueNode(
                    new ObjectFieldNode("first", 20),
                    new ObjectFieldNode("offset", 40))));
}
