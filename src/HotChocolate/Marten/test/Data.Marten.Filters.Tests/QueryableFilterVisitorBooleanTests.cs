using HotChocolate.Data.Filters;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableFilterVisitorBooleanTests(SchemaCache cache)
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = true, },
        new() { Bar = null, },
        new() { Bar = false, },
    ];

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = await cache.CreateSchemaAsync<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester = await cache.CreateSchemaAsync<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: false}}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester = await cache.CreateSchemaAsync<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: null}}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester = await cache.CreateSchemaAsync<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: true}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: false}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: null}}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public bool? Bar { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>;

    public class FooNullableFilterInput : FilterInputType<FooNullable>;
}
