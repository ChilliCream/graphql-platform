using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class DocumentRewriterBenchmark : FusionBenchmarkBase
{
    private DocumentRewriter _documentRewriter = null!;

    private DocumentNode _simpleQueryWithRequirements = null!;
    private DocumentNode _complexQuery = null!;
    private DocumentNode _conditionalRedundancyQuery = null!;

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
        return _documentRewriter.RewriteOperation(_simpleQueryWithRequirements);
    }

    [Benchmark]
    public OperationDefinitionNode Rewrite_Complex_Query()
    {
        return _documentRewriter.RewriteOperation(_complexQuery);
    }

    [Benchmark]
    public OperationDefinitionNode Rewrite_ConditionalRedundancy_Query()
    {
        return _documentRewriter.RewriteOperation(_conditionalRedundancyQuery);
    }
}
