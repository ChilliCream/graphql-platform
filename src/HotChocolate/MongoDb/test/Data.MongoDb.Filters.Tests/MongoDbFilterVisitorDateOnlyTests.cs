using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorDateOnlyTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = new DateOnly(2022, 01, 16) },
        new() { Bar = new DateOnly(2022, 01, 15) }
    ];

    private static readonly FooNullable[] s_fooNullableEntities =
    [
        new() { Bar = new DateOnly(2022, 01, 16) },
        new() { Bar = null },
        new() { Bar = new DateOnly(2022, 01, 15) }
    ];

    public MongoDbFilterVisitorDateOnlyTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_DateOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "2022-01-16")
            .AddResult(res2, "2022-01-15")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_DateOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "2022-01-16")
            .AddResult(res2, "2022-01-15")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_NullableDateOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            s_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { eq: null } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "2022-01-16")
            .AddResult(res2, "2022-01-15")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_NullableDateOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            s_fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { neq: null } }){ bar } }")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "2022-01-16")
            .AddResult(res2, "2022-01-15")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Bar { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly? Bar { get; set; }
    }

    public class FooFilterType : FilterInputType<Foo>;

    public class FooNullableFilterType : FilterInputType<FooNullable>;
}
