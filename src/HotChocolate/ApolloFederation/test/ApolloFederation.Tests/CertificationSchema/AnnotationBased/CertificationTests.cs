using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased;

public class CertificationTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        IRequestExecutor executor = await SchemaSetup.CreateAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_SDL()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            @"{
                _service {
                    sdl
                }
            }");

        // assert
        Assert.IsType<ResultMap>(
            Assert.IsType<ResultMap>(
                Assert.IsType<QueryResult>(result).Data)
                    .GetValueOrDefault("_service"))
                        .GetValueOrDefault("sdl")
                            .MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Id()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
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
                        new ObjectFieldNode("id", "apollo-federation"))
                }
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Package()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
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
                        new ObjectFieldNode("package", "@apollo/federation"))
                }
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Variation()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
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
                                new ObjectFieldNode("id", "OSS"))))
                }
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Provides()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            @"query ($id: ID!) {
                product(id: $id) {
                    createdBy { email totalProductsCreated }
                }
            }",
            new Dictionary<string, object?>
            {
                ["id"] = "apollo-federation"
            });

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Requires()
    {
        // arrange
        IRequestExecutor executor = await SchemaSetup.CreateAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            @"query ($id: ID!) {
                product(id: $id) {
                    dimensions { size weight }
                }
            }",
            new Dictionary<string, object?>
            {
                ["id"] = "apollo-federation"
            });

        // assert
        result.ToJson().MatchSnapshot();
    }
}
