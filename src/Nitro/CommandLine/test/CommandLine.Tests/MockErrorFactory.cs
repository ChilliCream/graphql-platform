using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests;

/// <summary>
/// Provides shared factory methods for creating fully-populated mock error objects
/// used across multiple test base classes.
/// </summary>
internal static class MockErrorFactory
{
    /// <summary>
    /// Sets up <see cref="IOpenApiCollectionValidationError"/> on the given mock,
    /// including a "petstore" collection with a "GET /pets" endpoint and a document error.
    /// </summary>
    public static void SetupOpenApiCollectionValidationError<T>(Mock<T> mock) where T : class
    {
        var location = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations_1>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(10);
        location.SetupGet(x => x.Column).Returns(5);

        var docError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationDocumentError>(
            MockBehavior.Strict);
        docError.SetupGet(x => x.Code).Returns("INVALID");
        docError.SetupGet(x => x.Message).Returns("Invalid schema.");
        docError.SetupGet(x => x.Path).Returns("/paths/~1pets");
        docError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var endpoint = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationEndpoint>(
            MockBehavior.Strict);
        endpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.HttpMethod).Returns("GET");
        endpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.Route).Returns("/pets");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_1[]
            entityErrors = [docError.Object];
        endpoint.As<IOpenApiCollectionValidationEntity_OpenApiCollectionValidationEndpoint>()
            .SetupGet(x => x.Errors).Returns(entityErrors);
        endpoint.As<IOpenApiCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(entityErrors);

        var openApiCol = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollection>(
            MockBehavior.Strict);
        openApiCol.SetupGet(x => x.Name).Returns("petstore");
        openApiCol.SetupGet(x => x.Id).Returns("collection-1");

        var colValidation = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollectionValidationCollection>(
            MockBehavior.Strict);
        colValidation.SetupGet(x => x.OpenApiCollection).Returns(openApiCol.Object);
        colValidation.SetupGet(x => x.Entities).Returns(new[] { endpoint.Object });

        mock.As<IOpenApiCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_1[]
                { colValidation.Object });
    }

    /// <summary>
    /// Sets up <see cref="IMcpFeatureCollectionValidationError"/> on the given mock,
    /// including an "mcp-collection" collection with a "test-tool" tool and a document error.
    /// Uses "Invalid MCP schema." as the document error message.
    /// </summary>
    public static void SetupMcpFeatureCollectionValidationError<T>(Mock<T> mock) where T : class
    {
        SetupMcpFeatureCollectionValidationError(mock, "Invalid MCP schema.");
    }

    /// <summary>
    /// Sets up <see cref="IMcpFeatureCollectionValidationError"/> on the given mock,
    /// including an "mcp-collection" collection with a "test-tool" tool and a document error.
    /// </summary>
    public static void SetupMcpFeatureCollectionValidationError<T>(
        Mock<T> mock,
        string docErrorMessage) where T : class
    {
        var location = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(5);
        location.SetupGet(x => x.Column).Returns(3);

        var docError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError>(
            MockBehavior.Strict);
        docError.SetupGet(x => x.Code).Returns("INVALID");
        docError.SetupGet(x => x.Message).Returns(docErrorMessage);
        docError.SetupGet(x => x.Path).Returns("/tools/test");
        docError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var tool = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool>(
            MockBehavior.Strict);
        tool.As<IMcpFeatureCollectionValidationTool>().SetupGet(x => x.Name).Returns("test-tool");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors[]
            entityErrors = [docError.Object];
        tool.As<IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool>()
            .SetupGet(x => x.Errors).Returns(entityErrors);
        tool.As<IMcpFeatureCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(entityErrors);

        var mcpCol = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollection>(
            MockBehavior.Strict);
        mcpCol.SetupGet(x => x.Name).Returns("mcp-collection");
        mcpCol.SetupGet(x => x.Id).Returns("mcp-1");

        var colValidation = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollectionValidationCollection>(
            MockBehavior.Strict);
        colValidation.SetupGet(x => x.McpFeatureCollection).Returns(mcpCol.Object);
        colValidation.SetupGet(x => x.Entities).Returns(new[] { tool.Object });

        mock.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections[]
                { colValidation.Object });
    }

    /// <summary>
    /// Sets up <see cref="IInvalidGraphQLSchemaError"/> on the given mock,
    /// with a single schema error entry ("Field 'Query.foo' has no type.").
    /// </summary>
    public static void SetupInvalidGraphQLSchemaError<T>(Mock<T> mock) where T : class
    {
        var schemaErrorEntry = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(
            MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message).Returns("Field 'Query.foo' has no type.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_ERROR");

        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.__typename)
            .Returns("InvalidGraphQLSchemaError");
        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Message)
            .Returns("Invalid GraphQL schema.");
        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { schemaErrorEntry.Object });
    }

    /// <summary>
    /// Sets up <see cref="IPersistedQueryValidationError"/> on the given mock,
    /// with a "test-client" client and a single query ("abc123" / "Query failed.").
    /// This variant is used in Fusion and Schemas test bases.
    /// </summary>
    public static void SetupPersistedQueryValidationError<T>(Mock<T> mock) where T : class
    {
        var pqClient = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(
            MockBehavior.Strict);
        pqClient.SetupGet(x => x.Name).Returns("test-client");
        pqClient.SetupGet(x => x.Id).Returns("client-1");

        var pqQuery = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        pqQuery.SetupGet(x => x.Hash).Returns("abc123");
        pqQuery.SetupGet(x => x.Message).Returns("Query failed.");
        pqQuery.SetupGet(x => x.DeployedTags).Returns(Array.Empty<string>());
        pqQuery.SetupGet(x => x.Errors).Returns(Array
            .Empty<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>());

        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Message)
            .Returns("Persisted query validation failed.");
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Client)
            .Returns(pqClient.Object);
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Queries)
            .Returns(new[] { pqQuery.Object });
    }

    /// <summary>
    /// Sets up <see cref="ISchemaVersionChangeViolationError"/> on the given mock,
    /// with an empty changes list.
    /// </summary>
    public static void SetupSchemaChangeViolationError<T>(Mock<T> mock) where T : class
    {
        mock.As<ISchemaVersionChangeViolationError>()
            .SetupGet(x => x.__typename)
            .Returns("SchemaVersionChangeViolationError");
        mock.As<ISchemaVersionChangeViolationError>()
            .SetupGet(x => x.Changes)
            .Returns(Array
                .Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>());
    }

    /// <summary>
    /// Sets up <see cref="IUnexpectedProcessingError"/> on the given mock.
    /// </summary>
    public static void SetupUnexpectedProcessingError<T>(
        Mock<T> mock,
        string message = "An unexpected error occurred.") where T : class
    {
        mock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.__typename)
            .Returns("UnexpectedProcessingError");
        mock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns(message);
    }

    /// <summary>
    /// Sets up <see cref="IPersistedQueryValidationError"/> on the given mock,
    /// with no client and a single query that has a query-level error.
    /// This variant is used in the Clients test base.
    /// </summary>
    public static void SetupClientPersistedQueryValidationError<T>(Mock<T> mock) where T : class
    {
        var queryErrorMock = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>(
            MockBehavior.Strict);
        queryErrorMock.SetupGet(x => x.Message).Returns("Field 'foo' does not exist.");
        queryErrorMock.SetupGet(x => x.Code).Returns("FIELD_NOT_FOUND");
        queryErrorMock.SetupGet(x => x.Path).Returns((string?)null);
        queryErrorMock.SetupGet(x => x.Locations)
            .Returns((IReadOnlyList<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>?)null);

        var queryMock = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        queryMock.SetupGet(x => x.Message).Returns("Query abc123 is invalid.");
        queryMock.SetupGet(x => x.Hash).Returns("abc123");
        queryMock.SetupGet(x => x.DeployedTags).Returns(new List<string>());
        queryMock.SetupGet(x => x.Errors).Returns(new[] { queryErrorMock.Object });

        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Message)
            .Returns("Validation failed for persisted queries.");
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Client)
            .Returns((IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client?)null);
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Queries)
            .Returns(new[] { queryMock.Object });
    }

    /// <summary>
    /// Sets up <see cref="ISchemaDeployment"/> on the given mock with a single
    /// <see cref="IInvalidGraphQLSchemaError"/> deployment error.
    /// Used for schema version publish wait-for-approval events.
    /// </summary>
    public static void SetupSchemaDeploymentWithInvalidGraphQLSchemaError<T>(Mock<T> mock) where T : class
    {
        var schemaErrorEntry = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(
            MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message).Returns("Field 'Query.foo' has no type.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_ERROR");

        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        errorMock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.__typename)
            .Returns("InvalidGraphQLSchemaError");
        errorMock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Message)
            .Returns("Invalid GraphQL schema.");
        errorMock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { schemaErrorEntry.Object });

        mock.As<ISchemaDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });
    }

    /// <summary>
    /// Sets up <see cref="IClientDeployment"/> on the given mock with a single
    /// <see cref="IPersistedQueryValidationError"/> deployment error (client variant).
    /// Used for client version publish wait-for-approval events.
    /// </summary>
    public static void SetupClientDeploymentWithPersistedQueryValidationError<T>(Mock<T> mock) where T : class
    {
        var deploymentErrorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors>(
            MockBehavior.Strict);
        SetupClientPersistedQueryValidationError(deploymentErrorMock);

        mock.As<IClientDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { deploymentErrorMock.Object });
    }

    /// <summary>
    /// Sets up <see cref="IOpenApiCollectionDeployment"/> on the given mock with a single
    /// <see cref="IOpenApiCollectionValidationError"/> deployment error.
    /// Used for OpenAPI collection publish wait-for-approval events.
    /// </summary>
    public static void SetupOpenApiCollectionDeploymentWithValidationError<T>(Mock<T> mock) where T : class
    {
        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_3>(
            MockBehavior.Strict);
        SetupOpenApiCollectionValidationError(errorMock);

        mock.As<IOpenApiCollectionDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });
    }

    /// <summary>
    /// Sets up <see cref="IMcpFeatureCollectionDeployment"/> on the given mock with a single
    /// <see cref="IMcpFeatureCollectionValidationError"/> deployment error.
    /// Used for MCP feature collection publish wait-for-approval events.
    /// </summary>
    public static void SetupMcpFeatureCollectionDeploymentWithValidationError<T>(
        Mock<T> mock,
        string docErrorMessage = "Invalid tool definition.") where T : class
    {
        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_2>(
            MockBehavior.Strict);
        SetupMcpFeatureCollectionValidationError(errorMock, docErrorMessage);

        mock.As<IMcpFeatureCollectionDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });
    }

    /// <summary>
    /// Sets up <see cref="IFusionConfigurationDeployment"/> on the given mock with a single
    /// <see cref="IOpenApiCollectionValidationError"/> deployment error.
    /// Used for Fusion publish wait-for-approval events.
    /// </summary>
    public static void SetupFusionDeploymentWithOpenApiCollectionValidationError<T>(Mock<T> mock) where T : class
    {
        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_OpenApiCollectionValidationError>(
            MockBehavior.Strict);
        SetupOpenApiCollectionValidationError(errorMock);

        mock.As<IFusionConfigurationDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1[]
                { errorMock.Object });
    }
}
