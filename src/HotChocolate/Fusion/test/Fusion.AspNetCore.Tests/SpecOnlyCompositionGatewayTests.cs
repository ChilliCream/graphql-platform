using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Mutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class SpecOnlyCompositionGatewayTests
{
    private const string SourceA =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query @shareable @cost(weight: "1") {
          items: [Item] @shareable @cost(weight: "2")
            @listSize(assumedSize: 2, slicingArguments: ["missing"])
        }

        type Item @shareable @cost(weight: "5") {
          value: String @shareable @cost(weight: "7")
        }
        """;

    private const string SourceB =
        """
        type Query @shareable @cost(weight: "3") {
          items: [Item] @shareable @cost(weight: "11")
        }

        type Item @shareable @cost(weight: "13") {
          value: String @shareable @cost(weight: "17")
        }
        """;

    private static readonly Uri s_endpoint = new("http://localhost:5000/graphql");

    [Fact]
    public async Task SpecOnlySources_Should_LoadAndPriceFoldedDirectives_When_GatewayStarts()
    {
        // arrange
        var log = new CompositionLog();
        var result = Compose([new("A", SourceA), new("B", SourceB)], log);
        Assert.True(result.IsSuccess);
        var compositeSchema = result.Value;
        var compositeDocument = compositeSchema.ToSyntaxNode();

        await using var executionSchema = FusionSchemaDefinition.Create(compositeDocument);
        using var session = new TestServerSession();
        using var gateway = CreateGateway(session, compositeDocument);
        var request = new OperationRequest("{ items { value } }");

        // act
        var operationCost = await SendValidateAsync(gateway, request);

        // assert
        Assert.True(executionSchema.DirectiveDefinitions.ContainsName(WellKnownDirectiveNames.Cost));
        Assert.True(executionSchema.DirectiveDefinitions.ContainsName(WellKnownDirectiveNames.ListSize));
        operationCost.MatchInlineSnapshot(
            """
            {
              "fieldCost": 45,
              "typeCost": 29
            }
            """);
        PrintCostSchema(compositeSchema).MatchSnapshot();
    }

    [Fact]
    public void ArgumentlessCostDefinition_Should_ReturnCompositionError_When_CostIsApplied()
    {
        // arrange
        var log = new CompositionLog();

        // act
        var result = Compose(
            [
                new SourceSchemaText(
                    "A",
                    """
                    directive @cost on FIELD_DEFINITION

                    type Query {
                      field: Int @cost
                    }
                    """)
            ],
            log);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(log).ToString().MatchInlineSnapshot(
            """
            {
              "message": "The @cost directive must have a 'weight' argument of type String.",
              "code": "INVALID_GRAPHQL",
              "severity": "Error",
              "coordinate": "Query.field",
              "member": "field",
              "schema": "A",
              "extensions": {}
            }
            """);
    }

    private static CompositionResult<MutableSchemaDefinition> Compose(
        SourceSchemaText[] sources,
        CompositionLog log)
        => new SchemaComposer(sources, new SchemaComposerOptions(), log).Compose();

    private static TestServer CreateGateway(
        TestServerSession session,
        DocumentNode compositeDocument)
    {
        return session.CreateServer(
            services =>
            {
                services.AddRouting();
                services
                    .AddGraphQLGatewayServer()
                    .AddInMemoryConfiguration(compositeDocument)
                    .AddHttpClientConfiguration("A", s_endpoint)
                    .AddHttpClientConfiguration("B", s_endpoint);
            },
            app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQL());
            });
    }

    private static async Task<System.Text.Json.JsonElement> SendValidateAsync(
        TestServer gateway,
        OperationRequest request)
    {
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            new GraphQLHttpRequest(request, s_endpoint)
            {
                OnMessageCreated = (_, message, _) => message.Headers.Add("GraphQL-Cost", "validate")
            },
            TestContext.Current.CancellationToken);

        var results = new List<OperationResult>();
        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        var operationCost = Assert.Single(results).Extensions.GetProperty("operationCost").Clone();

        foreach (var result in results)
        {
            result.Dispose();
        }

        return operationCost;
    }

    private static string PrintCostSchema(MutableSchemaDefinition schema)
        => string.Join(
            "\n\n",
            schema.DirectiveDefinitions[WellKnownDirectiveNames.Cost].ToSyntaxNode(),
            schema.DirectiveDefinitions[WellKnownDirectiveNames.ListSize].ToSyntaxNode(),
            schema.Types["Query"].ToSyntaxNode(),
            schema.Types["Item"].ToSyntaxNode());
}
