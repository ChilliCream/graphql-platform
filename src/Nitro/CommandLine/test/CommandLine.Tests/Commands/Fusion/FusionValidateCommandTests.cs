using System.IO.Compression;
using System.Text;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionValidateCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultArchiveFile = "fusion.far";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate the composed GraphQL schema of a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task BothArchiveAndSourceFiles_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddApiKey()
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile,
                "--source-schema-file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            You can only specify one of: '--source-schema-file' or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NeitherArchiveNorSourceFiles_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddApiKey()
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            You need to specify one of: '--source-schema-file' or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
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
        var client = CreateValidationExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
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
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException());
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
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
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException());
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
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

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsNullRequestId_ReturnsError(InteractionMode mode)
    {
        // arrange
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationInProgress(),
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   ├── Validation request created (ID: request-1)
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Fusion configuration is valid.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationInProgress(),
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationInProgress(),
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "isValid": true
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   ├── Validation request created (ID: request-1)
            │   ├── Validating...
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Fusion configuration validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Subscription_FailedWithError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Fusion configuration validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_ReturnSuccess_When_SourceSchemaFilesProvided()
    {
        // arrange
        const string sourceSchemaFile = "source-schema-1.graphqls";
        const string settingsFile = "source-schema-1-settings.json";
        const string settingsJson = """{"name":"Schema1","transports":{"http":{"url":"http://localhost/graphql"}}}""";
        const string schemaText = "schema { query: Query } type Query { hello: String }";

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists(sourceSchemaFile)).Returns(false);
        fileSystem.Setup(x => x.FileExists(sourceSchemaFile)).Returns(true);
        fileSystem.Setup(x => x.FileExists(settingsFile)).Returns(true);
        fileSystem.Setup(x => x.ReadAllBytesAsync(settingsFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(settingsJson));
        fileSystem.Setup(x => x.ReadAllTextAsync(sourceSchemaFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaText);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);
        client.Setup(x => x.ValidateSchemaVersionAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload());
        client.Setup(x => x.SubscribeToSchemaVersionValidationUpdatedAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(
                    new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
                    {
                        CreateValidationSuccess()
                    },
                    ct));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--source-schema-file",
                sourceSchemaFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_HandleLegacyFormat_When_FgpExtension()
    {
        // arrange
        const string archiveFile = "fusion.fgp";

        var fgpStream = new MemoryStream();
        await using (var package = FusionGraphPackage.Open(fgpStream, FileAccess.ReadWrite))
        {
            await package.SetSchemaAsync(
                HotChocolate.Language.Utf8GraphQLParser.Parse("type Query { hello: String }"));
        }

        fgpStream.Position = 0;

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(archiveFile))
            .Returns(fgpStream);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateSchemaVersionAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload());
        client.Setup(x => x.SubscribeToSchemaVersionValidationUpdatedAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(
                    new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
                    {
                        CreateValidationSuccess()
                    },
                    ct));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                archiveFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_UnknownSubscriptionEvent()
    {
        // arrange
        var unknownEvent = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "validate",
                "--api-id",
                DefaultApiId,
                "--stage",
                DefaultStage,
                "--archive",
                DefaultArchiveFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'production' of API 'api-1'
            ├── Validating against stage
            │   ├── Validation request created (ID: request-1)
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Failed to validate against stage.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static Stream CreateFarArchiveStream()
    {
        var memoryStream = new MemoryStream();
        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // archive-metadata.json is required for IsFarFormat detection and metadata parsing
            var metadataEntry = zip.CreateEntry("archive-metadata.json");
            using (var writer = new StreamWriter(metadataEntry.Open()))
            {
                writer.Write("""{"formatVersion":"1.0.0","supportedGatewayFormats":["1.0"],"sourceSchemas":[]}""");
            }

            // gateway/1.0/gateway.graphqls is the schema file
            var schemaEntry = zip.CreateEntry("gateway/1.0/gateway.graphqls");
            using (var writer = new StreamWriter(schemaEntry.Open()))
            {
                writer.Write("type Query { hello: String }");
            }

            // gateway/1.0/gateway-settings.json is the settings file
            var settingsEntry = zip.CreateEntry("gateway/1.0/gateway-settings.json");
            using (var writer = new StreamWriter(settingsEntry.Open()))
            {
                writer.Write("{}");
            }
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static Mock<IFileSystem> CreateArchiveFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(DefaultArchiveFile))
            .Returns(CreateFarArchiveStream());
        return fileSystem;
    }

    private static IValidateSchemaVersion_ValidateSchema CreateSuccessPayload()
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IValidateSchemaVersion_ValidateSchema CreateValidationPayloadWithErrors(
        params IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateValidationSetup(IValidateSchemaVersion_ValidateSchema payload)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateSchemaVersionAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateArchiveFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateValidationSetupWithSubscription(
            IValidateSchemaVersion_ValidateSchema mutationPayload,
            IEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate> subscriptionEvents)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateSchemaVersionAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToSchemaVersionValidationUpdatedAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateArchiveFileSystem();

        return (client, fileSystem);
    }

    private static Mock<IFusionConfigurationClient> CreateValidationExceptionClient(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateSchemaVersionAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateOperationInProgress()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationInProgress()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationSuccess()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess(
            "SchemaVersionValidationSuccess",
            ProcessingState.Success,
            Array.Empty<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Changes>());
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationFailed(
        params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[] errors)
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed(
            "SchemaVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            """
            Not authorized to validate.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError(
                "ApiNotFoundError",
                "API not found.",
                DefaultApiId),
            """
            API not found.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError(
                "Schema not found.",
                DefaultApiId,
                "v1"),
            """
            Schema not found.
            """
        ];

        var unexpectedError = new Mock<IValidateSchemaVersion_ValidateSchema_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        yield return
        [
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """
        ];
    }
}
