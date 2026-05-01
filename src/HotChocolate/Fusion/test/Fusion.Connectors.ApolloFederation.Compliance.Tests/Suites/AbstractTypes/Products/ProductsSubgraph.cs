using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public static class ProductsSubgraph
{
    public const string Name = "products";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<ProductInterfaceType>()
            .AddType<SimilarInterfaceType>()
            .AddType<ProductBookType>()
            .AddType<ProductMagazineType>()
            .AddType<ProductDimensionType>()
            .AddType<ProductUserType>()
            .AddType<PublisherTypeUnion>()
            .AddType<ProductAgencyType>()
            .AddType<SelfType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
