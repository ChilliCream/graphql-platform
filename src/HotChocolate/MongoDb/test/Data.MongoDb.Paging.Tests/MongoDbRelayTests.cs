using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
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
            .AddTransient<OffsetPagingProvider, MongoDbOffsetPagingProvider>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddTypeConverter<ObjectId, string>(x => x.ToString())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foo {
                   id
                }
            }");

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), result)
            .MatchAsync();
    }

    [Fact]
    public async Task Return_Node()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddTransient<OffsetPagingProvider, MongoDbOffsetPagingProvider>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<FooType>()
            .AddTypeConverter<ObjectId, string>(x => x.ToString())
            .AddTypeConverter<string, ObjectId>(x => ObjectId.Parse(x.ToString()))
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                node(id:""Rm9vCmQ2MGRmMTYyZWQwNzY2ZTE1Y2NlNmIxMGU="") {
                   id
                }
            }");

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), result)
            .MatchAsync();
    }

    public class Query
    {
        public Foo GetFoo() => new Foo { Id = ObjectId.Parse("507f191e810c19729de860ea"), };
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.ImplementsNode()
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
