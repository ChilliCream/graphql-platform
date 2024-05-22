using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorComparableTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = null, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorComparableTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThanOrEquals_Expression()
    {
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortLowerThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThan_Expression()
    {
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }


    [Fact]
    public async Task Create_ShortLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12and13");
        snapshot.Add(res2, "13and14");
        snapshot.Add(res3, "nullAnd14");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12and13");
        snapshot.Add(res2, "13and14");
        snapshot.Add(res3, "nullAnd14");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { eq: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { neq: null}}){ barShort}}")
            .Build());

        // assert
        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { gte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { ngte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThan_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlt: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }


    [Fact]
    public async Task Create_ShortNullableLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { lte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
            .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nlte: null}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12");
        snapshot.Add(res2, "13");
        snapshot.Add(res3, "14");
        snapshot.Add(res4, "null");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { in: [ 13, null ]}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12and13");
        snapshot.Add(res2, "13and14");
        snapshot.Add(res3, "13andNull");
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
            .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort}}")
            .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument("{ root(where: { barShort: { nin: [ 13, null ]}}){ barShort}}")
            .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(res1, "12and13");
        snapshot.Add(res2, "13and14");
        snapshot.Add(res3, "13andNull");
        await snapshot.MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }
        public short? BarShort { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
    }
}
