using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.AdditionalCoverage.UnionSiblingNestedLookup.Assets;

/// <summary>
/// The <c>assets</c> Apollo Federation subgraph for the union-sibling nested-lookup
/// tests. Resolves the <c>Asset</c> entity by key and owns its fields, returning
/// <c>MeterType</c> and <c>Category</c> only as non-resolvable stubs whose fields
/// live in the <c>search</c> subgraph.
/// </summary>
public static class AssetsSubgraph
{
    public const string Name = "assets";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<AssetType>()
            .AddType<MeterTypeType>()
            .AddType<CategoryType>();

        var app = builder.Build();
        app.MapSubgraph(enableBatching: true);

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}

public sealed class Asset
{
    public string Id { get; init; } = default!;
}

public sealed class MeterType
{
    public string Id { get; init; } = default!;
}

public sealed class Category
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

public sealed class AssetType : ObjectType<Asset>
{
    protected override void Configure(IObjectTypeDescriptor<Asset> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<StringType>>();

        descriptor
            .Field("statusMessage")
            .Type<StringType>()
            .Resolve(ctx => $"status-{ctx.Parent<Asset>().Id}");

        descriptor
            .Field("meterType")
            .Type<MeterTypeType>()
            .Resolve(ctx => new MeterType { Id = $"m{ctx.Parent<Asset>().Id}" });

        descriptor
            .Field("category")
            .Type<CategoryType>()
            .Resolve(ctx => new Category { Id = $"c{ctx.Parent<Asset>().Id}" });
    }

    private static Asset ResolveById(string id) => new() { Id = id };
}

public sealed class MeterTypeType : ObjectType<MeterType>
{
    protected override void Configure(IObjectTypeDescriptor<MeterType> descriptor)
    {
        descriptor.Key("id", resolvable: false);

        descriptor.Field(m => m.Id).Type<NonNullType<StringType>>();
    }
}

public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor.Key("id", resolvable: false);

        descriptor.Field(c => c.Id).Type<NonNullType<StringType>>();
    }
}
