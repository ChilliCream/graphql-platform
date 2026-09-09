using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public sealed class DocumentShapeParityTests : FusionTestBase
{
    private static readonly Uri s_endpoint = new("http://localhost:5000/graphql");

    [Fact]
    public async Task NamedFragments_Should_MatchInlineCost_When_C1HasExclusiveTypes()
    {
        // arrange
        var named = LoadFixture("c1-exclusive-types-fragments");
        var inline = LoadFixture("c1-exclusive-types");
        using var server = CreateSourceSchema("A", named.Sdl);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        var namedCost = await SendValidateAsync(gateway, named.Operation);
        var inlineCost = await SendValidateAsync(gateway, inline.Operation);

        // assert
        new
        {
            Expected = named.Expected,
            Named = namedCost,
            Inline = inlineCost
        }.MatchInlineSnapshot(
            """
            {
              "Expected": {
                "TypeCost": 2.0,
                "FieldCost": 21.0
              },
              "Named": {
                "TypeCost": 2.0,
                "FieldCost": 21.0
              },
              "Inline": {
                "TypeCost": 2.0,
                "FieldCost": 21.0
              }
            }
            """);
    }

    [Fact]
    public async Task NamedFragments_Should_MatchInlineCost_When_C4HasDuplicateResponseName()
    {
        // arrange
        var named = LoadFixture("c4-duplicate-response-name-fragments");
        var inline = LoadFixture("c4-duplicate-response-name");
        using var server = CreateSourceSchema("A", named.Sdl);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        var namedCost = await SendValidateAsync(gateway, named.Operation);
        var inlineCost = await SendValidateAsync(gateway, inline.Operation);

        // assert
        new
        {
            Expected = named.Expected,
            Named = namedCost,
            Inline = inlineCost
        }.MatchInlineSnapshot(
            """
            {
              "Expected": {
                "TypeCost": 2.0,
                "FieldCost": 11.0
              },
              "Named": {
                "TypeCost": 2.0,
                "FieldCost": 11.0
              },
              "Inline": {
                "TypeCost": 2.0,
                "FieldCost": 11.0
              }
            }
            """);
    }

    private static ParityFixture LoadFixture(string id)
    {
        var path = System.IO.Path.Combine("__resources__", "article", id + ".json");
        var fixture = JsonSerializer.Deserialize<ParityFixture>(
            File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return fixture
            ?? throw new InvalidOperationException($"Fixture '{path}' deserialized to null.");
    }

    private static async Task<CostValue> SendValidateAsync(Gateway gateway, string operation)
    {
        var request = new OperationRequest(operation);
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

        var operationCost = Assert.Single(results).Extensions.GetProperty("operationCost");
        var cost = new CostValue(
            operationCost.GetProperty("typeCost").GetDouble(),
            operationCost.GetProperty("fieldCost").GetDouble());

        foreach (var result in results)
        {
            result.Dispose();
        }

        return cost;
    }

    private sealed record ParityFixture(
        string Sdl,
        string Operation,
        ParityExpected Expected);

    private sealed record ParityExpected(double TypeCost, double FieldCost);

    private sealed record CostValue(double TypeCost, double FieldCost);
}
