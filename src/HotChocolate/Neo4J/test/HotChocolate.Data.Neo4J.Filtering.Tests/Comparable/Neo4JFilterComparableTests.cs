using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection("Database")]
public class Neo4JFilterComparableTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JFilterComparableTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private const string _fooEntitiesCypher =
        "CREATE (:FooComp {BarShort: 12}), (:FooComp {BarShort: 14}), (:FooComp {BarShort: 13})";

    private const string _fooNullableEntitiesCypher =
        @"CREATE
            (:FooCompNullable {BarShort: 12}),
            (:FooCompNullable {BarShort: NULL}),
            (:FooCompNullable {BarShort: 14}),
            (:FooCompNullable {BarShort: 13})";

    public class FooComp
    {
        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }

        public DateTime BarDateTime { get; set; }
    }

    public class FooCompNullable
    {
        public short? BarShort { get; set; }

        public DateTime? BarDateTime { get; set; }
    }

    public class FooCompFilterType : FilterInputType<FooComp>
    {
    }

    public class FooCompNullableFilterType : FilterInputType<FooCompNullable>
    {
    }

    [Fact]
    public async Task Create_ShortEqual_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { eq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { eq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { eq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotEqual_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { neq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { neq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { neq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThan_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gt: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gt: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gt: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gt: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThan_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngt: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngt: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngt: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngt: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortLowerThan_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThan_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(
            _fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortIn_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(_fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "13and14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooComp, FooCompFilterType>(_fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { eq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { eq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { eq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { neq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { neq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { neq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        SnapshotExtensions.Add(
                            Snapshot
                                .Create(), res1, "12"), res2, "13"), res3, "14"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { in: [ 13, null ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooCompNullable, FooCompNullableFilterType>(
                _fooNullableEntitiesCypher);

        // arrange
        const string query1 = "{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nin: [ 13, null ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "13andNull")
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
                    .Argument("test", a => a.Type<FilterInputType<FooComp>>()))
            .AddNeo4JFiltering(compatabilityMode: true)
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
                    .Argument("test", a => a.Type<FilterInputType<FooComp>>()))
            .AddNeo4JFiltering()
            .Create();

        // assert
        schema.MatchSnapshot();
    }
}
