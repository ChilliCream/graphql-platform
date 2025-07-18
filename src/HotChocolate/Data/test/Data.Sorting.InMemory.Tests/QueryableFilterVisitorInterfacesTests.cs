using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorInterfacesTests : IClassFixture<SchemaCache>
{
    private static readonly BarInterface[] s_barEntities =
    [
        new() { Test = new InterfaceImpl1 { Prop = "a" } },
        new() { Test = new InterfaceImpl1 { Prop = "b" } }
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorInterfacesTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_InterfaceStringEqual_Expression()
    {
        // arrange
        var tester = _cache
            .CreateSchema<BarInterface, SortInputType<BarInterface>>(
                s_barEntities,
                configure: Configure);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { test: { prop: ASC}}) "
                    + "{ test{ prop }}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { test: { prop: DESC}}) "
                    + "{ test{ prop }}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "ASC")
            .MatchAsync();
    }

    private static void Configure(ISchemaBuilder builder)
    {
        builder.AddObjectType<InterfaceImpl1>();
        builder.AddObjectType<InterfaceImpl2>();
    }

    public interface ITest
    {
        string Prop { get; set; }
    }

    public class InterfaceImpl1 : ITest
    {
        public string Prop { get; set; } = null!;

        public string Specific1 { get; set; } = null!;
    }

    public class InterfaceImpl2 : ITest
    {
        public string Prop { get; set; } = null!;

        public string Specific2 { get; set; } = null!;
    }

    public class BarInterface
    {
        public ITest Test { get; set; } = null!;
    }
}
