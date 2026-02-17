#pragma warning disable RCS1102 // Make class static
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class NodeResolverTests
{
    [Fact]
    public async Task NodeResolver_ResolveNode()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicField()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType<Entity>(d =>
                {
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<Entity?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType<Entity>(d =>
                {
                    d.ImplementsNode()
                        .ResolveNode<string>((_, id) =>
                            Task.FromResult<Entity?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicField()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<object?>(new Entity { Name = id }))
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<object?>(new Entity { Name = id }))
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_WithInterface()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddGlobalObjectIdentification()
            .AddQueryType<Query>()
            .AddType<Entity3>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "RW50aXR5Mzox") {
                    ... on Entity3 {
                        id
                    }
                }
            }
            """);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension()
    {
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
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension4>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension_Fetch_Through_Node_Field()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                """
                {
                    node(id: "RW50aXR5OmFiYw==") {
                        ... on Entity {
                            name
                        }
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    // Ensure Issue 7829 is fixed.
    [Fact]
    public async Task NodeAttribute_On_Extension_With_Renamed_Id()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEntityRenamed>()
            .AddTypeExtension<EntityExtensionRenamingId>()
            .ExecuteRequestAsync(
                """
                {
                  entity(id: 5) {
                    id
                    data
                  }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeResolver_And_AsSelector()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddTypeExtension<EntityExtension5>()
                .AddTypeExtension<Entity2Extension1>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw=="]) {
                    id
                }
            }
            """);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    public class Query
    {
        public Entity GetEntity(string name) => new Entity { Name = name };

        public Entity2 GetEntity2(string name) => new Entity2 { Name = name };
    }

    public class EntityType : ObjectType<Entity>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Entity> descriptor)
        {
            descriptor
                .ImplementsNode()
                .IdField(t => t.Id)
                .ResolveNode((_, id) => Task.FromResult<Entity?>(new Entity { Name = id }));
        }
    }

    public class Entity
    {
        public string Id
        {
            get => Name;
            set => Name = value;
        }

        public required string Name { get; set; }
    }

    public class Entity2
    {
        public string Id => Name;
        public required string Name { get; set; }

        public static Entity2 Get(string id) => new() { Name = id };
    }

    [Node]
    public class Entity3 : EntityBase, IResolvable<Entity3>
    {
        public string? Message { get; set; }
    }

    public class EntityBase
    {
        public int Id { get; set; }
    }

    public interface IResolvable<T> where T : EntityBase, new()
    {
        static Task<T> GetAsync(int id) => Task.FromResult(new T { Id = id });
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
        public static Entity Foo(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension4
    {
        public static Entity GetEntity(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension5
    {
        [NodeResolver]
        public static Entity GetEntity(string id, ISelection selection)
        {
            selection.AsSelector<Entity>();

            return new Entity { Name = id };
        }
    }

    [Node]
    [ExtendObjectType(typeof(Entity2))]
    public class Entity2Extension1
    {
        [NodeResolver]
        public static Entity2 GetEntity2(string id, ISelection selection)
        {
            selection.AsSelector<Entity2>();

            return new Entity2 { Name = id };
        }
    }

    public class QueryEntityRenamed
    {
        public EntityNoId GetEntity(int id)
            => new EntityNoId { Data = id };
    }

    public class EntityNoId
    {
        public int Data { get; set; }
    }

    [Node]
    [ExtendObjectType(typeof(EntityNoId))]
    public class EntityExtensionRenamingId
    {
        public int GetId([Parent] EntityNoId entity)
            => entity.Data;

        [NodeResolver]
        public EntityNoId GetEntity(int id)
            => new() { Data = id };
    }
}
#pragma warning restore RCS1102 // Make class static
