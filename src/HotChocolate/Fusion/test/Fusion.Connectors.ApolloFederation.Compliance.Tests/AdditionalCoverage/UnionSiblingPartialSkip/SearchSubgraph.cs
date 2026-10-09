using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.AdditionalCoverage.UnionSiblingPartialSkip.Search;

/// <summary>
/// The <c>search</c> Apollo Federation subgraph for the union-sibling partial-skip
/// tests. Owns the <c>CandidateResult</c> union root field with an asset branch
/// per sibling type and a doc branch, exposes <c>Asset</c> and <c>Doc</c> only as
/// non-resolvable stubs, and resolves the <c>MeterType</c> entity so its fields
/// require an <c>_entities</c> lookup back into this subgraph.
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
            .AddType<ExistingResult2Type>()
            .AddType<OtherResultType>()
            .AddType<AssetType>()
            .AddType<DocType>()
            .AddType<MeterTypeType>();

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

public sealed class Doc
{
    public string Id { get; init; } = default!;
}

public sealed class MeterType
{
    public string Id { get; init; } = default!;
}

public sealed class ExistingResult
{
    public Asset Asset { get; init; } = default!;
}

public sealed class ExistingResult2
{
    public Asset Asset { get; init; } = default!;
}

public sealed class OtherResult
{
    public Doc Doc { get; init; } = default!;
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
                new ExistingResult { Asset = new Asset { Id = "1" } },
                new ExistingResult { Asset = new Asset { Id = "2" } },
                new OtherResult { Doc = new Doc { Id = "d1" } }
            });
    }
}

public sealed class CandidateResultType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("CandidateResult");
        descriptor.Type<ExistingResultType>();
        descriptor.Type<ExistingResult2Type>();
        descriptor.Type<OtherResultType>();
    }
}

public sealed class ExistingResultType : ObjectType<ExistingResult>
{
    protected override void Configure(IObjectTypeDescriptor<ExistingResult> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class ExistingResult2Type : ObjectType<ExistingResult2>
{
    protected override void Configure(IObjectTypeDescriptor<ExistingResult2> descriptor)
    {
        descriptor.Field(r => r.Asset).Type<AssetType>();
    }
}

public sealed class OtherResultType : ObjectType<OtherResult>
{
    protected override void Configure(IObjectTypeDescriptor<OtherResult> descriptor)
    {
        descriptor.Field(r => r.Doc).Type<DocType>();
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

public sealed class DocType : ObjectType<Doc>
{
    protected override void Configure(IObjectTypeDescriptor<Doc> descriptor)
    {
        descriptor.Key("id", resolvable: false);

        descriptor.Field(d => d.Id).Type<NonNullType<StringType>>();
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
