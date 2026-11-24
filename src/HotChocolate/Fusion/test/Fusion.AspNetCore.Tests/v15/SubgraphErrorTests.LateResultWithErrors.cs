using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Fact]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsErrorsBelowAnAlreadyNulledObject()
    {
        // arrange
        var order = new SourceResultOrder("AB");
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { details: Details @shareable }
            type Details @shareable { name: String! }
            """,
            """
            {
              "data": { "details": { "name": null } },
              "errors": [{ "message": "Name unavailable.", "path": ["details", "name"] }]
            }
            """,
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { details: Details @shareable total: Int }
            type Details @shareable { price: Int }
            """,
            """
            {
              "data": { "details": { "price": null }, "total": 4 },
              "errors": [{ "message": "Price unavailable.", "path": ["details", "price"] }]
            }
            """,
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder
                .AddDiagnosticEventListener(_ => order)
                .ModifyRequestOptions(options => options.AllowOperationPlanRequests = false));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            "{ details { name price } total }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "details": null,
                "total": 4
              },
              "errors": [
                {
                  "message": "Name unavailable.",
                  "path": [
                    "details",
                    "name"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsErrorsBelowAnAlreadyNulledList()
    {
        // arrange
        var order = new SourceResultOrder("AB");
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { rows: [Row!] @shareable }
            type Row @shareable { name: String! }
            """,
            """
            {
              "data": { "rows": [{ "name": null }] },
              "errors": [{ "message": "Name unavailable.", "path": ["rows", 0, "name"] }]
            }
            """,
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { rows: [Row!] @shareable total: Int }
            type Row @shareable { price: Int }
            """,
            """
            {
              "data": { "rows": [{ "price": null }], "total": 4 },
              "errors": [{ "message": "Price unavailable.", "path": ["rows", 0, "price"] }]
            }
            """,
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder
                .AddDiagnosticEventListener(_ => order)
                .ModifyRequestOptions(options => options.AllowOperationPlanRequests = false));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            "{ rows { name price } total }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "rows": null,
                "total": 4
              },
              "errors": [
                {
                  "message": "Name unavailable.",
                  "path": [
                    "rows",
                    0,
                    "name"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_KeepOtherItems_When_SubgraphReturnsErrorsBelowAnAlreadyNulledListItem()
    {
        // arrange
        var order = new SourceResultOrder("AB");
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { rows: [Row] @shareable }
            type Row @shareable { name: String! }
            """,
            """
            {
              "data": { "rows": [{ "name": null }, { "name": "n" }] },
              "errors": [{ "message": "Name unavailable.", "path": ["rows", 0, "name"] }]
            }
            """,
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { rows: [Row] @shareable total: Int }
            type Row @shareable { price: Int }
            """,
            """
            {
              "data": { "rows": [{ "price": null }, { "price": 2 }], "total": 4 },
              "errors": [{ "message": "Price unavailable.", "path": ["rows", 0, "price"] }]
            }
            """,
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder
                .AddDiagnosticEventListener(_ => order)
                .ModifyRequestOptions(options => options.AllowOperationPlanRequests = false));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            "{ rows { name price } total }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "rows": [
                  null,
                  {
                    "name": "n",
                    "price": 2
                  }
                ],
                "total": 4
              },
              "errors": [
                {
                  "message": "Name unavailable.",
                  "path": [
                    "rows",
                    0,
                    "name"
                  ]
                }
              ]
            }
            """);
    }
}
