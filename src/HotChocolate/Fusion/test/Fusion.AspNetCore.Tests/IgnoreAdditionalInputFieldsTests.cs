using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class IgnoreAdditionalInputFieldsTests : FusionTestBase
{
    [Fact]
    public async Task Gateway_Should_SkipUndefinedInputFields_When_IgnoreAdditionalInputFieldsIsEnabled()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              search(input: SearchInput!): String
            }

            input SearchInput {
              term: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.IgnoreAdditionalInputFields = true));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = CreateRequest();

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Gateway_Should_RejectUndefinedInputFields_When_IgnoreAdditionalInputFieldsIsDisabled()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              search(input: SearchInput!): String
            }

            input SearchInput {
              term: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = CreateRequest();

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private static OperationRequest CreateRequest()
        => new(
            """
            query testQuery($input: SearchInput!) {
              search(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?>
                {
                    ["term"] = "abc",
                    ["unknown"] = "wrong"
                }
            });
}
