using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ShowWorkspaceCommandTests
{
    [Fact]
    public async Task Show_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("workspace", "show");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Show_WithData_JsonOutput_ReturnsWorkspace()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync("ws-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspaceNode("ws-1", "Workspace", personal: false));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "workspace",
            "show",
            "ws-1",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "ws-1",
              "name": "Workspace",
              "personal": false
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Show_WithoutData_ReturnsErrorMessage()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowWorkspaceAsync("ws-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowWorkspaceCommandQuery_Node?)null);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "workspace",
            "show",
            "ws-missing");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Could not find a workspace with ID ws-missing
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(Mock<IWorkspacesClient> client)
    {
        var host = new CommandTestHost()
            .AddService(client.Object)
            .AddService<ISessionService>(TestSessionService.WithWorkspace());

        return host;
    }

    private static IShowWorkspaceCommandQuery_Node_Workspace CreateWorkspaceNode(
        string id,
        string name,
        bool personal)
    {
        var workspace = new Mock<IShowWorkspaceCommandQuery_Node_Workspace>();
        workspace.SetupGet(x => x.Id).Returns(id);
        workspace.SetupGet(x => x.Name).Returns(name);
        workspace.SetupGet(x => x.Personal).Returns(personal);

        return workspace.Object;
    }
}
