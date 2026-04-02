using System.Runtime.CompilerServices;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionPublishCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultArchiveFile = "fusion.far";
    private const string DefaultRequestId = "request-123";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a Fusion configuration to a stage.

            Usage:
              nitro fusion publish [command] [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)                         The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -s, --source-schema <source-schema>            One or more source schemas that should be included in the composition. Source schemas can either be just a name ('example') or a name and a version ('example@1.0.0'). If no version is specified the value of the '--tag' option is taken as the source schema version.
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --wait-for-approval                            Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              -w, --working-directory <working-directory>    Set the working directory for the command
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information

            Commands:
              begin     Begin a configuration publish. This command will request a deployment slot.
              start     Start a Fusion configuration publish.
              validate  Validate a Fusion configuration against the schema and clients.
              cancel    Cancel a Fusion configuration publish.
              commit    Commit a Fusion configuration publish.

            Example:
              nitro fusion publish \
                --api-id "<api-id>" \
                --stage "dev" \
                --tag "v1" \
                --source-schema products \
                --source-schema reviews
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing one of the required options '--source-schema', '--source-schema-file', or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MultipleExclusiveOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile,
                "--source-schema-file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The options '--source-schema', '--source-schema-file', and '--archive' are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ArchiveFileDoesNotExist_ReturnsError_NonInteractive()
    {
        // arrange
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GetCurrentDirectory())
            .Returns("/tmp");
        fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
            .Returns(false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Archive file 'fusion.far' does not exist.
            """);

        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishExceptionSetup(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Publish_Should_UploadArchive_When_ArchiveFileProvided()
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishSuccessSetup(
            CreateReadyEvent(),
            CreateCommitSuccessEvent());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-123
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'production'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration to 'production'.
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Publish_Should_HandleSubscriptionEvents_When_Queued()
    {
        // arrange
        var queuedEvent = CreateQueuedEvent(3);
        var readyEvent = CreateReadyEvent();

        var (client, fileSystem) = CreateArchivePublishSuccessSetup(
            [queuedEvent, readyEvent],
            CreateCommitSuccessEvent());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-123
            │   ├── Queued at position 3.
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'production'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration to 'production'.
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Publish_Should_HandleSubscriptionEvents_When_PublishSucceeds()
    {
        // arrange
        var (client, fileSystem) = CreateArchivePublishSuccessSetup(
            CreateReadyEvent(),
            CreateCommitSuccessEvent());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-123
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'production'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration to 'production'.
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Publish_Should_HandleSubscriptionEvents_When_PublishFails()
    {
        // arrange
        var errorMock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors>(MockBehavior.Strict);
        errorMock.SetupGet(x => x.Message).Returns("Composition failed.");

        var failedEvent = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingFailed>(MockBehavior.Strict);
        failedEvent.As<IFusionConfigurationPublishingFailed>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });

        var (client, fileSystem) = CreateArchivePublishWithCommitEvents(
            CreateReadyEvent(),
            failedEvent.Object);

        var releasePayload = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition>(MockBehavior.Strict);
        releasePayload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors>?)null);
        client.Setup(x => x.ReleaseDeploymentSlotAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(releasePayload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--tag",
                DefaultTag,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'production' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-123
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'production'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Composition failed.
            The commit has failed.
            The commit has failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    // --- Helpers ---

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateReadyEvent()
    {
        return Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>();
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateCommitSuccessEvent()
    {
        return Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>();
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateQueuedEvent(int position)
    {
        var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsQueued>(MockBehavior.Strict);
        mock.As<IProcessingTaskIsQueued>()
            .SetupGet(x => x.QueuePosition)
            .Returns(position);
        return mock.Object;
    }

    private static Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>
        CreateSuccessPayload()
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);
        payload.SetupGet(x => x.RequestId).Returns(DefaultRequestId);
        return payload;
    }

    private static Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>
        CreateCommitPayload()
    {
        var payload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors>?)null);
        return payload;
    }

    private static Mock<IFileSystem> CreateArchiveFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GetCurrentDirectory())
            .Returns("/tmp");
        fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
            .Returns(true);
        fileSystem.Setup(x => x.OpenReadStream(DefaultArchiveFile))
            .Returns(new MemoryStream("archive-content"u8.ToArray()));
        return fileSystem;
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishSuccessSetup(
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged beginEvent,
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent)
    {
        return CreateArchivePublishSuccessSetup([beginEvent], commitEvent);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishSuccessSetup(
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] beginEvents,
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent)
    {
        return CreateArchivePublishWithCommitEvents(beginEvents, commitEvent, createCommitPayload: true);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishWithCommitEvents(
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged beginEvent,
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent,
            bool createCommitPayload = true)
    {
        return CreateArchivePublishWithCommitEvents([beginEvent], commitEvent, createCommitPayload);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishWithCommitEvents(
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] beginEvents,
            IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent,
            bool createCommitPayload = true)
    {
        var payload = CreateSuccessPayload();
        var claimPayload = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition>(MockBehavior.Strict);
        claimPayload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors>?)null);
        var commitPayloadMock = CreateCommitPayload();

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        var subscriptionCallCount = 0;
        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
            {
                var call = Interlocked.Increment(ref subscriptionCallCount);
                if (call == 1)
                {
                    return ToAsyncEnumerable(beginEvents, ct);
                }

                return ToAsyncEnumerable([commitEvent], ct);
            });

        client.Setup(x => x.ClaimDeploymentSlotAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(claimPayload.Object);

        client.Setup(x => x.CommitFusionArchiveAsync(
                DefaultRequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitPayloadMock.Object);

        var fileSystem = CreateArchiveFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateArchivePublishExceptionSetup(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateArchiveFileSystem();

        return (client, fileSystem);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
