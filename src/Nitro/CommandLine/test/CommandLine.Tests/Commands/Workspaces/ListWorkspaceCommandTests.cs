using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ListWorkspaceCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "workspace",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all workspaces

            Usage:
              nitro workspace list [options]

            Options:
              --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information
            """);
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage(
                null,
                false,
                ("ws-1", "my-workspace", false),
                ("ws-2", "personal-workspace", true)));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                           Workspaces

                              ┌──────┬──────────────────────┬────────────┐
                              │ Id   │ Name                 │ IsPersonal │
                              ├──────┼──────────────────────┼────────────┤
                              │ ws-1 │ my-workspace         │ ✕          │
                              │ ws-2 │ personal-workspace   │ ✓          │
                              └──────┴──────────────────────┴────────────┘
                                           Workspaces

                              ┌──────┬──────────────────────┬────────────┐
                              │ Id   │ Name                 │ IsPersonal │
                              ├──────┼──────────────────────┼────────────┤
                              │ ws-1 │ my-workspace         │ ✕          │
                              │ ws-2 │ personal-workspace   │ ✓          │
                              └──────┴──────────────────────┴────────────┘
                                           Workspaces

                              ┌──────┬──────────────────────┬────────────┐
                              │ Id   │ Name                 │ IsPersonal │
                              ├──────┼──────────────────────┼────────────┤
                              │ ws-1 │ my-workspace         │ ✕          │
                              │ ws-2 │ personal-workspace   │ ✓          │
                              └──────┴──────────────────────┴────────────┘                      {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage(
                "cursor-2",
                true,
                ("ws-1", "my-workspace", false),
                ("ws-2", "personal-workspace", true)));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "ws-1",
                  "name": "my-workspace",
                  "personal": false
                },
                {
                  "id": "ws-2",
                  "name": "personal-workspace",
                  "personal": true
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage());

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                      Workspaces

            There was no data found.
                      Workspaces

            There was no data found.
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage(
                null,
                false,
                ("ws-1", "my-workspace", false)));

        var command = new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "list",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                              Workspaces

                                   ┌──────┬──────────────┬────────────┐
                                   │ Id   │ Name         │ IsPersonal │
                                   ├──────┼──────────────┼────────────┤
                                   │ ws-1 │ my-workspace │ ✕          │
                                   └──────┴──────────────┴────────────┘
                                              Workspaces

                                   ┌──────┬──────────────┬────────────┐
                                   │ Id   │ Name         │ IsPersonal │
                                   ├──────┼──────────────┼────────────┤
                                   │ ws-1 │ my-workspace │ ✕          │
                                   └──────┴──────────────┴────────────┘
                                              Workspaces

                                   ┌──────┬──────────────┬────────────┐
                                   │ Id   │ Name         │ IsPersonal │
                                   ├──────┼──────────────┼────────────┤
                                   │ ws-1 │ my-workspace │ ✕          │
                                   └──────┴──────────────┴────────────┘                         {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListWorkspacesPage(
                null,
                false,
                ("ws-1", "my-workspace", false)));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "list",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "ws-1",
                  "name": "my-workspace",
                  "personal": false
                }
              ],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientException("list failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
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
        var client = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>
        CreateListWorkspacesPage(
            string? endCursor = null,
            bool hasNextPage = false,
            params (string Id, string Name, bool Personal)[] workspaces)
    {
        var items = workspaces
            .Select(static ws =>
                (IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node)
                new ListWorkspaceCommandQuery_Me_Workspaces_Edges_Node_Workspace(
                    ws.Id, ws.Name, ws.Personal))
            .ToArray();

        return new ConnectionPage<IListWorkspaceCommandQuery_Me_Workspaces_Edges_Node>(
            items, endCursor, hasNextPage);
    }

    private static Mock<IWorkspacesClient> CreateListExceptionClient(Exception ex)
    {
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListWorkspacesAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
