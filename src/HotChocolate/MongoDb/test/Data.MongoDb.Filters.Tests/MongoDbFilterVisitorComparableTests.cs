using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorComparableTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo
        {
            BarShort = 12,
            BarDateTime = new DateTime(2000, 1, 12, 0, 0, 0, DateTimeKind.Utc),
        },
        new Foo
        {
            BarShort = 14,
            BarDateTime = new DateTime(2000, 1, 14, 0, 0, 0, DateTimeKind.Utc),
        },
        new Foo
        {
            BarShort = 13,
            BarDateTime = new DateTime(2000, 1, 13, 0, 0, 0, DateTimeKind.Utc),
        },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new FooNullable
        {
            BarShort = 12,
            BarDateTime = new DateTime(2000, 1, 12, 0, 0, 0, DateTimeKind.Utc),
        },
        new FooNullable
        {
            BarShort = null,
            BarDateTime = null,
        },
        new FooNullable
        {
            BarShort = 14,
            BarDateTime = new DateTime(2000, 1, 14, 0, 0, 0, DateTimeKind.Utc),
        },
        new FooNullable
        {
            BarShort = 13,
            BarDateTime = new DateTime(2000, 1, 13, 0, 0, 0, DateTimeKind.Utc),
        },
    ];

    public MongoDbFilterVisitorComparableTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_ShortEqual_Expression_DateTime()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: \"2000-01-12T00:00:00Z\"}})" +
                    "{ barDateTime}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: \"2000-01-12T00:00:00Z\"}})" +
                    "{ barDateTime}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: null}}){ barDateTime}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortEqual_Expression_DateTime_Nullable()
    {
        // arrange
        var tester =
            CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: \"2000-01-12T00:00:00Z\"}})" +
                    "{ barDateTime}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: \"2000-01-12T00:00:00Z\"}})" +
                    "{ barDateTime}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barDateTime: { eq: null}}){ barDateTime}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            """
            {
              root(where: { barShort: { eq: 12 } }) {
                barShort
              }
            }
            """);

        var res2 = await tester.ExecuteAsync(
            """
            {
              root(where: { barShort: { eq: 13 } }) {
                barShort
              }
            }
            """);

        var res3 = await tester.ExecuteAsync(
            """
            {
              root(where: { barShort: { eq: null } }) {
                barShort
              }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1, "12").AddResult(res2, "13").AddResult(res3, "14").AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1, "12and13").AddResult(res2, "13and14").AddResult(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableEqual_Expression()
    {
        // arrange
        var tester =
            CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { eq: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { eq: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThan_Expression()
    {
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { gte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThanOrEquals_Expression()
    {
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { ngte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlt: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { lte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nlte: null}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { in: [ 13, null ]}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { barShort: { nin: [ 13, null ]}}){ barShort}}")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1, "12and13").AddResult(res2, "13and14").AddResult(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public void Create_Implicit_Operation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<Foo>>()))
            .AddMongoDbFiltering(compatibilityMode: true)
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Implicit_Operation_Normalized()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<Foo>>()))
            .AddMongoDbFiltering()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }

        public DateTime BarDateTime { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short? BarShort { get; set; }

        public DateTime? BarDateTime { get; set; }
    }

    public class FooFilterType : FilterInputType<Foo>;

    public class FooNullableFilterType : FilterInputType<FooNullable>;
}
