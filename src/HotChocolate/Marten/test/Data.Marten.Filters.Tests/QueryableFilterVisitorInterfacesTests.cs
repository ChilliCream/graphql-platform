using System.ComponentModel.DataAnnotations;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorInterfacesTests : IClassFixture<SchemaCache>
{
    private static readonly BarInterface[] _barEntities =
    {
        new() { Test = new InterfaceImpl1 { Prop = "a" } },
        new() { Test = new InterfaceImpl1 { Prop = "b" } }
    };

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
            .CreateSchema<BarInterface, FilterInputType<BarInterface>>(_barEntities,
                configure: Configure);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { test: { prop: { eq: \"a\"}}}) " +
                    "{ test{ prop }}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { test: { prop: { eq: \"b\"}}}) " +
                    "{ test{ prop }}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { test: { prop: { eq: null}}}) " +
                    "{ test{ prop}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1,
                        "a"),
                    res2,
                    "ba"),
                res3,
                "null")
            .MatchAsync();
    }

    private static void Configure(ISchemaBuilder builder)
        => builder
            .AddObjectType<InterfaceImpl1>(x => x.Implements<InterfaceType<Test>>())
            .AddObjectType<InterfaceImpl2>(x => x.Implements<InterfaceType<Test>>())
            .AddInterfaceType<Test>();

    public abstract class Test
    {
        [Key]
        public int Id { get; set; }

        public string Prop { get; set; } = default!;
    }

    public class InterfaceImpl1 : Test
    {
        public string Specific1 { get; set; } = default!;
    }

    public class InterfaceImpl2 : Test
    {
        public string Specific2 { get; set; } = default!;
    }

    public class BarInterface
    {
        [Key]
        public int Id { get; set; }

        public Test Test { get; set; } = default!;
    }
}
