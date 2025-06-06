using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableSortVisitorStringTests
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = "testatest" }, new() { Bar = "testbtest" }
    ];

    private static readonly FooNullable[] s_fooNullableEntities =
    [
        new() { Bar = "testatest" }, new() { Bar = "testbtest" }, new() { Bar = null }
    ];

    private readonly SchemaCache _cache;

    public QueryableSortVisitorStringTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_String_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooSortType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "ASC")
            .AddResult(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_String_OrderBy_Nullable()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
            s_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "ASC")
            .AddResult(res2, "DESC")
            .MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        public string Bar { get; set; } = null!;
    }

    public class FooNullable
    {
        public string? Id { get; set; }

        public string? Bar { get; set; }
    }

    public class FooSortType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }

    public class FooNullableSortType : SortInputType<FooNullable>
    {
        protected override void Configure(ISortInputTypeDescriptor<FooNullable> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
