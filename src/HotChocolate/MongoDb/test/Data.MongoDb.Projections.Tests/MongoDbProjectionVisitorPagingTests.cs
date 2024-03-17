using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionVisitorPagingTests : IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = false, Baz = "b", },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = null, Baz = null, },
        new() { Bar = false, Baz = "c", },
    ];

    private readonly SchemaCache _cache;

    public MongoDbProjectionVisitorPagingTests(MongoResource resource)
    {
        _cache = new SchemaCache(resource);
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes { bar baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes { baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ edges { node { bar baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ edges { node { baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_EdgesAndNodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes{ baz } edges { node { bar }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_EdgesAndNodesOverlap()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes{ baz } edges { node { baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes { bar baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_Nodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes { baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ edges { node { bar baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_Edges()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ edges { node { baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsTwoProperties_EdgesAndNodes()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes{ baz } edges { node { bar }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateNullable_ProjectsOneProperty_EdgesAndNodesOverlap()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooNullableEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes{ baz } edges { node { baz }} }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Projection_Should_Stop_When_UseProjectionEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ nodes{ bar list { barBaz } } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsTwoProperties_Items_WithArgs()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(take:10, skip:1) { items { bar baz } } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsTwoProperties_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items { bar baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_ProjectsOneProperty_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items { baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }


    [Fact]
    public async Task CreateOffsetPagingNullable_ProjectsTwoProperties_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items { bar baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPagingNullable_ProjectsOneProperty_Items()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooNullableEntities,
            useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items { baz } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_Projection_Should_Stop_When_UseProjectionEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items{ bar list { barBaz } } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task CreateOffsetPaging_Projection_Should_Stop_When_UsePagingEncountered()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root{ items{ bar paging { nodes {barBaz }} } }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Bar { get; set; }

        public string Baz { get; set; } = default!;

        public string? Qux { get; set; }

        public List<Bar>? List { get; set; }

        [UsePaging]
        public List<Bar>? Paging { get; set; }
    }

    public class Bar
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? BarBaz { get; set; }

        public string? BarQux { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool? Bar { get; set; }

        public string? Baz { get; set; }
    }
}
