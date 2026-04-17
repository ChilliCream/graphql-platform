using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class CreateEnvironmentCommandTests(NitroCommandFixture fixture)
    : EnvironmentsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new environment.

            Usage:
              nitro environment create [options]

            Options:
              -n, --name <name>              The name of the environment
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro environment create --name "dev"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--name",
            EnvironmentName);

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create");

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateEnvironmentMutation("workspace-from-session", EnvironmentName);

        var command = StartInteractiveCommand(
            "environment",
            "create");

        // act
        command.Input(EnvironmentName);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithOptions_ReturnSuccess_NonInteractive()
    {
        // arrange
        SetupCreateEnvironmentMutation(WorkspaceId, EnvironmentName);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.AssertSuccess(
            """
            Creating environment 'production'
            └── ✓ Created environment 'production'.

            {
              "id": "env-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateEnvironmentMutation(WorkspaceId, EnvironmentName);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "env-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);
    }

    [Fact]
    public async Task MutationReturnsNoChangeResult_ReturnsError()
    {
        // arrange
        SetupCreateEnvironmentMutationNoChanges(WorkspaceId, EnvironmentName);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment 'production'
            └── ✕ Failed to create the environment.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateEnvironmentHasChangeError_ReturnsError()
    {
        // arrange
        var changeError = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
        changeError.As<IError>().SetupGet(x => x.Message).Returns("Create denied");

        SetupCreateEnvironmentMutationWithChangeError(WorkspaceId, EnvironmentName, changeError.Object);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment 'production'
            └── ✕ Failed to create the environment.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Create denied
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateEnvironmentHasMutationErrors_ReturnsError()
    {
        // arrange
        var typedError = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors_ChangeStructureInvalid>(MockBehavior.Strict);
        typedError.As<IError>().SetupGet(x => x.Message).Returns("Change structure invalid.");

        SetupCreateEnvironmentMutationWithErrors(WorkspaceId, EnvironmentName, typedError.Object);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment 'production'
            └── ✕ Failed to create the environment.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Change structure invalid.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsResultNotEnvironment_ReturnsError()
    {
        // arrange
        SetupCreateEnvironmentMutationWithWrongResultType(WorkspaceId, EnvironmentName);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment 'production'
            └── ✕ Failed to create the environment.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateEnvironmentThrows_ReturnsError()
    {
        // arrange
        SetupCreateEnvironmentMutationException(WorkspaceId, EnvironmentName);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            EnvironmentName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment 'production'
            └── ✕ Failed to create the environment.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }
}
