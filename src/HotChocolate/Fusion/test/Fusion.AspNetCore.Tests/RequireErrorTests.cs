using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public sealed class RequireErrorTests : FusionTestBase
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_SkipDirectAndTransitiveDependents_When_UnselectedRequirementFails(ErrorHandlingMode mode)
    {
        // arrange
        var calls = new ConcurrentBag<string>();
        var request = new OperationRequest("{ products { id weight estimate } }", onError: mode);

        // act
        using var response = await ExecuteAsync(request, calls);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "weight": 30,
                    "estimate": 130
                  },
                  {
                    "id": "B",
                    "weight": null,
                    "estimate": null
                  },
                  {
                    "id": "C",
                    "weight": 120,
                    "estimate": 220
                  }
                ]
              },
              "errors": [
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "products",
                    1,
                    "estimate"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                },
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "products",
                    1,
                    "weight"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                }
              ]
            }
            """);
        Assert.Equal(["A.estimate", "A.weight", "C.estimate", "C.weight"], calls.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_SkipSiblingFields_When_SharedTransitiveRequirementFails(ErrorHandlingMode mode)
    {
        // arrange
        var calls = new ConcurrentBag<string>();
        var request = new OperationRequest("{ products { id estimate tax } }", onError: mode);

        // act
        using var response = await ExecuteAsync(request, calls);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "estimate": 130,
                    "tax": 3
                  },
                  {
                    "id": "B",
                    "estimate": null,
                    "tax": null
                  },
                  {
                    "id": "C",
                    "estimate": 220,
                    "tax": 12
                  }
                ]
              },
              "errors": [
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "products",
                    1,
                    "estimate"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                },
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "products",
                    1,
                    "tax"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                }
              ]
            }
            """);
        Assert.Equal(["A.estimate", "A.tax", "A.weight", "C.estimate", "C.tax", "C.weight"], calls.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportOnlyUnresolvedDependents_When_AllLookupsAreSkipped(ErrorHandlingMode mode)
    {
        // arrange
        var calls = new ConcurrentBag<string>();
        var request = new OperationRequest(
            "{ items: products(failingOnly: true) { id note quote: quotedEstimate levy: tax } }",
            onError: mode);

        // act
        using var response = await ExecuteAsync(request, calls);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": "B",
                    "note": null,
                    "quote": null,
                    "levy": null
                  }
                ]
              },
              "errors": [
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "items",
                    0,
                    "levy"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                },
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "items",
                    0,
                    "quote"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                }
              ]
            }
            """);
        Assert.Empty(calls);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepSourceError_When_RequiredDependentIsMissing(ErrorHandlingMode mode)
    {
        // arrange
        var calls = new ConcurrentBag<string>();
        using var gateway = await CreateRequiresGatewayAsync(calls, includeOperationPlan: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ items: products { id quote: requiredEstimate } }", onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
        Assert.Equal(["A.estimate", "A.weight", "C.estimate", "C.weight"], calls.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportDependentPath_When_NestedRequirementParentFails(ErrorHandlingMode mode)
    {
        // arrange
        var calls = new ConcurrentBag<string>();
        var request = new OperationRequest(
            "{ items: products(failingOnly: true) { id quote: dimensionalEstimate } }",
            onError: mode);

        // act
        using var result = await ExecuteAsync(request, calls);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": "B",
                    "quote": null
                  }
                ]
              },
              "errors": [
                {
                  "message": "volume unavailable for product B",
                  "path": [
                    "items",
                    0,
                    "quote"
                  ],
                  "extensions": {
                    "code": "VOLUME_UNAVAILABLE",
                    "productId": "B"
                  }
                }
              ]
            }
            """);
        Assert.Empty(calls);
    }

    private async Task<OperationResult> ExecuteAsync(OperationRequest request, ConcurrentBag<string> calls)
    {
        using var gateway = await CreateRequiresGatewayAsync(calls, includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        return await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
    }

    private Task<Gateway> CreateRequiresGatewayAsync(ConcurrentBag<string> calls, bool includeOperationPlan)
    {
        var catalog = CreateSourceSchema("catalog", b => b.AddQueryType<Catalog>(d => d.Name("Query")));
        var geometry = CreateSourceSchema("geometry", b => b.AddQueryType<Geometry>(d => d.Name("Query")));
        var metrics = CreateSourceSchema(
            "metrics", b => b.AddQueryType<Metrics>(d => d.Name("Query")), TrackCalls);
        var shipping = CreateSourceSchema(
            "shipping", b => b.AddQueryType<Shipping>(d => d.Name("Query")), TrackCalls);
        var billing = CreateSourceSchema(
            "billing", b => b.AddQueryType<Billing>(d => d.Name("Query")), TrackCalls);

        return CreateCompositeSchemaAsync(
            [
                ("catalog", catalog),
                ("geometry", geometry),
                ("metrics", metrics),
                ("shipping", shipping),
                ("billing", billing)
            ],
            includeOperationPlan: includeOperationPlan);

        void TrackCalls(IServiceCollection services) => services.AddSingleton(calls);
    }

    public sealed class Catalog
    {
        public List<Product?> GetProducts(bool failingOnly = false)
            => failingOnly ? [new("B")] : [new("A"), new("B"), new("C")];
    }

    [GraphQLName("Product")]
    public sealed record Product([property: ID, Shareable] string Id)
    {
        public string? Note => null;
    }

    public sealed class Geometry
    {
        [Lookup, Internal]
        public GeometryProduct? GetGeometry([ID] string id) => new(id);
    }

    [GraphQLName("Product")]
    public sealed record GeometryProduct([property: ID] string Id)
    {
        public Dimensions GetDimensions() => new(GetVolume());

        public int GetVolume() => Id switch
        {
            "B" => throw ThrowHelper.VolumeUnavailable(Id),
            "A" => 6,
            _ => 24
        };
    }

    public sealed record Dimensions(int Volume);

    public sealed class Metrics
    {
        [Lookup, Internal]
        public MetricsProduct? GetMetrics([ID] string id) => new(id);
    }

    [GraphQLName("Product")]
    public sealed record MetricsProduct([property: ID] string Id)
    {
        public int? GetWeight([Require] int? volume, [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.weight");

            return volume * 5 ?? 77;
        }
    }

    public sealed class Shipping
    {
        [Lookup, Internal]
        public ShippingProduct? GetShipping([ID] string id) => new(id);
    }

    [GraphQLName("Product")]
    public sealed record ShippingProduct([property: ID] string Id)
    {
        public int? GetEstimate([Require] int? weight, [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.estimate");

            return weight + 100 ?? 99;
        }

        public int? GetDimensionalEstimate(
            [Require("dimensions.volume")] int? volume,
            [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.dimensionalEstimate");

            return volume + 100;
        }

        public int GetRequiredEstimate([Require] int? weight, [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.estimate");

            return weight.GetValueOrDefault() + 100;
        }

        public int? GetQuotedEstimate([Require("{ weight }")] QuoteInput quote, [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.quotedEstimate");

            return quote.Weight + 100;
        }
    }

    public sealed record QuoteInput(int? Weight);

    public sealed class Billing
    {
        [Lookup, Internal]
        public BillingProduct? GetBilling([ID] string id) => new(id);
    }

    [GraphQLName("Product")]
    public sealed record BillingProduct([property: ID] string Id)
    {
        public int? GetTax([Require] int? weight, [Service] ConcurrentBag<string> calls)
        {
            calls.Add($"{Id}.tax");

            return weight / 10 ?? 66;
        }
    }
}
