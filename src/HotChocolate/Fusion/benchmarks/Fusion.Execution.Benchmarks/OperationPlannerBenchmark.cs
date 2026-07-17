using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class OperationPlannerBenchmark : FusionBenchmarkBase
{
    private const string Id = "123456789101112";

    private OperationPlanner _planner;

    private OperationDefinitionNode _simpleQueryWithRequirements;
    private OperationDefinitionNode _complexQuery;
    private OperationDefinitionNode _conditionalRedundancyQuery;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schema = CreateFusionSchema();

        var documentRewriter = new DocumentRewriter(schema);

        _simpleQueryWithRequirements = documentRewriter.RewriteDocument(CreateSimpleQueryWithRequirementsDocument()).GetOperation(operationName: null);
        _complexQuery = documentRewriter.RewriteDocument(CreateComplexDocument()).GetOperation(operationName: null);
        _conditionalRedundancyQuery = documentRewriter.RewriteDocument(CreateConditionalRedundancyDocument()).GetOperation(operationName: null);

        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationCompiler = new OperationCompiler(schema, pool);

        _planner = new OperationPlanner(schema, operationCompiler);
    }

    [Benchmark]
    public int Plan_Simple_Query_With_Requirements()
    {
        return _planner.CreatePlan(Id, Id, Id, _simpleQueryWithRequirements).SearchSpace;
    }

    [Benchmark]
    public int Plan_Complex_Query()
    {
        return _planner.CreatePlan(Id, Id, Id, _complexQuery).SearchSpace;
    }

    [Benchmark]
    public int Plan_ConditionalRedundancy_Query()
    {
        return _planner.CreatePlan(Id, Id, Id, _conditionalRedundancyQuery).SearchSpace;
    }
}
