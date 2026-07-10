using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class InlineFragmentOperationRewriterBenchmark : FusionBenchmarkBase
{
    private InlineFragmentOperationRewriter _rewriter;

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

        _rewriter = new InlineFragmentOperationRewriter(schema);
    }

    [Benchmark]
    public InlineFragmentOperationRewriterResult Rewrite_Simple_Query_With_Requirements()
    {
        return _rewriter.RewriteDocument(_simpleQueryWithRequirements);
    }

    [Benchmark]
    public InlineFragmentOperationRewriterResult Rewrite_Complex_Query()
    {
        return _rewriter.RewriteDocument(_complexQuery);
    }

    [Benchmark]
    public InlineFragmentOperationRewriterResult Rewrite_ConditionalRedundancy_Query()
    {
        return _rewriter.RewriteDocument(_conditionalRedundancyQuery);
    }
}
