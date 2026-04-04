using ChilliCream.Nitro.Client;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public abstract class SchemaCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ApiId = "api-1";
    protected const string Stage = "dev";
    protected const string RequestId = "request-id";

    protected void SetupValidateSchemaVersionMutation(
        params IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.ValidateSchemaVersionAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateValidateSchemaVersionPayload(errors));
    }

    protected void SetupValidateSchemaVersionMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ValidateSchemaVersionAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupSchemaVersionValidationSubscription(
        params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateSchemaVersionValidationSuccessEvent()];
        }

        SetupSchemaVersionValidationUpdatedSubscription(events);
    }

    #region Subscription Event Factories — Schema Version Validation

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationSuccessEvent()
    {
        return new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess>(MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationFailedEvent(
            params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[] errors)
    {
        var mock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed>(MockBehavior.Strict);
        mock.SetupGet(x => x.Errors).Returns(errors);
        return mock.Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionOperationInProgressEvent()
    {
        return new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_OperationInProgress>(MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationInProgressEvent()
    {
        return new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress>(MockBehavior.Strict).Object;
    }

    protected static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate
        CreateSchemaVersionValidationFailedEventWithErrors()
    {
        // 1. SchemaVersionChangeViolationError (empty changes list)
        var schemaViolation = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_SchemaVersionChangeViolationError>(MockBehavior.Strict);
        schemaViolation.SetupGet(x => x.__typename).Returns("SchemaVersionChangeViolationError");
        schemaViolation.SetupGet(x => x.Changes).Returns(Array.Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>());

        // 2. InvalidGraphQLSchemaError with one error entry
        var schemaErrorEntry = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message).Returns("Field 'Query.foo' has no type.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_ERROR");

        var graphqlError = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_InvalidGraphQLSchemaError>(MockBehavior.Strict);
        graphqlError.SetupGet(x => x.__typename).Returns("InvalidGraphQLSchemaError");
        graphqlError.SetupGet(x => x.Message).Returns("Invalid GraphQL schema.");
        graphqlError.SetupGet(x => x.Errors).Returns(new[] { schemaErrorEntry.Object });

        // 3. PersistedQueryValidationError with a client and query
        var pqClient = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(MockBehavior.Strict);
        pqClient.SetupGet(x => x.Name).Returns("test-client");
        pqClient.SetupGet(x => x.Id).Returns("client-1");

        var pqQuery = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(MockBehavior.Strict);
        pqQuery.SetupGet(x => x.Hash).Returns("abc123");
        pqQuery.SetupGet(x => x.Message).Returns("Query failed.");
        pqQuery.SetupGet(x => x.DeployedTags).Returns(Array.Empty<string>());
        pqQuery.SetupGet(x => x.Errors).Returns(Array.Empty<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>());

        var pqError = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_PersistedQueryValidationError>(MockBehavior.Strict);
        pqError.SetupGet(x => x.Message).Returns("Persisted query validation failed.");
        pqError.SetupGet(x => x.Client).Returns(pqClient.Object);
        pqError.SetupGet(x => x.Queries).Returns(new[] { pqQuery.Object });

        // 4. OpenApiCollectionValidationError with an endpoint entity
        var openApiLocation = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations_1>(MockBehavior.Strict);
        openApiLocation.SetupGet(x => x.Line).Returns(10);
        openApiLocation.SetupGet(x => x.Column).Returns(5);

        var openApiDocError = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationDocumentError>(MockBehavior.Strict);
        openApiDocError.SetupGet(x => x.Code).Returns("INVALID");
        openApiDocError.SetupGet(x => x.Message).Returns("Invalid schema.");
        openApiDocError.SetupGet(x => x.Path).Returns("/paths/~1pets");
        openApiDocError.SetupGet(x => x.Locations).Returns(new[] { openApiLocation.Object });

        var openApiEndpoint = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationEndpoint>(MockBehavior.Strict);
        openApiEndpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.HttpMethod).Returns("GET");
        openApiEndpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.Route).Returns("/pets");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_1[] openApiEntityErrors = [openApiDocError.Object];
        openApiEndpoint.As<IOpenApiCollectionValidationEntity_OpenApiCollectionValidationEndpoint>().SetupGet(x => x.Errors).Returns(openApiEntityErrors);
        openApiEndpoint.As<IOpenApiCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(openApiEntityErrors);

        var openApiCol = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollection>(MockBehavior.Strict);
        openApiCol.SetupGet(x => x.Name).Returns("petstore");
        openApiCol.SetupGet(x => x.Id).Returns("collection-1");

        var openApiColValidation = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollectionValidationCollection>(MockBehavior.Strict);
        openApiColValidation.SetupGet(x => x.OpenApiCollection).Returns(openApiCol.Object);
        openApiColValidation.SetupGet(x => x.Entities).Returns(new[] { openApiEndpoint.Object });

        var openApiError = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_OpenApiCollectionValidationError>(MockBehavior.Strict);
        openApiError.SetupGet(x => x.Collections).Returns(new[] { openApiColValidation.Object });

        // 5. McpFeatureCollectionValidationError with a tool entity
        var mcpDocError = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError>(MockBehavior.Strict);
        mcpDocError.SetupGet(x => x.Code).Returns("INVALID");
        mcpDocError.SetupGet(x => x.Message).Returns("Invalid MCP schema.");
        mcpDocError.SetupGet(x => x.Path).Returns("/tools/test");
        var mcpLocation = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>(MockBehavior.Strict);
        mcpLocation.SetupGet(x => x.Line).Returns(5);
        mcpLocation.SetupGet(x => x.Column).Returns(3);
        mcpDocError.SetupGet(x => x.Locations).Returns(new[] { mcpLocation.Object });

        var mcpTool = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool>(MockBehavior.Strict);
        mcpTool.As<IMcpFeatureCollectionValidationTool>().SetupGet(x => x.Name).Returns("test-tool");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors[] mcpEntityErrors = [mcpDocError.Object];
        mcpTool.As<IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool>().SetupGet(x => x.Errors).Returns(mcpEntityErrors);
        mcpTool.As<IMcpFeatureCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(mcpEntityErrors);

        var mcpCol = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollection>(MockBehavior.Strict);
        mcpCol.SetupGet(x => x.Name).Returns("mcp-collection");
        mcpCol.SetupGet(x => x.Id).Returns("mcp-1");

        var mcpColValidation = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollectionValidationCollection>(MockBehavior.Strict);
        mcpColValidation.SetupGet(x => x.McpFeatureCollection).Returns(mcpCol.Object);
        mcpColValidation.SetupGet(x => x.Entities).Returns(new[] { mcpTool.Object });

        var mcpError = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_McpFeatureCollectionValidationError>(MockBehavior.Strict);
        mcpError.SetupGet(x => x.Collections).Returns(new[] { mcpColValidation.Object });

        // 6. UnexpectedProcessingError
        var unexpectedError = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_UnexpectedProcessingError>(MockBehavior.Strict);
        unexpectedError.SetupGet(x => x.__typename).Returns("UnexpectedProcessingError");
        unexpectedError.SetupGet(x => x.Message).Returns("An unexpected error occurred.");

        // Assemble the event with all errors
        var mock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed>(MockBehavior.Strict);
        mock.SetupGet(x => x.Errors).Returns(new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[]
        {
            schemaViolation.Object,
            graphqlError.Object,
            pqError.Object,
            openApiError.Object,
            mcpError.Object,
            unexpectedError.Object
        });
        return mock.Object;
    }

    #endregion

    #region Error Factories — ValidateSchemaVersion

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionApiNotFoundError(string apiId = ApiId)
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"API '{apiId}' was not found.");
        mock.SetupGet(x => x.ApiId).Returns(apiId);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IValidateSchemaVersion_ValidateSchema_Errors
        CreateValidateSchemaVersionSchemaNotFoundError(string message = "Schema not found.")
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>>? _schemaValidationSetup;

    private void SetupSchemaVersionValidationUpdatedSubscription(
        IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[] events)
    {
        _schemaValidationSetup ??= FusionConfigurationClientMock
            .SetupSequence(x => x.SubscribeToSchemaVersionValidationUpdatedAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _schemaValidationSetup.Returns(events.ToAsyncEnumerable());
    }

    private static IValidateSchemaVersion_ValidateSchema
        CreateValidateSchemaVersionPayload(
            IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);

        payload.SetupGet(x => x.Id)
            .Returns(errors.Length > 0 ? null : RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }
}
