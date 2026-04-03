using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using HotChocolate.Fusion;
using Moq;
using Moq.Language;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public abstract class FusionCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string ApiId = "api-1";
    protected const string Stage = "dev";
    protected const string Tag = "v1";
    protected const string ArchiveFile = "fusion.far";
    protected const string SourceSchemaFile = "products/schema.graphqls";
    protected const string SourceSchemaSettingsFile = "products/schema-settings.json";
    protected const string SourceSchema = "products";
    protected const string RequestId = "request-id";
    private const string SourceSchemaText =
        """
        type Query {
          field: String!
        }
        """;
    private const string SourceSchemaSettings =
        $$"""
        {
          "name": "{{SourceSchema}}"
        }
        """;

    protected void SetupSourceSchemaDownload()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadSourceSchemaArchiveAsync(
                ApiId,
                SourceSchema,
                Tag,
                It.IsAny<CancellationToken>()))
            .Returns(async () => await CreateSourceSchemaArchiveStreamAsync());
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
        var stream = new MemoryStream();

        SetupFile(ArchiveFile, stream);
    }

    protected void SetupSourceSchemaFile()
    {
        SetupFile(SourceSchemaFile, SourceSchemaText);

        SetupFile(SourceSchemaSettingsFile, SourceSchemaSettings);
    }

    protected void SetupRequestDeploymentSlotMutation(
        bool waitForApproval = false,
        params IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.RequestDeploymentSlotAsync(
                ApiId,
                Stage,
                Tag,
                null,
                null,
                It.IsAny<SourceSchemaVersion[]>(),
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

    protected void SetupFusionConfigurationDownload()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                ApiId,
                Stage,
                WellKnownVersions.LatestGatewayFormatVersion.ToString(),
            It.IsAny<CancellationToken>()))
            .Returns(async () => await CreateFusionAsyncStreamAsync());
    }

    protected void SetupFusionConfigurationValidationMutation(
        params IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.ValidateFusionConfigurationPublishAsync(
                RequestId,
                // TODO: This needs to be properly asserted
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateValidateFusionConfigurationPublishPayload(errors));
    }

    protected void SetupFusionConfigurationValidationSubscription(
        params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] events)
    {
        if (events.Length == 0)
        {
            events = [CreateValidationSuccessEvent()];
        }

        SetupPublishingTaskSubscription(events);
    }

    protected void SetupFusionConfigurationUploadMutation(
        params ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors[] errors)
    {
        FusionConfigurationClientMock
            .Setup(x => x.CommitFusionArchiveAsync(
                RequestId,
                // TODO: This needs to be properly asserted
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateCommitFusionArchivePayload(errors));
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

    protected void SetupFusionConfigurationDownloadException()
    {
        FusionConfigurationClientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                ApiId,
                Stage,
                WellKnownVersions.LatestGatewayFormatVersion.ToString(),
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

    private async Task<Stream?> CreateFusionAsyncStreamAsync()
    {
        return null;
    }

    private async Task<Stream> CreateSourceSchemaArchiveStreamAsync()
    {
        return await FusionSourceSchemaArchiveHelper.CreateArchiveStreamAsync(
            Encoding.UTF8.GetBytes(SourceSchemaText),
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

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged CreateReadyEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>(MockBehavior.Strict).Object;
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged CreateValidationSuccessEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationSuccess>(MockBehavior.Strict).Object;
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged CreatPublishSuccessEvent()
    {
        return new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>(MockBehavior.Strict).Object;
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
}
