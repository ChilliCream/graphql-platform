using HotChocolate.Execution;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased;

public class CertificationTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        var executor = await SchemaSetup.CreateAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_SDL()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                {
                  _service {
                    sdl
                  }
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result
            .ExpectOperationResult()
            .UnwrapData()
            .GetProperty("_service")
            .GetProperty("sdl")
            .GetString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Id()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($representations: [_Any!]!) {
                  _entities(representations: $representations) {
                    ... on Product {
                      sku
                    }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "representations": [
                    {
                      "__typename": "Product",
                      "id": "apollo-federation"
                    }
                  ]
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Package()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($representations: [_Any!]!) {
                  _entities(representations: $representations) {
                    ... on Product {
                      sku
                    }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "representations": [
                    {
                      "__typename": "Product",
                      "sku": "federation",
                      "package": "@apollo/federation"
                    }
                  ]
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Product_By_Variation()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($representations: [_Any!]!) {
                  _entities(representations: $representations) {
                    ... on Product {
                      sku
                    }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "representations": [
                    {
                      "__typename": "Product",
                      "sku": "federation",
                      "variation": {
                        "id": "OSS"
                      }
                    }
                  ]
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Provides()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($id: ID!) {
                  product(id: $id) {
                    createdBy { email totalProductsCreated }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "id": "apollo-federation"
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Requires()
    {
        // arrange
        var executor = await SchemaSetup.CreateAsync();

        // act
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($id: ID!) {
                  product(id: $id) {
                    dimensions { size weight }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "id": "apollo-federation"
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }
}
