using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorBooleanTests : IClassFixture<SchemaCache>
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

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorBooleanTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        var snapshot = new Snapshot();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());
        snapshot.Add(res1, "true");

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());
        snapshot.Add(res2, "false");

        // assert
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        var snapshot = new Snapshot();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: true}}){ bar}}")
                .Build());
        snapshot.Add(res1, "true");

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: false}}){ bar}}")
                .Build());
        snapshot.Add(res2, "false");

        // assert
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);
        var snapshot = new Snapshot();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: true}}){ bar}}")
                .Build());
        snapshot.Add(res1, "true");

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: false}}){ bar}}")
                .Build());
        snapshot.Add(res2, "false");

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: null}}){ bar}}")
                .Build());
        snapshot.Add(res3, "null");

        // assert
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);
        var snapshot = new Snapshot();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: true}}){ bar}}")
                .Build());
        snapshot.Add(res1, "true");

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: false}}){ bar}}")
                .Build());
        snapshot.Add(res2, "false");

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: null}}){ bar}}")
                .Build());
        snapshot.Add(res3, "null");

        // assert
        await snapshot.MatchAsync();
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

    public class FooFilterInput : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
    }
}
