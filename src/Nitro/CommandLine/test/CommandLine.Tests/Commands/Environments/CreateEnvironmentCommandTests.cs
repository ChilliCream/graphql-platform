using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class CreateEnvironmentCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "environment",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Creates a new environment

            Usage:
              nitro environment create [options]

            Options:
              -n, --name <name>              The name of the environment.
              --workspace-id <workspace-id>  The ID of the workspace. [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>            The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information
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
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create")
            .ExecuteAsync();

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
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "workspace-from-session",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload("env-1", "production", "workspace-a"));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "environment",
                "create")
            .Start();

        // act
        command.Input("production");
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("? Name production", result.StdOut);
        Assert.Contains("Successfully created environment!", result.StdOut);
        Assert.Contains("\"name\": \"production\"", result.StdOut);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload("env-1", "production", "workspace-a"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating environment...
            └── Successfully created environment!

            {
              "id": "env-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessPayload("env-1", "production", "workspace-a"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNoChangeResult_ReturnsError()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(changes: [], errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment...
            └── Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create environment.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsChangeError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var changeError = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
        changeError.As<IError>().SetupGet(x => x.Message).Returns("Create denied");

        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(
                changes: [CreateChange(result: null, error: changeError.Object)],
                errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Create denied
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsTypedError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var typedError = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors_ChangeStructureInvalid>(
            MockBehavior.Strict);
        typedError.As<IError>().SetupGet(x => x.Message).Returns("Change structure invalid.");

        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(changes: null, errors: [typedError.Object]));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Change structure invalid.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsResultNotEnvironment_ReturnsError()
    {
        // arrange
        var wrongResult = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_ApiDocument>(
            MockBehavior.Strict);

        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayload(
                changes: [CreateChange(wrongResult.Object, error: null)],
                errors: null));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating environment...
            └── Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create environment.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientException("create failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: create failed
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "environment",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "production")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static Mock<IEnvironmentsClient> CreateExceptionClient(Exception ex)
    {
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges CreateSuccessPayload(
        string id,
        string name,
        string workspaceName)
    {
        return CreatePayload(
            changes: [CreateChange(CreateEnvironmentResult(id, name, workspaceName), error: null)],
            errors: null);
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges CreatePayload(
        IReadOnlyList<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes>? changes,
        IReadOnlyList<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Errors>? errors)
    {
        var payload = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(changes);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes CreateChange(
        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result? result,
        ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error? error)
    {
        var change = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.ReferenceId).Returns("env");
        change.SetupGet(x => x.Result).Returns(result);
        change.SetupGet(x => x.Error).Returns(error);
        return change.Object;
    }

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_Environment CreateEnvironmentResult(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace>(
            MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var result = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_Environment>(
            MockBehavior.Strict);
        result.SetupGet(x => x.Id).Returns(id);
        result.SetupGet(x => x.Name).Returns(name);
        result.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return result.Object;
    }
}
