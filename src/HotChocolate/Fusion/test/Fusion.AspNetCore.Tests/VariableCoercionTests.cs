using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class VariableCoercionTests : FusionTestBase
{
    [Fact]
    public async Task String_With_Quotes()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema1.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($input: String!) {
              field(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = "tag:\"type_portable-lamp\""
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Float_Input_Accepts_Whole_Number()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        var request = new OperationRequest(
            """
            query testQuery($input: Float!) {
              field(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = 500
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(500, response.Data.GetProperty("field").GetDouble());
    }

    [Fact]
    public async Task Float_Input_Accepts_Zero_Decimal()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        var request = new OperationRequest(
            """
            query testQuery($input: Float!) {
              field(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = 500.0
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(500, response.Data.GetProperty("field").GetDouble());
    }

    [Fact]
    public async Task Float_Input_Accepts_Fractional_Decimal()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($input: Float!) {
              field(input: $input)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = 500.99
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.Equal(500.99, response.Data.GetProperty("field").GetDouble(), precision: 2);
    }

    [Fact]
    public async Task InputObject_Invalid_Field()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                catName(input: Cat!): String
            }

            input Cat {
              cute: Boolean!
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
            query testQuery($cat: Cat!) {
              catName(input: $cat)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["cat"] = new Dictionary<string, object?>
                {
                    ["invalidField"] = "invalid"
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task InputObject_Missing_NonNull_Field()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                catName(input: Cat!): String
            }

            input Cat {
              name: String!
              cute: Boolean!
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
            query testQuery($cat: Cat!) {
              catName(input: $cat)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["cat"] = new Dictionary<string, object?>
                {
                    ["cute"] = true
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task InputObject_Missing_NonNull_Field_With_Default_Value()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                catName(input: Cat!): String
            }

            input Cat {
              name: String! = "Cat"
              cute: Boolean!
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
            query testQuery($cat: Cat!) {
              catName(input: $cat)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["cat"] = new Dictionary<string, object?>
                {
                    ["cute"] = true
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task InputObject_Missing_Nullable_Field()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
                catName(input: Cat!): String
            }

            input Cat {
              name: String
              cute: Boolean!
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
            query testQuery($cat: Cat!) {
              catName(input: $cat)
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["cat"] = new Dictionary<string, object?>
                {
                    ["cute"] = true
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

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
              cute: Boolean!
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
              cute: Boolean!
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
              cute: Boolean!
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
              cute: Boolean!
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
                        ["cute"] = true
                    }
                }
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public string GetField(string input) => input;
        }
    }

    public static class SourceSchema2
    {
        public class Query
        {
            public double GetField(double input) => input;
        }
    }
}
