using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Planning;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloLookupMergeTests : Testing.FusionTestBase
{
    [Fact]
    public async Task ApolloEntityLookups_Should_SendOneDeduplicatedLookup_When_TargetPathsShareLookup()
    {
        // arrange
        using var gateway = await CreateGatewayAsync(grouping: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task ApolloEntityLookups_Should_FoldLeafLookupIntoDeeperLookup_When_NothingDependsOnLeaf()
    {
        // arrange
        using var gateway = await CreateGatewayAsync(grouping: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              products { name }
              brands { featured { name } }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData(false, OperationMergePolicy.Aggressive)]
    [InlineData(true, OperationMergePolicy.Aggressive)]
    [InlineData(false, OperationMergePolicy.None)]
    [InlineData(true, OperationMergePolicy.Conservative)]
    [InlineData(true, OperationMergePolicy.Balanced)]
    public async Task ApolloEntityLookups_Should_ApplyMergePolicy_When_RequestGroupingIsDisabled(
        bool crossDepth,
        OperationMergePolicy policy)
    {
        // arrange
        using var gateway = await CreateGatewayAsync(grouping: false, policy);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            crossDepth
                ? "{ products { name } brands { featured { name } } }"
                : "{ products { name } brands { topProduct { name } } }");

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(
            gateway,
            request,
            result,
            postFix: $"{(crossDepth ? "CrossDepth" : "SameDepth")}_{policy}");
    }

    private Task<Gateway> CreateGatewayAsync(bool grouping)
        => CreateGatewayAsync(grouping, OperationMergePolicy.Aggressive);

    private Task<Gateway> CreateGatewayAsync(bool grouping, OperationMergePolicy policy)
    {
        var catalog = CreateSourceSchema(
            "catalog",
            b => b.AddApolloFederation()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("products").Type<ListType<NonNullType<CatalogProductType>>>()
                        .Resolve(_ => new[] { new Product("p1"), new Product("p2") });
                    d.Field("brands").Type<ListType<NonNullType<CatalogBrandType>>>()
                        .Resolve(_ => new[] { new Brand("b1"), new Brand("b2") });
                }));
        var featured = CreateSourceSchema(
            "featured",
            b => b.AddApolloFederation().AddQueryType(d => d.Name("Query"))
                .AddType<FeaturedBrandType>());
        var details = CreateSourceSchema(
            "details",
            b => b.AddApolloFederation().AddQueryType(d => d.Name("Query"))
                .AddType<DetailsProductType>());

        return CreateCompositeSchemaAsync(
            [("catalog", catalog), ("featured", featured), ("details", details)],
            configureGatewayBuilder: builder => builder.ModifyPlannerOptions(options =>
            {
                options.EnableRequestGrouping = grouping;
                options.MergePolicy = policy;
            }),
            enableGlobalObjectIdentification: false);
    }

    public sealed record Product(string Id);

    public sealed record Brand(string Id);

    public sealed class CatalogProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveProduct(default!));
        }
    }

    public sealed class DetailsProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveProduct(default!));
            descriptor.Field("name").Type<NonNullType<StringType>>()
                .Resolve(ctx => "Product " + ctx.Parent<Product>().Id);
        }
    }

    public sealed class CatalogBrandType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Name("Brand");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveBrand(default!));
            descriptor.Field("topProduct").Type<NonNullType<CatalogProductType>>()
                .Resolve(ctx => ProductForBrand(ctx.Parent<Brand>()));
        }
    }

    public sealed class FeaturedBrandType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Name("Brand");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveBrand(default!));
            descriptor.Field("featured").Type<NonNullType<CatalogProductType>>()
                .Resolve(ctx => ProductForBrand(ctx.Parent<Brand>()));
        }
    }

    private static Product ResolveProduct(string id) => new(id);

    private static Brand ResolveBrand(string id) => new(id);

    private static Product ProductForBrand(Brand brand)
        => new(brand.Id == "b1" ? "p1" : "p2");
}
