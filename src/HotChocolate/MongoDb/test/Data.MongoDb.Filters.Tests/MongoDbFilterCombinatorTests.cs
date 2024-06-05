using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterCombinatorTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    public MongoDbFilterCombinatorTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_Empty_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { }){ bar }}")
                .Build());

        await Snapshot.Create()
            .Add(res1)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Bar { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }
}
