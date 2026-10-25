using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.AdditionalCoverage.UnionSiblingNestedLookup.Search;

/// <summary>
/// The <c>search</c> Apollo Federation subgraph for the union-sibling nested-lookup
/// tests. Owns the <c>CandidateResult</c> union root field, exposes <c>Asset</c>
/// only as a non-resolvable stub, and resolves the <c>MeterType</c> and
/// <c>Category</c> entities, so their fields require an <c>_entities</c> lookup
/// back into this subgraph.
/// </summary>
public static class SearchSubgraph
{
    public const string Name = "search";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<CandidateResultType>()
            .AddType<ExistingResultType>()
            .AddType<ConfigMatchesResultType>()
            .AddType<NotSubmissableResultType>()
            .AddType<CustomResultType>()
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

public sealed class ExistingResult
{
    public Asset Asset { get; init; } = default!;
}

public sealed class ConfigMatchesResult
{
    public Asset Asset { get; init; } = default!;
}

public sealed class NotSubmissableResult
{
    public Asset Asset { get; init; } = default!;
}

public sealed class CustomResult
{
    public Asset Asset { get; init; } = default!;
}

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("search")
            .Type<NonNullType<ListType<NonNullType<CandidateResultType>>>>()
            .Resolve(_ => new object[]
            {
                new CustomResult { Asset = new Asset { Id = "1" } },
                new CustomResult { Asset = new Asset { Id = "2" } },
                new CustomResult { Asset = new Asset { Id = "3" } }
            });
    }
}

public sealed class CandidateResultType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("CandidateResult");
        descriptor.Type<NotSubmissableResultType>();
        descriptor.Type<ExistingResultType>();
        descriptor.Type<ConfigMatchesResultType>();
        descriptor.Type<CustomResultType>();
    }
}

public sealed class ExistingResultType : ObjectType<ExistingResult>
{
    protected override void Configure(IObjectTypeDescriptor<ExistingResult> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class ConfigMatchesResultType : ObjectType<ConfigMatchesResult>
{
    protected override void Configure(IObjectTypeDescriptor<ConfigMatchesResult> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class NotSubmissableResultType : ObjectType<NotSubmissableResult>
{
    protected override void Configure(IObjectTypeDescriptor<NotSubmissableResult> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class CustomResultType : ObjectType<CustomResult>
{
    protected override void Configure(IObjectTypeDescriptor<CustomResult> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class AssetType : ObjectType<Asset>
{
    protected override void Configure(IObjectTypeDescriptor<Asset> descriptor)
    {
        descriptor.Key("id", resolvable: false);

        descriptor.Field(a => a.Id).Type<NonNullType<StringType>>();
    }
}

public sealed class MeterTypeType : ObjectType<MeterType>
{
    protected override void Configure(IObjectTypeDescriptor<MeterType> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<StringType>>();

        descriptor
            .Field("type")
            .Type<NonNullType<StringType>>()
            .Resolve(ctx => $"type-{ctx.Parent<MeterType>().Id}");
    }

    private static MeterType ResolveById(string id) => new() { Id = id };
}

public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<StringType>>();

        descriptor
            .Field("allowedMeterTypes")
            .Type<NonNullType<ListType<NonNullType<MeterTypeType>>>>()
            .Resolve(ctx => new[] { new MeterType { Id = $"a{ctx.Parent<Category>().Id}" } });
    }

    private static Category ResolveById(string id) => new() { Id = id };
}
