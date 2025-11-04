using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class VariableCoercionTests : FusionTestBase
{
    [Fact]
    public async Task OneOf_Only_One_Option_Provided()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                petName(input: Pet!): String
            }

            input Pet @oneOf {
              dog: Dog
              cat: Cat
            }

            input Dog {
              breed: String!
            }

            input Cat {
              cuteness: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($pet: Pet!) {
              petName(input: $pet)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["pet"] = new Dictionary<string, object?>
                {
                    ["dog"] = new Dictionary<string, object?>
                    {
                        ["breed"] = "Rottweiler"
                    }
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OneOf_Only_One_Option_Provided_But_Value_Is_Null()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                petName(input: Pet!): String
            }

            input Pet @oneOf {
              dog: Dog
              cat: Cat
            }

            input Dog {
              breed: String!
            }

            input Cat {
              cuteness: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($pet: Pet!) {
              petName(input: $pet)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["pet"] = new Dictionary<string, object?>
                {
                    ["dog"] = null
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OneOf_No_Option_Provided()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                petName(input: Pet!): String
            }

            input Pet @oneOf {
              dog: Dog
              cat: Cat
            }

            input Dog {
              breed: String!
            }

            input Cat {
              cuteness: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($pet: Pet!) {
              petName(input: $pet)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["pet"] = new Dictionary<string, object?>()
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OneOf_Multiple_Options_Provided()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                petName(input: Pet!): String
            }

            input Pet @oneOf {
              dog: Dog
              cat: Cat
            }

            input Dog {
              breed: String!
            }

            input Cat {
              cuteness: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($pet: Pet!) {
              petName(input: $pet)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["pet"] = new Dictionary<string, object?>
                {
                    ["dog"] = new Dictionary<string, object?>
                    {
                        ["breed"] = "Rottweiler"
                    },
                    ["cat"] = new Dictionary<string, object?>
                    {
                        ["cuteness"] = 100.0
                    }
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}
