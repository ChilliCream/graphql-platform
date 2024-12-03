using HotChocolate.Execution;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorStructTests : IClassFixture<SchemaCache>
{
    private static readonly Bar[] _barEntities =
    [
        new() { Foo = new Foo { BarShort = 12, }, },
        new() { Foo = new Foo { BarShort = 14, }, },
        new() { Foo = new Foo { BarShort = 13, }, },
    ];

    private static readonly BarNullable[] _barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable { BarShort = 12, },
            FooList = [new FooNullable { BarShort = 13, },],
            FooNullableList = [new FooNullable { BarShort = 13, },],
        },
        new()
        {
            Foo = new FooNullable { BarShort = null, },
            FooList = [new FooNullable { BarShort = null, },],
            FooNullableList = [new FooNullable { BarShort = null, },],
        },
        new()
        {
            Foo = new FooNullable { BarShort = 14, },
            FooList = [new FooNullable { BarShort = 14, },],
            FooNullableList = [new FooNullable { BarShort = 14, },],
        },
        new()
        {
            Foo = new FooNullable { BarShort = 13, },
            FooList = [new FooNullable { BarShort = 13, },],
            FooNullableList =
                [new FooNullable { BarShort = 13, }, null,],
        },
        new() { Foo = null, FooList = null, FooNullableList = null, },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorStructTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ObjectShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: 12}}}) { foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: 13}}}) { foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: null}}}) { foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: 12}}}) { foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: 13}}}) { foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { eq: null}}}) { foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNull()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: { barShort: { neq: 123}}}) { foo{ barShort}}}")
                .Build());
        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { foo: null}) { foo{ barShort}}}")
                .Build());
        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { foo { barShort }}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "selected")
            .Add(res2, "null")
            .Add(res3, "all")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableListShortEqual_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNullableList:{ some: { barShort: { eq: 12}}}}) { foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNullableList:{ some: { barShort: { eq: 13}}}}) { foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNullableList:{ some: { barShort: { eq: null}}}}) { foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectListShortEqual_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooList:{ some: { barShort: { eq: 12}}}}) { foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooList:{ some: { barShort: { eq: 13}}}}) { foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooList:{ some: { barShort: { eq: null}}}}) { foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    public struct Foo
    {
        public short BarShort { get; set; }
    }

    public struct FooNullable
    {
        public short? BarShort { get; set; }
    }

    public struct Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; }
    }

    public struct BarNullable
    {
        public int Id { get; set; }

        public FooNullable? Foo { get; set; }

        public FooNullable?[]? FooNullableList { get; set; }

        public FooNullable[]? FooList { get; set; }
    }

    public class BarFilterInput : FilterInputType<Bar>
    {
    }

    public class BarNullableFilterInput : FilterInputType<BarNullable>
    {
    }
}
