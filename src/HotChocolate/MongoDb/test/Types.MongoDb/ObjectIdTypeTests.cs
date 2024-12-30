using HotChocolate.Execution;
using HotChocolate.Types.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace HotChocolate.Types;

public class ObjectIdTypeTests
{
    [Fact]
    public async Task Should_MapObjectIdToScalar()
    {
        // arrange
        var executor = await CreateSchema();

        // act
        var schema = executor.Schema.Print();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Should_ReturnObjectIdOnQuery()
    {
        // arrange
        var executor = await CreateSchema();
        const string query =
            """
            {
              foo {
                id
              }
            }
            """;

        // act
        var result = await executor.ExecuteAsync(query, CancellationToken.None);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Should_ReturnInputOnQuery()
    {
        // arrange
        var executor = await CreateSchema();
        var query = @"
            {
                loopback(objectId: ""6124e80f3f5fc839830c1f6b"")
            }";

        // act
        var result = await executor.ExecuteAsync(query, CancellationToken.None);

        // assert
        result.MatchSnapshot();
    }

    private static ValueTask<IRequestExecutor> CreateSchema()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<ObjectIdType>()
            .BuildRequestExecutorAsync();

    public class Query
    {
        public Foo GetFoo() => new() { Id = new ObjectId("6124e80f3f5fc839830c1f6b"), };

        public ObjectId Loopback(ObjectId objectId) => objectId;
    }

    public class Foo
    {
        public ObjectId Id { get; set; }
    }
}
