using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class CreateEnvironmentCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspace_ReturnsError()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        var host = CreateHost(client, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "create",
            "--name",
            "prod",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            $"""
            You are not logged in. Run `nitro login` to sign in or specify the workspace ID{" "}
            with the --workspace-id option (if available).
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithOptions_JsonOutput_ReturnsEnvironment()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateEnvironmentAsync(
                "ws-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEnvironmentResult("env-1", "prod", "Workspace"));

        var host = CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "create",
            "--workspace-id",
            "ws-1",
            "--name",
            "prod",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "env-1",
              "name": "prod",
              "workspace": {
                "name": "Workspace"
              }
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IEnvironmentsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IEnvironmentsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static TestSessionService NoSession() => new();

    private static ICreateEnvironmentCommandMutation_PushWorkspaceChanges CreateEnvironmentResult(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var environment = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Result_Environment>();
        environment.SetupGet(x => x.Id).Returns(id);
        environment.SetupGet(x => x.Name).Returns(name);
        environment.SetupGet(x => x.Workspace).Returns(workspace.Object);

        var change = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_WorkspaceChangePayload>();
        change.SetupGet(x => x.ReferenceId).Returns("ref-1");
        change.SetupGet(x => x.Error).Returns((ICreateEnvironmentCommandMutation_PushWorkspaceChanges_Changes_Error?)null);
        change.SetupGet(x => x.Result).Returns(environment.Object);

        var payload = new Mock<ICreateEnvironmentCommandMutation_PushWorkspaceChanges>();
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
