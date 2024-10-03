using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class RelaySchemaTests
{
    [Fact]
    public async Task AddGlobalObjectIdentification_Node_Field_On_Query_Exists()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_QueryField_On_MutationPayload_Exists()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_With_Extensions()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType(d => d.Name("Mutation"))
            .AddTypeExtension<MutationExtension>()
            .AddQueryFieldToMutationPayloads()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_With_Different_FieldName()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads(o => o.QueryFieldName = "rootQuery")
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_With_Different_PayloadPredicate()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation2>()
            .AddQueryFieldToMutationPayloads(o =>
            {
                o.MutationPayloadPredicate = type => type.Name.EndsWith("Result");
            })
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_Refetch_SomeId()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("mutation { foo { query { some { id } } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddQueryFieldToMutationPayloads_Refetch_SomeId_With_Query_Inst()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("mutation { foo { query { some { id } } } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Relay_ShouldReturnNonNullError_When_IdIsNull()
    {
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
