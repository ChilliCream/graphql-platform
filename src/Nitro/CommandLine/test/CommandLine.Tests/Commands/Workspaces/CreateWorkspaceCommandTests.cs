using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CreateWorkspaceCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "workspace",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new workspace.

            Usage:
              nitro workspace create [options]

            Options:
                            --default                Set the created workspace as the default workspace
                            --name <name>            The name of the workspace
                            --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information
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
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredName_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "create")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayload("ws-1", "my-workspace", false));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayload("ws-1", "my-workspace", false));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MissingName_PromptsUser_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayload("ws-1", "my-workspace", false));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "create")
            .Start();

        // act
        command.Input("my-workspace");
        command.Confirm(false);

        var result = await command.RunToCompletionAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ? Name my-workspace
            ? Set as default workspace [y/n] (y): n

            [    ] Failed to create the workspace.

            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayloadWithNullResult());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreateWorkspaceCommandMutation_CreateWorkspace_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ICreateWorkspaceCommandMutation_CreateWorkspace_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ICreateWorkspaceCommandMutation_CreateWorkspace_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the workspace.
            """);
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
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the workspace.
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
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating workspace 'my-workspace'
            └── ✕ Failed to create the workspace.
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
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync(
                "my-workspace",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "create",
                "--name",
                "my-workspace")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    public static TheoryData<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors, string> CreateMutationErrorCases =>
        new()
        {
            {
                new CreateWorkspaceCommandMutation_CreateWorkspace_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """
            },
            {
                new CreateWorkspaceCommandMutation_CreateWorkspace_Errors_ValidationError(
                    "ValidationError", "Name is required", []),
                """
                Name is required
                """
            }
        };

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateWorkspacePayload(
        string id, string name, bool personal)
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns(new CreateWorkspaceCommandMutation_CreateWorkspace_Workspace_Workspace(id, name, personal));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors>());
        return payload.Object;
    }

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateWorkspacePayloadWithNullResult()
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns((ICreateWorkspaceCommandMutation_CreateWorkspace_Workspace?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateWorkspaceCommandMutation_CreateWorkspace_Errors>());
        return payload.Object;
    }

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateWorkspacePayloadWithErrors(
        params ICreateWorkspaceCommandMutation_CreateWorkspace_Errors[] errors)
    {
        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>(MockBehavior.Strict);
        payload.SetupGet(x => x.Workspace)
            .Returns((ICreateWorkspaceCommandMutation_CreateWorkspace_Workspace?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
