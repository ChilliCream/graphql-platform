using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.FusionInfo;

public sealed class FusionInfoResultTests
{
    [Fact]
    public void FusionInfoResult_Properties()
    {
        // act
        var result = new FusionInfoResult
        {
            Tag = "abc123",
            Subgraphs = [],
            ComposedSchemaPath = "/tmp/fusion.graphql",
            SubgraphSchemaPaths = new Dictionary<string, string>(),
            TotalTypes = 10,
            TotalFields = 50
        };

        // assert
        Assert.Equal("abc123", result.Tag);
        Assert.Empty(result.Subgraphs);
        Assert.Equal("/tmp/fusion.graphql", result.ComposedSchemaPath);
        Assert.Empty(result.SubgraphSchemaPaths);
        Assert.Equal(10, result.TotalTypes);
        Assert.Equal(50, result.TotalFields);
    }

    [Fact]
    public void SubgraphInfo_Properties()
    {
        // act
        var info = new SubgraphInfo
        {
            Name = "accounts",
            EndpointUrl = "https://accounts.example.com/graphql",
            SchemaCoordinateCount = 25,
            RootTypes = new SubgraphRootTypes
            {
                Query = ["me", "users"],
                Mutation = ["createUser"],
                Subscription = []
            }
        };

        // assert
        Assert.Equal("accounts", info.Name);
        Assert.Equal("https://accounts.example.com/graphql", info.EndpointUrl);
        Assert.Equal(25, info.SchemaCoordinateCount);
        Assert.Equal(2, info.RootTypes.Query.Count);
        Assert.Single(info.RootTypes.Mutation);
        Assert.Empty(info.RootTypes.Subscription);
    }

    [Fact]
    public void SubgraphInfo_Null_EndpointUrl()
    {
        // act
        var info = new SubgraphInfo
        {
            Name = "products",
            EndpointUrl = null,
            SchemaCoordinateCount = 5,
            RootTypes = new SubgraphRootTypes()
        };

        // assert
        Assert.Null(info.EndpointUrl);
    }

    [Fact]
    public void SubgraphRootTypes_Defaults_To_Empty_Lists()
    {
        // act
        var rootTypes = new SubgraphRootTypes();

        // assert
        Assert.Empty(rootTypes.Query);
        Assert.Empty(rootTypes.Mutation);
        Assert.Empty(rootTypes.Subscription);
    }

    [Fact]
    public void SubgraphRootTypes_With_Values()
    {
        // act
        var rootTypes = new SubgraphRootTypes
        {
            Query = ["users", "products", "orders"],
            Mutation = ["createUser", "deleteUser"],
            Subscription = ["onUserCreated"]
        };

        // assert
        Assert.Equal(3, rootTypes.Query.Count);
        Assert.Equal(2, rootTypes.Mutation.Count);
        Assert.Single(rootTypes.Subscription);
        Assert.Contains("users", rootTypes.Query);
        Assert.Contains("onUserCreated", rootTypes.Subscription);
    }

    [Fact]
    public void FusionInfoError_Properties()
    {
        // act
        var error = new FusionInfoError { Error = "Something failed" };

        // assert
        Assert.Equal("Something failed", error.Error);
    }

    [Fact]
    public void FusionInfoError_Default_Value()
    {
        // act
        var error = new FusionInfoError();

        // assert
        Assert.Equal(string.Empty, error.Error);
    }

