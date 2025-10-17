using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace Fusion.Execution.Benchmarks;

// TODO: It would be great if we could separate the planning from the dependency tree generation and operation compilation.

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class OperationPlannerBenchmark : FusionBenchmarkBase
{
    private const string Id = "123456789101112";

    private OperationPlanner _planner = null!;

    private OperationDefinitionNode _simpleQueryWithRequirements = null!;
    private OperationDefinitionNode _complexQuery = null!;
    private OperationDefinitionNode _conditionalRedundancyQuery = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schema = CreateFusionSchema();

        var documentRewriter = new DocumentRewriter(schema);

        _simpleQueryWithRequirements = documentRewriter.RewriteOperation(CreateSimpleQueryWithRequirementsDocument());
        _complexQuery = documentRewriter.RewriteOperation(CreateComplexDocument());
        _conditionalRedundancyQuery = documentRewriter.RewriteOperation(CreateConditionalRedundancyDocument());

        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationCompiler = new OperationCompiler(schema, pool);

        _planner = new OperationPlanner(schema, operationCompiler);
    }

    [Benchmark]
    public OperationPlan Plan_Simple_Query_With_Requirements()
    {
        return _planner.CreatePlan(Id, Id, Id, _simpleQueryWithRequirements);
    }

    [Benchmark]
    public OperationPlan Plan_Complex_Query()
    {
        return _planner.CreatePlan(Id, Id, Id, _complexQuery);
    }

    [Benchmark]
    public OperationPlan Plan_ConditionalRedundancy_Query()
    {
        return _planner.CreatePlan(Id, Id, Id, _conditionalRedundancyQuery);
    }
}
