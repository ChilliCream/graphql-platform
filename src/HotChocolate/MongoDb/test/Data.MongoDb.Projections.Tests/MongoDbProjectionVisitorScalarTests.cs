using HotChocolate.Execution;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionVisitorScalarTests : IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = false, Baz = "b", },
    ];

    private readonly SchemaCache _cache;

    public MongoDbProjectionVisitorScalarTests(MongoResource resource)
    {
        _cache = new SchemaCache(resource);
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ bar baz }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ baz }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_WithResolver()
    {
        // arrange
        var tester = _cache.CreateSchema(
            _fooEntities,
            objectType: new ObjectType<Foo>(
                x => x
                    .Field("foo")
                    .Resolve(
                        new[]
                        {
                                "foo",
                        })
                    .Type<ListType<StringType>>()));

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ baz foo }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Bar { get; set; }

        public string Baz { get; set; } = default!;
    }
}
