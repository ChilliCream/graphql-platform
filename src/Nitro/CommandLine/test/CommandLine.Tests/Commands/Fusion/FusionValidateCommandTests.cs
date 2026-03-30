using System.IO.Compression;
using System.Text;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionValidateCommandTests
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultArchiveFile = "fusion.far";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validates the composed GraphQL schema of a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file.
              --cloud-url <cloud-url>                        The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
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
        var result = await new CommandBuilder()
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
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Validates the composed GraphQL schema of a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file.
              --cloud-url <cloud-url>                        The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information
            """);
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
        var result = await new CommandBuilder()
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
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Validates the composed GraphQL schema of a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file.
              --cloud-url <cloud-url>                        The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information
            """);
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
            new NitroClientException("validation request failed"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: validation request failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientException("validation request failed"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: validation request failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientException("validation request failed"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            There was an unexpected error executing your request: validation request failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException("forbidden"));
        var fileSystem = CreateArchiveFileSystem();

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

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
        var result = await new CommandBuilder()
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
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(expectedStdErr);

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
        var result = await new CommandBuilder()
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
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            Could not create validation request!
            """);

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
        var result = await new CommandBuilder()
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
            Validating...
            ├── The validation is in progress.
            ├── The validation is in progress.
            └── ✓ Schema validation succeeded.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Schema validation succeeded.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
        Assert.Equal("{}", result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
            Validating...
            ├── The validation is in progress.
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Schema validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithError_ReturnsError_Interactive()
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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] The validation is in progress.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Schema validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithError_ReturnsError_JsonOutput()
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
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            Something went wrong during validation.
            Schema validation failed.
            """);

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
