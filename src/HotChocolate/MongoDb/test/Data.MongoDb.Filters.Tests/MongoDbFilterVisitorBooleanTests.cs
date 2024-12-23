using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorBooleanTests
    : SchemaCache
    , IClassFixture<MongoResource>
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

    public MongoDbFilterVisitorBooleanTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
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
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
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
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            _fooNullableEntities);

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
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
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
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool? Bar { get; set; }
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
