using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CreateWorkspaceCommandTests(NitroCommandFixture fixture)
    : WorkspacesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new workspace.

            Usage:
              nitro workspace create [options]

            Options:
              --name <name>            The name of the workspace
              --default                Set the created workspace as the default workspace
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro workspace create --name "my-workspace"
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
            "workspace",
            "create",
            "--name",
            WorkspaceName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredName_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create");

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess()
    {
        // arrange
        SetupCreateWorkspaceMutation();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default",
            "false");

        // assert
        result.AssertSuccess(
            """
            Creating workspace 'my-workspace'
            └── ✓ Created workspace 'my-workspace'.

            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateWorkspaceMutation();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default",
            "false");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
    }

    [Fact]
    public async Task MissingName_PromptsUser_ReturnsSuccess()
    {
        // arrange
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateWorkspaceMutation();

        var command = StartInteractiveCommand(
            "workspace",
            "create");

        // act
        command.Input(WorkspaceName);
        command.Confirm(false);

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task CreateWorkspaceReturnsNullWorkspace_ReturnsError()
    {
        // arrange
        SetupCreateWorkspaceMutationNullWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default",
            "false");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetCreateWorkspaceErrors))]
    public async Task CreateWorkspaceHasErrors_ReturnsError(
        ICreateWorkspaceCommandMutation_CreateWorkspace_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupCreateWorkspaceMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default",
            "false");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors, string>
        GetCreateWorkspaceErrors() => new()
    {
        { CreateCreateWorkspaceUnauthorizedError(), "Not authorized" },
        { CreateCreateWorkspaceValidationError(), "Name is required" }
    };

    [Fact]
    public async Task CreateWorkspaceThrows_ReturnsError()
    {
        // arrange
        SetupCreateWorkspaceMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default",
            "false");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Create_Should_PromptForDefault_When_InteractiveAndDefaultNotProvided()
    {
        // arrange
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateWorkspaceMutation();

        var command = StartInteractiveCommand(
            "workspace",
            "create",
            "--name",
            WorkspaceName);

        // act
        command.Confirm(true);

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);

        _sessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Services.Sessions.Workspace>(w => w.Id == WorkspaceId && w.Name == WorkspaceName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_Should_SetDefault_When_DefaultFlagProvided()
    {
        // arrange
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateWorkspaceMutation();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "create",
            "--name",
            WorkspaceName,
            "--default");

        // assert
        Assert.Equal(0, result.ExitCode);

        _sessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Services.Sessions.Workspace>(w => w.Id == WorkspaceId && w.Name == WorkspaceName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_InteractiveWithNameOption()
    {
        // arrange
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateWorkspaceMutation();

        var command = StartInteractiveCommand(
            "workspace",
            "create",
            "--name",
            WorkspaceName);

        // act
        command.Confirm(false);

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);

        _sessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.IsAny<Services.Sessions.Workspace>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
