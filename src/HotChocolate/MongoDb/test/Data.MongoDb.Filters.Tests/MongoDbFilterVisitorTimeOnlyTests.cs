using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorTimeOnlyTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = new TimeOnly(06, 30), },
        new() { Bar = new TimeOnly(16, 00), },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = new TimeOnly(06, 30), },
        new() { Bar = null, },
        new() { Bar = new TimeOnly(16, 00), },
    ];

    public MongoDbFilterVisitorTimeOnlyTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_TimeOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                .Build());

        // arrange
        await Snapshot
            .Create()
            .AddResult(res1, "0630")
            .AddResult(res2, "1600")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_TimeOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                .Build());

        // arrange
        await Snapshot
            .Create()
            .AddResult(res1, "0630")
            .AddResult(res2, "1600")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableTimeOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: null } }){ bar } }")
                .Build());

        // arrange
        await Snapshot
            .Create()
            .AddResult(res1, "0630")
            .AddResult(res2, "1600")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableTimeOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: null } }){ bar } }")
                .Build());

        // arrange
        await Snapshot
            .Create()
            .AddResult(res1, "0630")
            .AddResult(res2, "1600")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public TimeOnly Bar { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public TimeOnly? Bar { get; set; }
    }

    public class FooFilterType
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterType
        : FilterInputType<FooNullable>
    {
    }
}
