using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public abstract class FusionCommandTestBase(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    protected const string ArchiveFile = "fusion.far";
    protected const string SourceSchemaFile = "products/schema.graphqls";
    protected const string SourceSchemaSettingsFile = "products/schema-settings.json";
    protected const string SourceSchema = "products";
    protected static readonly SourceSchemaVersion[] SourceSchemaVersions = [new(SourceSchema, Tag)];
    private const string SourceSchemaText =
        """
        type Query {
          field: String!
        }
        """;
    private const string InvalidSourceSchemaText =
        """
        type Query {
          field(arg: String @require(field: "non-existent")): String
        }
        """;
    private const string SourceSchemaSettings =
        $$"""
        {
          "name": "{{SourceSchema}}"
        }
        """;

    protected async Task<string> GetFusionSchemaAsync(MemoryStream stream)
    {
        using var archive = FusionArchive.Open(stream);

        var config = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion);
        Assert.NotNull(config);
        await using var schemaStream = await config.OpenReadSchemaAsync();
        using var reader = new StreamReader(schemaStream);
        return await reader.ReadToEndAsync();
    }

    protected void SetupSourceSchemaDownload(string version = Tag)
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadSourceSchemaArchiveAsync(
                ApiId,
                SourceSchema,
                version,
                It.IsAny<CancellationToken>()))
            .Returns(async () => await CreateSourceSchemaArchiveStreamAsync(SourceSchemaText));
    }

    protected void SetupSourceSchemaDownloadWithInvalidSchema()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadSourceSchemaArchiveAsync(
                ApiId,
                SourceSchema,
                Tag,
                It.IsAny<CancellationToken>()))
            .Returns(async () => await CreateSourceSchemaArchiveStreamAsync(InvalidSourceSchemaText));
    }

    protected void SetupMissingSourceSchemaDownload()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadSourceSchemaArchiveAsync(
                ApiId,
                SourceSchema,
                Tag,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
    }

    protected void SetupArchiveFile()
    {
        var stream = CreateFusionArchiveStream();

        SetupFile(ArchiveFile, stream);
    }

    protected void SetupSourceSchemaFileWithInvalidSchema()
    {
        SetupSourceSchemaFile(InvalidSourceSchemaText);
    }

    protected void SetupSourceSchemaFile(string? schemaText = null)
    {
        schemaText ??= SourceSchemaText;

        SetupFile(SourceSchemaFile, schemaText);

        SetupFile(SourceSchemaSettingsFile, SourceSchemaSettings);
    }

    protected void SetupRequestDeploymentSlotMutation(
        bool waitForApproval = false,
        string? subgraphId = null,
        string? subgraphName = null,
        SourceSchemaVersion[]? sourceSchemaVersions = null,
        params IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.RequestDeploymentSlotAsync(
                ApiId,
                Stage,
                Tag,
                subgraphId,
                subgraphName,
                sourceSchemaVersions,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateRequestDeploymentSlotPayload(errors));
    }

    protected void SetupRequestDeploymentSlotSubscription(
        params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateReadyEvent()];
        }

        SetupPublishingTaskSubscription(events);
    }

    protected void SetupClaimDeploymentSlotMutation(
        params IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.ClaimDeploymentSlotAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateClaimDeploymentSlotPayload(errors));
    }

    protected void SetupReleaseDeploymentSlotMutation(
        params ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.ReleaseDeploymentSlotAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateReleaseDeploymentSlotPayload(errors));
    }

    protected void SetupReleaseDeploymentSlotMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ReleaseDeploymentSlotAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupFusionConfigurationDownload(
        string version = "2.0.0",
        string archiveFormat = ArchiveFormats.Far)
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                ApiId,
                Stage,
                version,
                archiveFormat,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateFusionArchiveStream(archiveFormat));
    }

    protected void SetupMissingFusionConfigurationDownload(
        string version = "2.0.0",
        string archiveFormat = ArchiveFormats.Far)
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                ApiId,
                Stage,
                version,
                archiveFormat,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
    }

    protected void SetupFusionConfigurationDownloadException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                ApiId,
                Stage,
                WellKnownVersions.LatestGatewayFormatVersion.ToString(),
                ArchiveFormats.Far,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected CapturedUpload SetupFusionConfigurationValidationMutation(
        params IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors[] errors)
    {
        var archiveStream = new MemoryStream();

        FusionConfigurationClientMock
            .Setup(x => x.ValidateFusionConfigurationPublishAsync(
                RequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Stream, CancellationToken>((_, stream, _) =>
            {
                stream.CopyTo(archiveStream);
                archiveStream.Position = 0;
            })
            .ReturnsAsync(() => CreateValidateFusionConfigurationPublishPayload(errors));

        return new CapturedUpload(archiveStream);
    }

    protected void SetupFusionConfigurationValidationSubscription(
        params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateValidationInProgressEvent(), CreateValidationSuccessEvent()];
        }

        SetupPublishingTaskSubscription(events);
    }

    protected MemoryStream SetupFusionConfigurationUploadMutation(
        params ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors[] errors)
    {
        var archiveStream = new MemoryStream();

        FusionConfigurationClientMock
            .Setup(x => x.CommitFusionArchiveAsync(
                RequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Stream, CancellationToken>((_, stream, _) =>
            {
                stream.CopyTo(archiveStream);
                archiveStream.Position = 0;
            })
            .ReturnsAsync(() => CreateCommitFusionArchivePayload(errors));

        return archiveStream;
    }

    protected void SetupSourceSchemaDownloadException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadSourceSchemaArchiveAsync(
                ApiId,
                SourceSchema,
                Tag,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupRequestDeploymentSlotMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.RequestDeploymentSlotAsync(
                ApiId,
                Stage,
                Tag,
                null,
                null,
                It.IsAny<SourceSchemaVersion[]>(),
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupClaimDeploymentSlotMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ClaimDeploymentSlotAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupFusionConfigurationValidationMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ValidateFusionConfigurationPublishAsync(
                RequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupFusionConfigurationUploadMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.CommitFusionArchiveAsync(
                RequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupFusionConfigurationUploadSubscription(
        params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        if (events.Length == 0)
        {
            events = [CreatPublishSuccessEvent()];
        }

        SetupPublishingTaskSubscription(events);
    }

    protected MemoryStream SetupUploadSourceSchemaMutation(
        params IUploadFusionSubgraph_UploadFusionSubgraph_Errors[] errors)
    {
        var capturedStream = new MemoryStream();

        FusionConfigurationClientMock
            .Setup(x => x.UploadFusionSubgraphAsync(
                ApiId,
                Tag,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, SourceMetadata, CancellationToken>(
                (_, _, stream, _, _) =>
                {
                    stream.CopyTo(capturedStream);
                    capturedStream.Position = 0;
                })
            .ReturnsAsync(() => CreateUploadSourceSchemaPayload(errors));

        return capturedStream;
    }

    protected static async Task AssertFusionSourceSchemaArchive(Stream stream)
    {
        using var archive = FusionSourceSchemaArchive.Open(stream, leaveOpen: true);

        var settings = await archive.TryGetSettingsAsync();
        Assert.NotNull(settings);
        Assert.Equal(SourceSchemaSettings, settings.RootElement.ToString());

        var schema = await archive.TryGetSchemaAsync();
        Assert.True(schema.HasValue);
        Assert.Equal(SourceSchemaText, Encoding.UTF8.GetString(schema.Value.Span));
    }

    protected void SetupUploadSourceSchemaMutationException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.UploadFusionSubgraphAsync(
                ApiId,
                Tag,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #region Subscription Event Factories

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateReadyEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateQueuedEvent(int queuePosition = 1)
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsQueued>(MockBehavior.Strict);
        mock.SetupGet(x => x.QueuePosition).Returns(queuePosition);
        return mock.Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateValidationInProgressEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ValidationInProgress>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateOperationInProgressEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_OperationInProgress>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateProcessingTaskApprovedEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskApproved>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreatePublishingSuccessEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateValidationSuccessEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationSuccess>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreatPublishSuccessEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>(MockBehavior.Strict).Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreatePublishingFailedEvent(
            params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors[] errors)
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingFailed>(MockBehavior.Strict);
        mock.SetupGet(x => x.Errors).Returns(errors);
        return mock.Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreatePublishingFailedEventWithErrors()
    {
        var schemaErrorEntry = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message).Returns("Field 'Query.foo' has no type.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_ERROR");

        return CreatePublishingFailedEvent(
            CreatePublishingInvalidGraphQLSchemaError("Invalid GraphQL schema.", schemaErrorEntry.Object),
            CreatePublishingGenericError("An error occurred."));
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateValidationFailedEventWithErrors()
    {
        // 1. SchemaVersionChangeViolationError (empty changes list)
        var schemaViolation = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_SchemaVersionChangeViolationError>(MockBehavior.Strict);
        MockErrorFactory.SetupSchemaChangeViolationError(schemaViolation);

        // 2. InvalidGraphQLSchemaError with one error entry
        var graphqlError = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_InvalidGraphQLSchemaError_1>(MockBehavior.Strict);
        MockErrorFactory.SetupInvalidGraphQLSchemaError(graphqlError);

        // 3. PersistedQueryValidationError with a client and query
        var pqError = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_PersistedQueryValidationError>(MockBehavior.Strict);
        MockErrorFactory.SetupPersistedQueryValidationError(pqError);

        // 4. OpenApiCollectionValidationError with an endpoint entity
        var openApiError = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_OpenApiCollectionValidationError>(MockBehavior.Strict);
        MockErrorFactory.SetupOpenApiCollectionValidationError(openApiError);

        // 5. McpFeatureCollectionValidationError with a tool entity
        var mcpError = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_McpFeatureCollectionValidationError>(MockBehavior.Strict);
        MockErrorFactory.SetupMcpFeatureCollectionValidationError(mcpError);

        // 6. UnexpectedProcessingError
        var unexpectedError = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_UnexpectedProcessingError_1>(MockBehavior.Strict);
        MockErrorFactory.SetupUnexpectedProcessingError(unexpectedError);

        // Assemble the event with all errors
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationFailed>(MockBehavior.Strict);
        mock.SetupGet(x => x.Errors).Returns(new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_1[]
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

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateWaitForApprovalEvent(
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment? deployment = null)
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_WaitForApproval>(MockBehavior.Strict);
        mock.SetupGet(x => x.Deployment).Returns(deployment);
        return mock.Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateWaitForApprovalEventWithErrors()
    {
        var deploymentMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_FusionConfigurationDeployment>(MockBehavior.Strict);
        MockErrorFactory.SetupFusionDeploymentWithOpenApiCollectionValidationError(deploymentMock);

        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_WaitForApproval>(MockBehavior.Strict);
        mock.SetupGet(x => x.Deployment).Returns(deploymentMock.Object);
        return mock.Object;
    }

    #endregion

    #region Subscription Event Error Factories — Publishing Failed

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors
        CreatePublishingInvalidGraphQLSchemaError(
            string message = "Invalid GraphQL schema.",
            params IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors[] errors)
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_InvalidGraphQLSchemaError>(MockBehavior.Strict);
        mock.As<IInvalidGraphQLSchemaError>().SetupGet(x => x.__typename).Returns("InvalidGraphQLSchemaError");
        mock.As<IInvalidGraphQLSchemaError>().SetupGet(x => x.Message).Returns(message);
        mock.As<IInvalidGraphQLSchemaError>().SetupGet(x => x.Errors).Returns(errors);
        mock.As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors>()
            .SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors
        CreatePublishingGenericError(string message = "An error occurred.")
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    private ISetupSequentialResult<IAsyncEnumerable<
        IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>>? _setup;

    private void SetupPublishingTaskSubscription(
        IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        _setup ??= FusionConfigurationClientMock
            .SetupSequence(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                RequestId,
                It.IsAny<CancellationToken>()));

        _setup.Returns(events.ToAsyncEnumerable());
    }

    private static MemoryStream CreateFusionArchiveStream(
        string archiveFormat = ArchiveFormats.Far)
    {
        var extension = archiveFormat switch
        {
            ArchiveFormats.Far => ".far",
            ArchiveFormats.Fgp => ".fgp",
            _ => throw new ArgumentException($"Unknown archive format: {archiveFormat}")
        };

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "__resources__",
            "fusion-archives",
            $"gateway{extension}");

        var memoryStream = new MemoryStream();
        using var fileStream = File.OpenRead(path);
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<Stream> CreateSourceSchemaArchiveStreamAsync(
        string schema)
    {
        return await FusionSourceSchemaArchiveHelper.CreateArchiveStreamAsync(
            Encoding.UTF8.GetBytes(schema),
            JsonDocument.Parse(SourceSchemaSettings));
    }

    private static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish
        CreateRequestDeploymentSlotPayload(
            IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors[] errors)
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);

        payload.SetupGet(x => x.RequestId).Returns(RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IStartFusionConfigurationPublish_StartFusionConfigurationComposition
        CreateClaimDeploymentSlotPayload(
            IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors[] errors)
    {
        var payload = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition CreateReleaseDeploymentSlotPayload(
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors[] errors)
    {
        var payload = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition
        CreateValidateFusionConfigurationPublishPayload(
            IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors[] errors)
    {
        var payload = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish
        CreateCommitFusionArchivePayload(
            ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors[] errors)
    {
        var payload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns(errors.Length > 0 ? errors : null);

        return payload.Object;
    }

    private static IUploadFusionSubgraph_UploadFusionSubgraph
        CreateUploadSourceSchemaPayload(
            IUploadFusionSubgraph_UploadFusionSubgraph_Errors[] errors)
    {
        var payload = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Errors).Returns(errors);
            payload.SetupGet(x => x.FusionSubgraphVersion)
                .Returns((IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion?)null);
        }
        else
        {
            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);

            var version = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion>(MockBehavior.Strict);
            version.SetupGet(x => x.Id).Returns("fsv-1");
            payload.SetupGet(x => x.FusionSubgraphVersion).Returns(version.Object);
        }

        return payload.Object;
    }

    #region Error Factories — UploadSourceSchema

    protected static IUploadFusionSubgraph_UploadFusionSubgraph_Errors
        CreateUploadSourceSchemaUnauthorizedError(string message = "Not authorized to upload.")
    {
        var mock = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUploadFusionSubgraph_UploadFusionSubgraph_Errors
        CreateUploadSourceSchemaDuplicatedTagError(string message = "Tag already exists.")
    {
        var mock = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_DuplicatedTagError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUploadFusionSubgraph_UploadFusionSubgraph_Errors
        CreateUploadSourceSchemaConcurrentOperationError(string message = "A concurrent operation is in progress.")
    {
        var mock = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_ConcurrentOperationError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IUploadFusionSubgraph_UploadFusionSubgraph_Errors
        CreateUploadSourceSchemaInvalidArchiveError(string message = "The archive is invalid.")
    {
        var mock = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_Errors_InvalidFusionSourceSchemaArchiveError>(MockBehavior.Strict);
        mock.As<IInvalidFusionSourceSchemaArchiveError>().SetupGet(x => x.Message).Returns(message);
        mock.As<IError>().SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Error Factories — RequestDeploymentSlot (Begin)

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotApiNotFoundError(string apiId = ApiId)
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_ApiNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"API '{apiId}' was not found.");
        mock.SetupGet(x => x.ApiId).Returns(apiId);
        return mock.Object;
    }

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotStageNotFoundError(string name = Stage)
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_StageNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns($"Stage '{name}' was not found.");
        mock.SetupGet(x => x.Name).Returns(name);
        return mock.Object;
    }

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotSubgraphInvalidError(string message = "Subgraph is invalid.")
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_SubgraphInvalidError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotInvalidStateTransitionError(string message = "Invalid processing state transition.")
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_InvalidProcessingStateTransitionError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors
        CreateRequestDeploymentSlotInvalidSourceMetadataError(string message = "Invalid source metadata input.")
    {
        var mock = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_InvalidSourceMetadataInputError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Error Factories — ClaimDeploymentSlot (Start)

    protected static IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors
        CreateClaimDeploymentSlotUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors
        CreateClaimDeploymentSlotRequestNotFoundError(string message = "Fusion configuration request was not found.")
    {
        var mock = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors
        CreateClaimDeploymentSlotInvalidStateTransitionError(string message = "Invalid processing state transition.")
    {
        var mock = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors_InvalidProcessingStateTransitionError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Error Factories — ReleaseDeploymentSlot (Cancel)

    protected static ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors
        CreateReleaseDeploymentSlotUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors
        CreateReleaseDeploymentSlotRequestNotFoundError(string message = "Fusion configuration request was not found.")
    {
        var mock = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors
        CreateReleaseDeploymentSlotInvalidStateTransitionError(string message = "Invalid processing state transition.")
    {
        var mock = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors_InvalidProcessingStateTransitionError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Error Factories — ValidateFusionConfiguration

    protected static IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors
        CreateValidationUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors
        CreateValidationRequestNotFoundError(string message = "Fusion configuration request was not found.")
    {
        var mock = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors
        CreateValidationInvalidStateTransitionError(string message = "Invalid processing state transition.")
    {
        var mock = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_InvalidProcessingStateTransitionError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    #region Error Factories — CommitFusionArchive (Upload)

    protected static ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors
        CreateUploadUnauthorizedError(string message = "Unauthorized.")
    {
        var mock = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors
        CreateUploadRequestNotFoundError(string message = "Fusion configuration request was not found.")
    {
        var mock = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    protected static ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors
        CreateUploadInvalidStateTransitionError(string message = "Invalid processing state transition.")
    {
        var mock = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors_InvalidProcessingStateTransitionError>(MockBehavior.Strict);
        mock.SetupGet(x => x.Message).Returns(message);
        return mock.Object;
    }

    #endregion

    protected sealed class CapturedUpload(MemoryStream stream) : IDisposable
    {
        public FusionArchive GetArchive()
            => FusionArchive.Open(stream, leaveOpen: true);

        public void Dispose() => stream.Dispose();
    }
}
