using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class DocumentRewriterBenchmark : FusionBenchmarkBase
{
    private DocumentRewriter _documentRewriter;

    private DocumentNode _simpleQueryWithRequirements;
    private DocumentNode _complexQuery;
    private DocumentNode _conditionalRedundancyQuery;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simpleQueryWithRequirements = CreateSimpleQueryWithRequirementsDocument();
        _complexQuery = CreateComplexDocument();
        _conditionalRedundancyQuery = CreateConditionalRedundancyDocument();

        var schema = CreateFusionSchema();

        _documentRewriter = new DocumentRewriter(schema);
    }

    [Benchmark]
    public OperationDefinitionNode Rewrite_Simple_Query_With_Requirements()
    {
        return _documentRewriter.RewriteDocument(_simpleQueryWithRequirements).GetOperation(operationName: null);
    }

    [Benchmark]
    public OperationDefinitionNode Rewrite_Complex_Query()
    {
        return _documentRewriter.RewriteDocument(_complexQuery).GetOperation(operationName: null);
    }

    [Benchmark]
    public OperationDefinitionNode Rewrite_ConditionalRedundancy_Query()
    {
        return _documentRewriter.RewriteDocument(_conditionalRedundancyQuery).GetOperation(operationName: null);
    }
}
