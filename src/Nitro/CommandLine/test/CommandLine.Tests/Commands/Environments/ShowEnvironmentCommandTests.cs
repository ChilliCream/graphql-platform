using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ShowEnvironmentCommandTests
{
    [Fact]
    public async Task Show_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("environment", "show");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Show_WithData_JsonOutput_ReturnsEnvironment()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowEnvironmentAsync("env-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEnvironmentNode("env-1", "prod", "Workspace"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "show",
            "env-1",
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

    [Fact]
    public async Task Show_WithoutData_ReturnsSuccessAndErrorMessage()
    {
        // arrange
        var client = new Mock<IEnvironmentsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowEnvironmentAsync("env-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowEnvironmentCommandQuery_Node?)null);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "environment",
            "show",
            "env-missing");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ Could not find an environment with ID env-missing
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IEnvironmentsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IEnvironmentsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IShowEnvironmentCommandQuery_Node_Environment CreateEnvironmentNode(
        string id,
        string name,
        string workspaceName)
    {
        var workspace = new Mock<IListEnvironmentCommandQuery_WorkspaceById_Environments_Edges_Node_Workspace_Workspace>();
        workspace.SetupGet(x => x.Name).Returns(workspaceName);

        var environment = new Mock<IShowEnvironmentCommandQuery_Node_Environment>();
        environment.SetupGet(x => x.Id).Returns(id);
        environment.SetupGet(x => x.Name).Returns(name);
        environment.SetupGet(x => x.Workspace).Returns(workspace.Object);

        return environment.Object;
    }
}
