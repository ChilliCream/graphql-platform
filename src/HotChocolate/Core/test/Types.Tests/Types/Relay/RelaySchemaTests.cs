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
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType<Mutation>()
                .EnableRelaySupport(new RelayOptions { AddQueryFieldToMutationPayloads = true })
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task EnableRelay_AddQueryToMutationPayloads_With_Extensions()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType(d => d.Name("Mutation"))
                .AddTypeExtension<MutationExtension>()
                .EnableRelaySupport(new RelayOptions { AddQueryFieldToMutationPayloads = true })
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task EnableRelay_AddQueryToMutationPayloads_Refetch_SomeId()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddMutationType<Mutation>()
                .EnableRelaySupport(new RelayOptions { AddQueryFieldToMutationPayloads = true })
                .ExecuteRequestAsync("mutation { foo { query { some { id } } } }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task EnableRelay_AddQueryToMutationPayloads_Refetch_SomeId_With_Query_Inst()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .EnableRelaySupport(new RelayOptions { AddQueryFieldToMutationPayloads = true })
                .ExecuteRequestAsync("mutation { foo { query { some { id } } } }")
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

        public class Query
        {
            public Some GetSome() => new();
        }

        [Node]
        public class Some
        {
            public string Id => "some";

            public static Some GetSome(string id) => new();
        }

        public class Mutation
        {
            public FooPayload Foo() => new();
        }

        [ExtendObjectType("Mutation")]
        public class MutationExtension
        {
            public FooPayload Foo() => new();
        }

        public class FooPayload { }
    }
}
