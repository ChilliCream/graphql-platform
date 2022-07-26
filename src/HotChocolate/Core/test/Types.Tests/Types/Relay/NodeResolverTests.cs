using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

public class NodeResolverTests
{
    [Obsolete]
    [Fact]
    public async Task NodeResolver_ResolveNode()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .EnableRelaySupport()
            .AddType<EntityType>()
            .AddQueryType<Query>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5CmRmb28=\")  " +
            "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Obsolete]
    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .EnableRelaySupport()
            .AddObjectType<Entity>(d =>
            {
                d.AsNode()
                    .NodeResolver<string>((ctx, id) =>
                        Task.FromResult(new Entity { Name = id }))
                    .Resolve(ctx => ctx.Parent<Entity>().Id);
            })
            .AddQueryType<Query>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5CmRmb28=\")  " +
            "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Obsolete]
    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .EnableRelaySupport()
            .AddObjectType<Entity>(d =>
            {
                d.AsNode()
                    .NodeResolver((ctx, id) =>
                        Task.FromResult(new Entity { Name = (string)id }))
                    .Resolve(ctx => ctx.Parent<Entity>().Id);
            })
            .AddQueryType<Query>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5CmRmb28=\")  " +
            "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Obsolete]
    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .EnableRelaySupport()
            .AddObjectType(d =>
            {
                d.Name("Entity");
                d.AsNode()
                    .NodeResolver<string>((ctx, id) =>
                        Task.FromResult<object>(new Entity { Name = id }))
                    .Resolve(ctx => ctx.Parent<Entity>().Id);
                d.Field("name")
                    .Type<StringType>()
                    .Resolve(t => t.Parent<Entity>().Name);
            })
            .AddQueryType(d =>
            {
                d.Name("Query")
                    .Field("entity")
                    .Type(new NamedTypeNode("Entity"))
                    .Resolve(new Entity { Name = "foo" });
            })
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5CmRmb28=\")  " +
            "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Obsolete]
    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .EnableRelaySupport()
            .AddObjectType(d =>
            {
                d.Name("Entity");
                d.AsNode()
                    .NodeResolver((ctx, id) =>
                        Task.FromResult<object>(new Entity { Name = (string)id }))
                    .Resolve(ctx => ctx.Parent<Entity>().Id);
                d.Field("name")
                    .Type<StringType>()
                    .Resolve(t => t.Parent<Entity>().Name);
            })
            .AddQueryType(d =>
            {
                d.Name("Query")
                    .Field("entity")
                    .Type(new NamedTypeNode("Entity"))
                    .Resolve(new Entity { Name = "foo" });
            })
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5CmRmb28=\")  " +
            "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension2()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension3()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension3>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension4()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension4>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Obsolete]
    [Fact]
    public async Task NodeAttribute_On_Extension_Fetch_Through_Node_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension>()
            .EnableRelaySupport()
            .ExecuteRequestAsync(
                @"{
                        node(id: ""RW50aXR5CmRhYmM="") {
                            ... on Entity {
                                name
                            }
                        }
                    }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public Entity GetEntity(string name) => new Entity { Name = name };

        public Entity2 GetEntity2(string name) => new Entity2 { Name = name };
    }

    [Obsolete]
    public class EntityType
        : ObjectType<Entity>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Entity> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) =>
                    Task.FromResult(new Entity { Name = id }));
        }
    }

    public class Entity
    {
        public string Id => Name;
        public string Name { get; set; }
    }

    public class Entity2
    {
        public string Id => Name;
        public string Name { get; set; }

        public static Entity2 Get(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension
    {
        public static Entity GetEntity(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension2
    {
        [NodeResolver]
        public static Entity Foo(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension3
    {
        [NodeResolver]
        public Entity Foo(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension4
    {
        public Entity GetEntity(string id) => new() { Name = id };
    }
}
