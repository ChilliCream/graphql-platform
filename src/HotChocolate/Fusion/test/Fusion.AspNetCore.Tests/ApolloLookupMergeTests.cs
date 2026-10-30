using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloLookupMergeTests : FusionTestBase
{
    [Fact]
    public async Task ApolloEntityLookups_Should_SendOneDeduplicatedLookup_When_TargetPathsShareLookup()
    {
        // arrange
        using var gateway = await CreateGatewayAsync();
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
        using var gateway = await CreateGatewayAsync();
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

    [Fact]
    public async Task DeferredApolloLookups_Should_DeliverNonDeferredData_When_NestedGroupNeedsKeyOfOuterGroupField()
    {
        // arrange
        using var gateway = await CreateRelatedGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var deferredRequest = new OperationRequest(
            """
            {
              products {
                ... @defer(label: "outer") {
                  name
                  related {
                    id
                    ... @defer(label: "inner") { name }
                  }
                }
              }
            }
            """);
        var request = new OperationRequest(
            """
            {
              products {
                name
                related {
                  id
                  name
                }
              }
            }
            """);

        // act
        var deferredData = await ExecuteAndMergeAsync(client, deferredRequest);
        var data = await ExecuteAndMergeAsync(client, request);

        // assert
        Assert.Equal(data, deferredData);
        deferredData.MatchInlineSnapshot(
            """
            {"products":[{"name":"Product p1","related":{"id":"p1-rel","name":"Product p1-rel"}},{"name":"Product p2","related":{"id":"p2-rel","name":"Product p2-rel"}}]}
            """);
    }

    [Fact]
    public async Task DeferredApolloLookups_Should_DeliverNonDeferredData_When_GroupIsAnchoredAtObjectBelowListItem()
    {
        // arrange
        using var gateway = await CreateRelatedGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var deferredRequest = new OperationRequest(
            """
            {
              products {
                related {
                  id
                  ... @defer(label: "deeper") { name }
                }
              }
            }
            """);
        var request = new OperationRequest(
            """
            {
              products {
                related {
                  id
                  name
                }
              }
            }
            """);

        // act
        var deferredData = await ExecuteAndMergeAsync(client, deferredRequest);
        var data = await ExecuteAndMergeAsync(client, request);

        // assert
        Assert.Equal(data, deferredData);
        deferredData.MatchInlineSnapshot(
            """
            {"products":[{"related":{"id":"p1-rel","name":"Product p1-rel"}},{"related":{"id":"p2-rel","name":"Product p2-rel"}}]}
            """);
    }

    [Fact]
    public async Task DeferredApolloLookups_Should_DeliverNonDeferredData_When_GroupIsAnchoredAtObjectBelowRoot()
    {
        // arrange
        using var gateway = await CreateRelatedGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var deferredRequest = new OperationRequest(
            """
            {
              product {
                related {
                  id
                  ... @defer(label: "deeper") { name }
                }
              }
            }
            """);
        var request = new OperationRequest(
            """
            {
              product {
                related {
                  id
                  name
                }
              }
            }
            """);

        // act
        var deferredData = await ExecuteAndMergeAsync(client, deferredRequest);
        var data = await ExecuteAndMergeAsync(client, request);

        // assert
        Assert.Equal(data, deferredData);
        deferredData.MatchInlineSnapshot(
            """
            {"product":{"related":{"id":"p1-rel","name":"Product p1-rel"}}}
            """);
    }

    private static async Task<string> ExecuteAndMergeAsync(
        GraphQLHttpClient client,
        OperationRequest request)
    {
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        var body = await result.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        JsonNode? data = null;
        var pendingPaths = new Dictionary<string, JsonArray>();

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var payload = JsonNode.Parse(line)!.AsObject();
            data ??= payload["data"]?.DeepClone();

            if (payload["completed"] is JsonArray completed)
            {
                foreach (var entry in completed)
                {
                    if (entry!["errors"] is JsonArray errors)
                    {
                        Assert.Fail(
                            $"The delivery group '{entry["id"]}' completed with errors: {errors.ToJsonString()}");
                    }
                }
            }

            if (payload["pending"] is JsonArray pending)
            {
                foreach (var entry in pending)
                {
                    pendingPaths[entry!["id"]!.GetValue<string>()] = entry["path"]!.AsArray();
                }
            }

            if (payload["incremental"] is not JsonArray incremental)
            {
                continue;
            }

            foreach (var item in incremental)
            {
                var target = data;
                var path = pendingPaths[item!["id"]!.GetValue<string>()]
                    .Concat(item["subPath"] as JsonArray ?? []);

                foreach (var segment in path)
                {
                    target = segment!.GetValueKind() is JsonValueKind.Number
                        ? target?[segment.GetValue<int>()]
                        : target?[segment.GetValue<string>()];
                }

                if (target is JsonObject targetObject && item["data"] is JsonObject itemData)
                {
                    MergeInto(targetObject, itemData);
                }
            }
        }

        return data?.ToJsonString() ?? "null";
    }

    private static void MergeInto(JsonObject target, JsonObject source)
    {
        foreach (var (name, value) in source)
        {
            if (value is JsonObject sourceObject && target[name] is JsonObject targetObject)
            {
                MergeInto(targetObject, sourceObject);
            }
            else
            {
                target[name] = value?.DeepClone();
            }
        }
    }

    private Task<Gateway> CreateRelatedGatewayAsync()
    {
        var catalog = CreateSourceSchema(
            "a",
            b => b.AddApolloFederation()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("products").Type<NonNullType<ListType<NonNullType<KeyedProductType>>>>()
                        .Resolve(_ => new[] { new Product("p1"), new Product("p2") });
                    d.Field("product").Type<NonNullType<KeyedProductType>>()
                        .Resolve(_ => new Product("p1"));
                }));
        var related = CreateSourceSchema(
            "b",
            b => b.AddApolloFederation().AddQueryType(d => d.Name("Query"))
                .AddType<RelatedProductType>());
        var details = CreateSourceSchema(
            "c",
            b => b.AddApolloFederation().AddQueryType(d => d.Name("Query"))
                .AddType<NamedProductType>());

        return CreateCompositeSchemaAsync(
            [("a", catalog), ("b", related), ("c", details)],
            enableGlobalObjectIdentification: false);
    }

    private Task<Gateway> CreateGatewayAsync()
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

    public sealed class KeyedProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveProduct(default!));
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        }
    }

    public sealed class RelatedProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveProduct(default!));
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field("related").Type<NonNullType<RelatedProductType>>()
                .Resolve(ctx => new Product(ctx.Parent<Product>().Id + "-rel"));
        }
    }

    public sealed class NamedProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Key("id").ResolveReferenceWith(_ => ResolveProduct(default!));
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
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
