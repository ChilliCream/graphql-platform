using CookieCrumble;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionVisitorIsProjectedTests(MongoResource resource)
    : IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo
        {
            IsProjectedTrue = true,
            IsProjectedFalse = false,
        },
        new Foo
        {
            IsProjectedTrue = true,
            IsProjectedFalse = false,
        },
    ];

    private static readonly Bar[] _barEntities =
    [
        new Bar { IsProjectedFalse = false, },
        new Bar { IsProjectedFalse = false, },
    ];

    private readonly SchemaCache _cache = new(resource);

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithOneProps()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithTwoProps()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root { isProjectedFalse isProjectedTrue  }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_AlwaysBeProjectedWhenSelected_When_True()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_NotFailWhenSelectionSetSkippedCompletely()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            """
            {
              root {
                isProjectedFalse
              }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
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
        public Guid Id { get; set; } = Guid.NewGuid();

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }
}
