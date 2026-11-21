using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Caching;

public partial class FusionCachingTests
{
    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_EntityFetchBringsInUnannotatedField()
    {
        // arrange
        using var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<LookupCatalogSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var detailsServer = CreateSourceSchema(
            "details",
            b => b.AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<ApolloDetailsSchema.Product>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalogServer), ("details", detailsServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { name material } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_EntityFetchesShareABatch()
    {
        // arrange
        using var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<LookupCatalogSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var reviewsServer = CreateSourceSchema(
            "reviews",
            b => b.AddQueryType<ReviewsSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var detailsServer = CreateSourceSchema(
            "details",
            b => b.AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<ApolloDetailsSchema.Product>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("catalog", catalogServer),
                ("reviews", reviewsServer),
                ("details", detailsServer)
            ],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "{ products { material } topReviews { product { material } } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }
}

public static class ApolloDetailsSchema
{
    [Key("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        public string Material => "Oak";

        [ReferenceResolver]
        public static Product Get(int id) => new(id);
    }
}
