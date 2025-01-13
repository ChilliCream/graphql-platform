using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HotChocolate.Data.MongoDb.Paging;

public class MongoDbRelayTests
{
    [Fact]
    public async Task Return_BsonId()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddObjectIdConverters()
            .AddGlobalObjectIdentification()
            .AddMongoDbPagingProviders()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                foo {
                   id
                }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Return_BsonId_Not_Compressed()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddObjectIdConverters(compressGlobalIds: false)
            .AddGlobalObjectIdentification()
            .AddMongoDbPagingProviders()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                foo {
                   id
                }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Return_Node()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddObjectIdConverters()
            .AddGlobalObjectIdentification()
            .AddMongoDbPagingProviders()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id:"Rm9vOlB/GR6BDBlynehg6g==") {
                   id
                }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Return_Node_Not_Compressed()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddObjectIdConverters(compressGlobalIds: false)
            .AddGlobalObjectIdentification()
            .AddMongoDbPagingProviders()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id:"Rm9vOjUwN2YxOTFlODEwYzE5NzI5ZGU4NjBlYQ==") {
                   id
                }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    public class Query
    {
        public Foo GetFoo() => new() { Id = ObjectId.Parse("507f191e810c19729de860ea"), };
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .ImplementsNode()
                .IdField(x => x.Id)
                .ResolveNode((_, objectId) => Task.FromResult<Foo?>(new Foo { Id = objectId, }));
        }
    }

    public class Foo
    {
        [BsonId]
        public ObjectId Id { get; set; }
    }
}
