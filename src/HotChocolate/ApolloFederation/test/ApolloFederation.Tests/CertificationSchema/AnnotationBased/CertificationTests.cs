using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased;

public class CertificationTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        var executor = await SchemaSetup.CreateAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_SDL()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                _service {
                    sdl
                }
            }");

        // assert
        Assert.IsType<ObjectResult>(
            Assert.IsType<ObjectResult>(
                Assert.IsType<OperationResult>(result).Data)
                    .GetValueOrDefault("_service"))
                        .GetValueOrDefault("sdl")
                            .MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Id()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query ($representations: [_Any!]!) {
                _entities(representations: $representations) {
                    ... on Product {
                        sku
                    }
                }
            }",
            new Dictionary<string, object?>
            {
                ["representations"] = new List<object?>
                {
                    new ObjectValueNode(
                        new ObjectFieldNode("__typename", "Product"),
                        new ObjectFieldNode("id", "apollo-federation")),
                },
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Package()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query ($representations: [_Any!]!) {
                _entities(representations: $representations) {
                    ... on Product {
                        sku
                    }
                }
            }",
            new Dictionary<string, object?>
            {
                ["representations"] = new List<object?>
                {
                    new ObjectValueNode(
                        new ObjectFieldNode("__typename", "Product"),
                        new ObjectFieldNode("sku", "federation"),
                        new ObjectFieldNode("package", "@apollo/federation")),
                },
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Variation()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query ($representations: [_Any!]!) {
                _entities(representations: $representations) {
                    ... on Product {
                        sku
                    }
                }
            }",
            new Dictionary<string, object?>
            {
                ["representations"] = new List<object?>
                {
                    new ObjectValueNode(
                        new ObjectFieldNode("__typename", "Product"),
                        new ObjectFieldNode("sku", "federation"),
                        new ObjectFieldNode("variation",
                            new ObjectValueNode(
                                new ObjectFieldNode("id", "OSS")))),
                },
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Provides()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query ($id: ID!) {
                product(id: $id) {
                    createdBy { email totalProductsCreated }
                }
            }",
            new Dictionary<string, object?>
            {
                ["id"] = "apollo-federation",
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Requires()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"query ($id: ID!) {
                product(id: $id) {
                    dimensions { size weight }
                }
            }",
            new Dictionary<string, object?>
            {
                ["id"] = "apollo-federation",
            });

        // assert
        result.ToJson().MatchSnapshot();
    }
}
