using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.WireFormat.Right;

/// <summary>
/// The <c>right</c> Apollo Federation subgraph for the entity-batch wire-format
/// tests. Resolves the <c>Child</c> entity by key and owns <c>Child.value</c>, so
/// selecting <c>value</c> against a <c>Child</c> reference produced by the
/// <c>left</c> subgraph forces an <c>_entities</c> lookup against this subgraph.
/// </summary>
public static class RightSubgraph
{
    public const string Name = "right";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<ChildType>();

        var app = builder.Build();
        app.MapSubgraph(enableBatching: true);

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
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
            .Field("version")
            .Type<StringType>()
            .Resolve(_ => "1");
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

        descriptor
            .Field("value")
            .Argument("suffix", a => a.Type<StringType>())
            .Type<StringType>()
            .Resolve(ctx =>
            {
                var child = ctx.Parent<Child>();
                var suffix = ctx.ArgumentValue<string?>("suffix");
                var value = $"child-{child.Id}";

                return string.IsNullOrEmpty(suffix) ? value : value + suffix;
            });
    }

    private static Child ResolveById(string id) => new() { Id = id };
}
