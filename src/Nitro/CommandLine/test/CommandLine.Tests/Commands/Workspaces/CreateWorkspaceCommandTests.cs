using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CreateWorkspaceCommandTests
{
    [Fact]
    public async Task Create_WithName_JsonOutput_ReturnsWorkspace()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateWorkspaceAsync("My Workspace", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspaceResult("ws-1", "My Workspace", personal: false));

        var host = CreateHost(client, session: new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync(
            "workspace",
            "create",
            "--name",
            "My Workspace",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "ws-1",
              "name": "My Workspace",
              "personal": false
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IWorkspacesClient> client,
        TestSessionService session)
    {
        var host = new CommandBuilder()
            .AddService(client.Object)
            .AddService<ISessionService>(session);

        return host;
    }

    private static ICreateWorkspaceCommandMutation_CreateWorkspace CreateWorkspaceResult(
        string id,
        string name,
        bool personal)
    {
        var workspace = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace_Workspace_Workspace>();
        workspace.SetupGet(x => x.Id).Returns(id);
        workspace.SetupGet(x => x.Name).Returns(name);
        workspace.SetupGet(x => x.Personal).Returns(personal);

        var payload = new Mock<ICreateWorkspaceCommandMutation_CreateWorkspace>();
        payload.SetupGet(x => x.Workspace).Returns(workspace.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
