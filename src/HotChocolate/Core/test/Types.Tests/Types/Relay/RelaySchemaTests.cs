using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay;

public class RelaySchemaTests
{
    [Obsolete]
    [Fact]
    public void EnableRelay_Node_Field_On_Query_Exists()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .EnableRelaySupport()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Obsolete]
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

    [Obsolete]
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

    [Obsolete]
    [Fact]
    public async Task EnableRelay_AddQueryToMutationPayloads_With_Different_FieldName()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation>()
            .EnableRelaySupport(new RelayOptions
            {
                AddQueryFieldToMutationPayloads = true,
                QueryFieldName = "rootQuery"
            })
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Obsolete]
    [Fact]
    public async Task EnableRelay_AddQueryToMutationPayloads_With_Different_PayloadPredicate()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation2>()
            .EnableRelaySupport(new RelayOptions
            {
                AddQueryFieldToMutationPayloads = true,
                MutationPayloadPredicate = type => type.Name.EndsWith("Result")
            })
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Obsolete]
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

    [Obsolete]
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

    [Fact]
    public async Task Relay_ShouldReturnNonNullError_When_IdIsNull()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("user")
                .Type<UserType>()
                .Resolve(_ => new User { Name = "TEST", }))
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("query { user { id name } } ")
            .MatchSnapshotAsync();
    }

    public class User
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class UserType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor
                .ImplementsNode()
                .IdField(f => f.Id)
                .ResolveNode(ResolveNode);
        }

        private Task<User> ResolveNode(IResolverContext context, string id)
        {
            return Task.FromResult(new User { Name = "TEST", });
        }
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("some")
                .Type<SomeType>()
                .Resolve(new object());
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
                .Resolve("bar");
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

    public class Mutation2
    {
        public BazPayload Baz() => new();

        public BarResult Bar() => new();
    }

    [ExtendObjectType("Mutation")]
    public class MutationExtension
    {
        public FooPayload Foo() => new();
    }

    public class FooPayload
    {
    }

    public class BazPayload
    {
        public string Some { get; set; }
    }

    public class BarResult
    {
    }
}
