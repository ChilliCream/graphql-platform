using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionVisitorIsProjectedTests(MongoResource resource)
    : IClassFixture<MongoResource>
{
    private static readonly Foo[] s_fooEntities =
    [
        new Foo
        {
            IsProjectedTrue = true,
            IsProjectedFalse = false
        },
        new Foo
        {
            IsProjectedTrue = true,
            IsProjectedFalse = false
        }
    ];

    private static readonly Bar[] s_barEntities =
    [
        new Bar { IsProjectedFalse = false },
        new Bar { IsProjectedFalse = false }
    ];

    private readonly SchemaCache _cache = new(resource);

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithOneProps()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithTwoProps()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse isProjectedTrue  }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsProjected_Should_AlwaysBeProjectedWhenSelected_When_True()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IsProjected_Should_NotFailWhenSelectionSetSkippedCompletely()
    {
        // arrange
        var tester = _cache.CreateSchema(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            """
            {
              root {
                isProjectedFalse
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [IsProjected(true)]
        public bool? IsProjectedTrue { get; set; }

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }

    public class Bar
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }
}
