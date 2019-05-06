using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class NodeResolverTests
    {
        // TODO : tests
        // resolve node
        // ids are correctly deserialized

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
                "{ node(id: \"RW50aXR5LXhmb28=\")  " +
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
                descriptor.AsNode<Entity, string>(
                    (ctx, id) => Task.FromResult(new Entity { Name = id }));
                descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            }
        }

        // ? descriptor.AsNode().IdField(t => t.Id).NodeResolver()


        public class Entity
        {
            public string Id => Name;
            public string Name { get; set; }
        }

    }
}
