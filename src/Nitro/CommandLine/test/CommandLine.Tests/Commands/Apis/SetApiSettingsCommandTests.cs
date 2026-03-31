using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class SetApiSettingsCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "api",
                "set-settings",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set the settings of an API.

            Usage:
              nitro api set-settings <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
                            --treat-dangerous-as-breaking    Treat dangerous changes as breaking [env: NITRO_TREAT_DANGEROUS_AS_BREAKING]
                            --allow-breaking-schema-changes  Allow breaking schema changes when no client breaks [env: NITRO_ALLOW_BREAKING_SCHEMA_CHANGES]
                            --cloud-url <cloud-url>          The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>              The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>                  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help                   Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateSetApiSettingsPayload("api-1", "my-api", ["products"]));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
                result.AssertSuccess(
                        """
                        Updating settings for API 'api-1'
                        └── ✓ Updated settings for API 'api-1'.

                        {
                            "id": "api-1",
                            "name": "my-api",
                            "path": "products",
                            "workspace": {
                                "name": "Workspace"
                            },
                            "apiDetailPromptSettings": {
                                "apiDetailPromptSchemaRegistry": {
                                    "treatDangerousAsBreaking": true,
                                    "allowBreakingSchemaChanges": false
                                }
                            }
                        }
                        """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(SetApiSettingsMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        InteractionMode mode,
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateSetSettingsMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(SetApiSettingsMutationErrorCasesNonInteractive))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateSetSettingsMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
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
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
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
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_PromptsForTreatDangerousAsBreaking_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateSetApiSettingsPayload("api-1", "my-api", ["products"]));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--allow-breaking-schema-changes",
                "false")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_PromptsForAllowBreakingChanges_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateSetApiSettingsPayload("api-1", "my-api", ["products"]));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingOptions_ReturnsError_NonInteractive(InteractionMode mode)
    {
        // arrange & act
        var client = new Mock<IApisClient>(MockBehavior.Strict);

        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--treat-dangerous-as-breaking'.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "set-settings")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'set-settings'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNoData_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(
                    api: null,
                    errors: []));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "set-settings",
                "api-1",
                "--treat-dangerous-as-breaking",
                "true",
                "--allow-breaking-schema-changes",
                "false")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating settings for API 'api-1'
            └── ✕ Failed to update the API settings.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    public static TheoryData<InteractionMode, ISetApiSettingsCommandMutation_UpdateApiSettings_Errors, string>
        SetApiSettingsMutationErrorCases =>
        new()
        {
            {
                InteractionMode.Interactive,
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_ApiNotFoundError(
                    "ApiNotFoundError",
                    "API not found",
                    "api-1"),
                """
                API not found
                """
            },
            {
                InteractionMode.Interactive,
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized"),
                """
                Not authorized
                """
            },
            {
                InteractionMode.Interactive,
                CreateUnknownSetApiSettingsMutationError("payload denied"),
                """
                Unexpected mutation error: payload denied
                """
            },
            {
                InteractionMode.JsonOutput,
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_ApiNotFoundError(
                    "ApiNotFoundError",
                    "API not found",
                    "api-1"),
                """
                API not found
                """
            },
            {
                InteractionMode.JsonOutput,
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized"),
                """
                Not authorized
                """
            },
            {
                InteractionMode.JsonOutput,
                CreateUnknownSetApiSettingsMutationError("payload denied"),
                """
                Unexpected mutation error: payload denied
                """
            }
        };

    public static TheoryData<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors, string>
        SetApiSettingsMutationErrorCasesNonInteractive =>
        new()
        {
            {
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_ApiNotFoundError(
                    "ApiNotFoundError",
                    "API not found",
                    "api-1"),
                """
                API not found
                """
            },
            {
                new SetApiSettingsCommandMutation_UpdateApiSettings_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized"),
                """
                Not authorized
                """
            },
            {
                CreateUnknownSetApiSettingsMutationError("payload denied"),
                """
                Unexpected mutation error: payload denied
                """
            }
        };

    private static Mock<IApisClient> CreateSetSettingsMutationErrorClient(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError)
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateSetApiSettingsPayloadWithErrors(mutationError));
        return client;
    }

    private static Mock<IApisClient> CreateSetSettingsExceptionClient(Exception ex)
    {
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static ISetApiSettingsCommandMutation_UpdateApiSettings_Errors CreateUnknownSetApiSettingsMutationError(
        string message)
    {
        var error = new Mock<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors>(MockBehavior.Strict);
        error.As<IError>()
            .SetupGet(x => x.Message)
            .Returns(message);
        return error.Object;
    }
}
