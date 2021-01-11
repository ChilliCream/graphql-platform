using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class RelaySchemaTests
    {
        [Fact]
        public void EnableRelay_Node_Field_On_Query_Exists()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .EnableRelaySupport()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task EnableRelay_AddQueryToMutationPayloads()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType<Mutation>()
                .EnableRelaySupport(new RelayOptions { AddQueryFieldsToMutations = true })
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor
                    .Field("some")
                    .Type<SomeType>()
                    .Resolver(new object());
            }
        }

        public class SomeType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor
                    .Name("Some")
                    .ImplementsNode()
                    .ResolveNode<object>((_, _) => Task.FromResult(new object()));

                descriptor
                    .Field("id")
                    .Type<NonNullType<IdType>>()
                    .Resolver("bar");
            }
        }

        public class Mutation
        {
            public FooPayload Foo() => new();
        }

        public class FooPayload { }
    }
}
