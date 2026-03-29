using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class SetApiSettingsCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "api",
                "set-settings",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Sets the settings of an API

            Usage:
              nitro api set-settings <id> [options]

            Arguments:
              <id>  The ID

            Options:
                            --treat-dangerous-as-breaking    Treat dangerous changes as breaking [env: NITRO_TREAT_DANGEROUS_AS_BREAKING]
                            --allow-breaking-schema-changes  Allow breaking schema changes when no client breaks [env: NITRO_ALLOW_BREAKING_SCHEMA_CHANGES]
                            --cloud-url <cloud-url>          The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>              The API key that is used for the authentication [env: NITRO_API_KEY]
                            --output <json>                  The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
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
        var result = await new CommandBuilder()
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
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
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
        var result = await new CommandBuilder()
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
                        Updating API settings...
                        └── Successfully updated API settings!

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
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateSetSettingsMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
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

            [    ] Updating API settings...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(SetApiSettingsMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateSetSettingsMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
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
            Updating API settings...
            └── Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(SetApiSettingsMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ISetApiSettingsCommandMutation_UpdateApiSettings_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreateSetSettingsMutationErrorClient(mutationError);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
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
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientException("update failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
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

            [    ] Updating API settings...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: update failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientException("update failed"));

        // act
        var result = await new CommandBuilder()
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
            Updating API settings...
            └── Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: update failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientException("update failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
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
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: update failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
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

            [    ] Updating API settings...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateSetSettingsExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
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
            Updating API settings...
            └── Failed!
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
        var client = CreateSetSettingsExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
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
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    public static TheoryData<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors, string>
        SetApiSettingsMutationErrorCases =>
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
