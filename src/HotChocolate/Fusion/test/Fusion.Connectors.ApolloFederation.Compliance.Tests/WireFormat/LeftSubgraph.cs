using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.WireFormat.Left;

/// <summary>
/// The <c>left</c> Apollo Federation subgraph for the entity-batch wire-format
/// tests. Exposes <c>Query.parent</c> and owns the <c>Child</c> key. Each
/// <c>Parent.child</c> selection yields a <c>Child</c> reference that the gateway
/// resolves against the <c>right</c> subgraph, so a query that selects
/// <c>child</c> twice produces two same-subgraph entity lookups.
/// </summary>
public static class LeftSubgraph
{
    public const string Name = "left";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<ParentType>()
            .AddType<ChildType>();

        var app = builder.Build();
        app.MapSubgraph(enableBatching: true);

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}

public sealed class Parent
{
    public string Id { get; init; } = default!;

    public string ChildId { get; init; } = default!;
}

public sealed class Child
{
    public string Id { get; init; } = default!;
}

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("parent")
            .Type<ParentType>()
            .Resolve(_ => new Parent { Id = "1", ChildId = "1" });
    }
}

public sealed class ParentType : ObjectType<Parent>
{
    protected override void Configure(IObjectTypeDescriptor<Parent> descriptor)
    {
        descriptor.Field(p => p.Id).Type<NonNullType<StringType>>();

        descriptor
            .Field("child")
            .Type<ChildType>()
            .Resolve(ctx => new Child { Id = ctx.Parent<Parent>().ChildId });

        descriptor.Ignore(p => p.ChildId);
    }
}

public sealed class ChildType : ObjectType<Child>
{
    protected override void Configure(IObjectTypeDescriptor<Child> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<StringType>>();
    }

    private static Child ResolveById(string id) => new() { Id = id };
}
