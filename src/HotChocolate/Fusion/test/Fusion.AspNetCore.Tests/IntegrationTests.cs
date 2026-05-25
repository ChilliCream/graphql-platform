using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class IntegrationTests : FusionTestBase
{
    [Fact]
    public async Task Recursive_Input_Object_Type()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field(input: RecursiveInput!): String
            }

            input RecursiveInput {
              child: RecursiveInput
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($input: RecursiveInput!) {
              field(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?>
                {
                    ["child"] = null
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}