    [Fact]
    public void FusionInfoResult_Serialization_Roundtrip()
    {
        // arrange
        var original = new FusionInfoResult
        {
            Tag = "v1",
            Subgraphs =
            [
                new SubgraphInfo
                {
                    Name = "users",
                    EndpointUrl = "https://users.example.com/graphql",
                    SchemaCoordinateCount = 15,
                    RootTypes = new SubgraphRootTypes
                    {
                        Query = ["me"],
                        Mutation = ["createUser"],
                        Subscription = []
                    }
                }
            ],
            ComposedSchemaPath = "/tmp/fusion.graphql",
            SubgraphSchemaPaths = new Dictionary<string, string>
            {
                ["users"] = "/tmp/users/schema.graphql"
            },
            TotalTypes = 5,
            TotalFields = 20
        };

        // act
        var json = JsonSerializer.Serialize(original,
            FusionInfoJsonContext.Default.FusionInfoResult);
        var deserialized = JsonSerializer.Deserialize(json,
            FusionInfoJsonContext.Default.FusionInfoResult);

        // assert
        Assert.NotNull(deserialized);
        Assert.Equal("v1", deserialized.Tag);
        Assert.Single(deserialized.Subgraphs);
        Assert.Equal("users", deserialized.Subgraphs[0].Name);
        Assert.Equal("https://users.example.com/graphql",
            deserialized.Subgraphs[0].EndpointUrl);
        Assert.Equal(15, deserialized.Subgraphs[0].SchemaCoordinateCount);
        Assert.Single(deserialized.Subgraphs[0].RootTypes.Query);
        Assert.Equal("/tmp/fusion.graphql", deserialized.ComposedSchemaPath);
        Assert.Equal(5, deserialized.TotalTypes);
        Assert.Equal(20, deserialized.TotalFields);
    }

    [Fact]
    public void FusionInfoError_Serialization_Roundtrip()
    {
        // arrange
        var original = new FusionInfoError { Error = "Not authenticated" };

        // act
        var json = JsonSerializer.Serialize(original,
            FusionInfoJsonContext.Default.FusionInfoError);
        var deserialized = JsonSerializer.Deserialize(json,
            FusionInfoJsonContext.Default.FusionInfoError);

        // assert
        Assert.NotNull(deserialized);
        Assert.Equal("Not authenticated", deserialized.Error);
    }

    [Fact]
    public void FusionInfoResult_Json_Property_Names()
    {
        // arrange
        var result = new FusionInfoResult
        {
            Tag = "t1",
            Subgraphs = [],
            ComposedSchemaPath = "/path",
            SubgraphSchemaPaths = new Dictionary<string, string>(),
            TotalTypes = 1,
            TotalFields = 2
        };

        // act
        var json = JsonSerializer.Serialize(result,
            FusionInfoJsonContext.Default.FusionInfoResult);

        // assert - verify camelCase JSON property names from JsonPropertyName attributes
        Assert.Contains("\"tag\"", json);
        Assert.Contains("\"subgraphs\"", json);
        Assert.Contains("\"composedSchemaPath\"", json);
        Assert.Contains("\"subgraphSchemaPaths\"", json);
        Assert.Contains("\"totalTypes\"", json);
        Assert.Contains("\"totalFields\"", json);
    }

    [Fact]
    public void FusionInfoResult_Multiple_Subgraphs()
    {
        // act
        var result = new FusionInfoResult
        {
            Tag = "multi",
            Subgraphs =
            [
                new SubgraphInfo
                {
                    Name = "accounts",
                    SchemaCoordinateCount = 10,
                    RootTypes = new SubgraphRootTypes { Query = ["me"] }
                },
                new SubgraphInfo
                {
                    Name = "products",
                    SchemaCoordinateCount = 20,
                    RootTypes = new SubgraphRootTypes { Query = ["products"] }
                },
                new SubgraphInfo
                {
                    Name = "reviews",
                    SchemaCoordinateCount = 8,
                    RootTypes = new SubgraphRootTypes { Query = ["reviews"] }
                }
            ],
            ComposedSchemaPath = "/tmp/fusion.graphql",
            SubgraphSchemaPaths = new Dictionary<string, string>
            {
                ["accounts"] = "/tmp/accounts/schema.graphql",
                ["products"] = "/tmp/products/schema.graphql",
                ["reviews"] = "/tmp/reviews/schema.graphql"
            },
            TotalTypes = 38,
            TotalFields = 100
        };

        // assert
        Assert.Equal(3, result.Subgraphs.Count);
        Assert.Equal(3, result.SubgraphSchemaPaths.Count);
    }
}
