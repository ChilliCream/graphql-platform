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

    protected void SetupRequestDeploymentSlotMutation()
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
            .ReturnsAsync(CreateRequestDeploymentSlotPayload);
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

    protected void SetupClaimDeploymentSlotMutation()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ClaimDeploymentSlotAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClaimDeploymentSlotPayload);
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

    protected void SetupFusionConfigurationValidationMutation()
    {
        FusionConfigurationClientMock
            .Setup(x => x.ValidateFusionConfigurationPublishAsync(
                RequestId,
                // TODO: This needs to be properly asserted
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidateFusionConfigurationPublishPayload);
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

    protected void SetupFusionConfigurationUploadMutation()
    {
        FusionConfigurationClientMock
            .Setup(x => x.CommitFusionArchiveAsync(
                RequestId,
                // TODO: This needs to be properly asserted
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CraeteCommitFusionArchivePayload);
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

    private static IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish CreateRequestDeploymentSlotPayload()
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);

        payload.SetupGet(x => x.RequestId).Returns(RequestId);

        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);

        return payload.Object;
    }

    private static IStartFusionConfigurationPublish_StartFusionConfigurationComposition CreateClaimDeploymentSlotPayload()
    {
        var payload = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors>?)null);

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

    private IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition CreateValidateFusionConfigurationPublishPayload()
    {
        var payload = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors>?)null);

        return payload.Object;
    }

    private ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish CraeteCommitFusionArchivePayload()
    {
        var payload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);

        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors>?)null);

        return payload.Object;
    }
}
