using BenchmarkDotNet.Attributes;
using HotChocolate.Language.Visitors.Benchmarks.Resources;

namespace HotChocolate.Language.Visitors.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class WalkerBenchmarks
{
    private readonly BenchmarkSyntaxWalkerV1 _walkerV1 = new();
    private readonly BenchmarkSyntaxWalkerV2 _walkerV2 = new();
    private readonly BenchmarkSyntaxWalkerV3 _walkerV3 = new();
    private readonly DocumentNode _introspectionQuery;

    public WalkerBenchmarks()
    {
        var resources = new ResourceHelper();
        _introspectionQuery = Utf8GraphQLParser.Parse(
            resources.GetResourceString("IntrospectionQuery.graphql"));
    }

    [Benchmark]
    public void Walker_V1()
        => _walkerV1.Visit(_introspectionQuery, null);

    [Benchmark]
    public void Walker_V2()
        => _introspectionQuery.Accept(_walkerV2);


    [Benchmark]
    public void Walker_V3()
        => _walkerV3.Visit(_introspectionQuery);
}
