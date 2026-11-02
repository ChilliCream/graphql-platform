using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableFilterVisitorExecutableTests
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = true },
        new() { Bar = false }
    ];

    private static readonly FooNullable[] s_fooNullableEntities =
    [
        new() { Bar = true },
        new() { Bar = null },
        new() { Bar = false }
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorExecutableTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(s_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { eq: null}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(s_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ rootExecutable(where: { bar: { neq: null}}){ bar}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        public string? Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        public string? Id { get; set; }

        public bool? Bar { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>;

    public class FooNullableFilterInput : FilterInputType<FooNullable>;
}
