using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorObjectIdTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f69"), },
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6a"), },
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6b"), },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f69"), },
        new() { },
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6a"), },
        new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6b"), },
    ];

    public MongoDbFilterVisitorObjectIdTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_ObjectIdEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotEqual_Expression()
    {
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }


    [Fact]
    public async Task Create_ObjectIdGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }


    [Fact]
    public async Task Create_ObjectIdLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "6124e80f3f5fc839830c1f69"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdIn_Expression()
    {
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(@"{
                    root(where: {
                        objectId: { in: [
                                ""6124e80f3f5fc839830c1f69"",
                                ""6124e80f3f5fc839830c1f6a""
                            ]}})
                        {
                            objectId
                        }
                    }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(@"{
                    root(where: {
                        objectId: {
                            in: [ null, ""6124e80f3f5fc839830c1f6b"" ]
                        }}) {
                        objectId
                    }
                }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(@"{
                    root(where: {
                        objectId: {
                            in: [ null, ""6124e80f3f5fc839830c1f6b"" ]
                        }}) {
                        objectId
                    }
                }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a"), res2, "band6124e80f3f5fc839830c1f6b"), res3, "nullAnd6124e80f3f5fc839830c1f6b")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                      root(
                        where: {
                          objectId: {
                            nin: [""6124e80f3f5fc839830c1f69"", ""6124e80f3f5fc839830c1f6a""]
                          }
                        }
                      ) {
                        objectId
                      }
                    }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                      root(
                        where: {
                          objectId: {
                            nin: [null, ""6124e80f3f5fc839830c1f6b""]
                          }
                        }
                      ) {
                        objectId
                      }
                    }")
                .SetDocument("{ root(where: { objectId: { nin: [ null, \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                      root(
                        where: {
                          objectId: {
                            nin: [null, ""6124e80f3f5fc839830c1f6b""]
                          }
                        }
                      ) {
                        objectId
                      }
                    }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a"), res2, "band6124e80f3f5fc839830c1f6b"), res3, "nullAnd6124e80f3f5fc839830c1f6b")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableEqual_Expression()
    {
        // arrange
        var tester =
            CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { eq: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { neq: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { gt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotGreaterThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }


    [Fact]
    public async Task Create_ObjectIdNullableGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { gte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { ngte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { lt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotLowerThan_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlt: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }


    [Fact]
    public async Task Create_ObjectIdNullableLowerThanOrEquals_Expression()
    {
        var tester =
            CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { lte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { nlte: null}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "a"), res2, "6124e80f3f5fc839830c1f6a"), res3, "6124e80f3f5fc839830c1f6b"), res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f69\", " +
                    "\"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f6a\", " +
                    "\"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f6a\", " +
                    "null ]}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a"), res2, "band6124e80f3f5fc839830c1f6b"), res3, "bandNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectIdNullableNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f69\", " +
                    "\"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f6a\", \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f6a\", null ]}}){ objectId}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a"), res2, "band6124e80f3f5fc839830c1f6b"), res3, "bandNull")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public ObjectId ObjectId { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public ObjectId? ObjectId { get; set; }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterType : FilterInputType<FooNullable>
    {
    }
}
