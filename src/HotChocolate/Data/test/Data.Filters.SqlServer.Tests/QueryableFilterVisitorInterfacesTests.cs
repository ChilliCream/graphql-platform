using System.ComponentModel.DataAnnotations;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorInterfacesTests : IClassFixture<SchemaCache>
{
    private static readonly BarInterface[] _barEntities =
    [
        new() { Test = new InterfaceImpl1 { Prop = "a", }, },
        new() { Test = new InterfaceImpl1 { Prop = "b", }, },
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
            .CreateSchema<BarInterface, FilterInputType<BarInterface>>(
                _barEntities,
                configure: Configure,
                onModelCreating: OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { test: { prop: { eq: \"a\"}}}) " +
                    "{ test{ prop }}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { test: { prop: { eq: \"b\"}}}) " +
                    "{ test{ prop }}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { test: { prop: { eq: null}}}) " +
                    "{ test{ prop}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "ba")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    private static void Configure(ISchemaBuilder builder)
        => builder
            .AddObjectType<InterfaceImpl1>(x => x.Implements<InterfaceType<Test>>())
            .AddObjectType<InterfaceImpl2>(x => x.Implements<InterfaceType<Test>>())
            .AddInterfaceType<Test>();

    private static void OnModelCreating(ModelBuilder builder)
        => builder
            .Entity<Test>()
            .HasDiscriminator<string>("_t")
            .HasValue<InterfaceImpl1>(nameof(InterfaceImpl1))
            .HasValue<InterfaceImpl2>(nameof(InterfaceImpl2));

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
