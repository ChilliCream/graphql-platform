using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ValidateMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultMcpFeatureCollectionId = "mcp-1";
    private const string DefaultStage = "production";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mcp",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate an MCP feature collection version.
            
            Usage:
              nitro mcp validate [options]
            
            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --stage <stage> (REQUIRED)                                          The name of the stage [env: NITRO_STAGE]
              -p, --prompt-pattern <prompt-pattern>                               One or more file patterns to locate MCP prompt definition files (*.json)
              -t, --tool-pattern <tool-pattern>                                   One or more file patterns to locate MCP tool definition files (*.graphql)
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information
            """);
    }

    [Fact]
    public async Task NoSession_Or_ApiKey_ReturnsError_NonInteractive()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 14 prompt(s) and 0 tool(s).
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            A prompt with the name 'source-schema-1-settings' already exists in the archive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            A prompt with the name 'source-schema-1-settings' already exists in the archive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
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
    [MemberData(nameof(MutationErrorCasesNonInteractive))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors mutationError,
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        InteractionMode mode,
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors mutationError,
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
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
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
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>?)null);
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the MCP feature collection.
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
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>?)null);
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
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
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated MCP feature collection against stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
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
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {}
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            MCP Feature Collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Subscription_FailedWithSimpleError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var errorMock = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            MCP Feature Collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
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
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithTimeoutError_ReturnsError_NonInteractive()
    {
        // arrange
        var timeoutError = new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors_ProcessingTimeoutError(
            "ProcessingTimeoutError",
            "The validation timed out.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(timeoutError)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The validation timed out.
            MCP Feature Collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithValidationError_ReturnsError_NonInteractive()
    {
        // arrange
        var validationError = new Mock<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        validationError.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(Array.Empty<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections>());

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(validationError.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "validate",
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--prompt-pattern",
                "**/*.json",
                "--tool-pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating MCP feature collection against stage 'production'
            ├── Found 1 prompt(s) and 1 tool(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP Feature Collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static Mock<IFileSystem> CreateMcpFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns(["prompt.mcp-prompt.json"]);
        fileSystem.Setup(x => x.OpenReadStream("prompt.mcp-prompt.json"))
            .Returns(new MemoryStream("{}"u8.ToArray()));
        return fileSystem;
    }

    private static IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection CreateSuccessPayload()
    {
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection CreateValidationPayloadWithErrors(
        params IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static (Mock<IMcpClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetup(
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection payload)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateMcpFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IMcpClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithSubscription(
        IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection mutationPayload,
        IEnumerable<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate> subscriptionEvents)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToMcpFeatureCollectionValidationAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateMcpFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IMcpClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithException(
        Exception ex)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionValidationAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateMcpFileSystem();

        return (client, fileSystem);
    }

    private static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate CreateOperationInProgress()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate CreateValidationInProgress()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    private static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate CreateValidationSuccess()
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_McpFeatureCollectionVersionValidationSuccess(
            "McpFeatureCollectionVersionValidationSuccess",
            ProcessingState.Success);
    }

    private static IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate CreateValidationFailed(
        params IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_Errors[] errors)
    {
        return new ValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate_McpFeatureCollectionVersionValidationFailed(
            "McpFeatureCollectionVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        var modes = new[] { InteractionMode.Interactive, InteractionMode.JsonOutput };

        foreach (var mode in modes)
        {
            yield return
            [
                mode,
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to validate."),
                """
                Not authorized to validate.
                """
            ];

            yield return
            [
                mode,
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    DefaultStage),
                """
                Stage not found.
                """
            ];

            yield return
            [
                mode,
                new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    DefaultMcpFeatureCollectionId,
                    "MCP Feature Collection not found."),
                """
                MCP Feature Collection not found.
                """
            ];

            var unexpectedError = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>();
            unexpectedError
                .As<IError>()
                .SetupGet(x => x.Message)
                .Returns("Something went wrong.");

            yield return
            [
                mode,
                unexpectedError.Object,
                """
                Unexpected mutation error: Something went wrong.
                """
            ];
        }
    }

    public static IEnumerable<object[]> MutationErrorCasesNonInteractive()
    {
        yield return
        [
            new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            """
            Not authorized to validate.
            """
        ];

        yield return
        [
            new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new ValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                DefaultMcpFeatureCollectionId,
                "MCP Feature Collection not found."),
            """
            MCP Feature Collection not found.
            """
        ];

        var unexpectedError = new Mock<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection_Errors>();
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
