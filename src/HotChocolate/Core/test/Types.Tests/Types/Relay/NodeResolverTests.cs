using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class NodeResolverTests
    {
        [Fact]
        public async Task NodeResolver_ResolveNode()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"RW50aXR5CmRmb28=\")  " +
                "{ ... on Entity { id name } } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task NodeResolver_ResolveNode_DynamicField()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddObjectType<Entity>(d =>
                {
                    d.AsNode()
                        .NodeResolver<string>((ctx, id) =>
                            Task.FromResult(new Entity { Name = id }))
                        .Resolver(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"RW50aXR5CmRmb28=\")  " +
                "{ ... on Entity { id name } } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task NodeResolver_ResolveNode_DynamicFieldObject()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddObjectType<Entity>(d =>
                {
                    d.AsNode()
                        .NodeResolver((ctx, id) =>
                            Task.FromResult(new Entity { Name = (string)id }))
                        .Resolver(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"RW50aXR5CmRmb28=\")  " +
                "{ ... on Entity { id name } } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task NodeResolverObject_ResolveNode_DynamicField()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.AsNode()
                        .NodeResolver<string>((ctx, id) =>
                        {
                            return Task.FromResult<object>(
                                new Entity { Name = id });
                        })
                        .Resolver(ctx => ctx.Parent<Entity>().Id);
                    d.Field("name")
                        .Type<StringType>()
                        .Resolver(t => t.Parent<Entity>().Name);
                })
                .AddQueryType(d =>
                {
                    d.Name("Query")
                        .Field("entity")
                        .Type(new NamedTypeNode("Entity"))
                        .Resolver(new Entity { Name = "foo" });
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"RW50aXR5CmRmb28=\")  " +
                "{ ... on Entity { id name } } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task NodeResolverObject_ResolveNode_DynamicFieldObject()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.AsNode()
                        .NodeResolver((ctx, id) =>
                        {
                            return Task.FromResult<object>(
                                new Entity { Name = (string)id });
                        })
                        .Resolver(ctx => ctx.Parent<Entity>().Id);
                    d.Field("name")
                        .Type<StringType>()
                        .Resolver(t => t.Parent<Entity>().Name);
                })
                .AddQueryType(d =>
                {
                    d.Name("Query")
                        .Field("entity")
                        .Type(new NamedTypeNode("Entity"))
                        .Resolver(new Entity { Name = "foo" });
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"RW50aXR5CmRmb28=\")  " +
                "{ ... on Entity { id name } } }");

            // assert
            result.MatchSnapshot();
        }

        public class Query
        {
            public Entity GetEntity(string name) => new Entity { Name = name };
        }

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
    }
}
