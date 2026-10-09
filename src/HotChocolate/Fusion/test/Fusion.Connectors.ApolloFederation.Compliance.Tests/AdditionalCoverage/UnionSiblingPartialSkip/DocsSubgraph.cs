using System.Text;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.AdditionalCoverage.UnionSiblingPartialSkip.Docs;

/// <summary>
/// The <c>docs</c> Apollo Federation subgraph for the union-sibling partial-skip
/// tests. Resolves the <c>Doc</c> entity by key, but its <c>_entities</c>
/// endpoint fails slowly with HTTP 500, so the gateway processes this failure
/// after the sibling asset lookup batch has already completed.
/// </summary>
public static class DocsSubgraph
{
    public const string Name = "docs";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<DocType>()
            .AddType<MeterTypeType>();

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsPost(context.Request.Method))
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    leaveOpen: true);
                var body = await reader.ReadToEndAsync(context.RequestAborted);
                context.Request.Body.Position = 0;

                if (body.Contains("_entities", StringComparison.Ordinal))
                {
                    await Task.Delay(300, context.RequestAborted);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return;
                }
            }

            await next();
        });

        app.MapSubgraph(enableBatching: true);

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}

public sealed class Doc
{
    public string Id { get; init; } = default!;
}

public sealed class MeterType
{
    public string Id { get; init; } = default!;
}

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("docsVersion")
            .Type<StringType>()
            .Resolve(_ => "1");
    }
}

public sealed class DocType : ObjectType<Doc>
{
    protected override void Configure(IObjectTypeDescriptor<Doc> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(d => d.Id).Type<NonNullType<StringType>>();

        descriptor
            .Field("meterType")
            .Type<MeterTypeType>()
            .Resolve(ctx => new MeterType { Id = $"md{ctx.Parent<Doc>().Id}" });
    }

    private static Doc ResolveById(string id) => new() { Id = id };
}

public sealed class MeterTypeType : ObjectType<MeterType>
{
    protected override void Configure(IObjectTypeDescriptor<MeterType> descriptor)
    {
        descriptor.Key("id", resolvable: false);

        descriptor.Field(m => m.Id).Type<NonNullType<StringType>>();
    }
}
