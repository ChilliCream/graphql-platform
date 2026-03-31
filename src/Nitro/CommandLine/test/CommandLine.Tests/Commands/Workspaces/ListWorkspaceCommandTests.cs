using ChilliCream.Nitro.Client;
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
              List all workspaces.

            Usage:
              nitro workspace list [options]

            Options:
              --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The output format. Setting this option will disable the interactive mode. [env: NITRO_OUTPUT_FORMAT]
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

                   ┌──────┬────────────────────┬────────────┐
                   │ Id   │ Name               │ IsPersonal │
                   ├──────┼────────────────────┼────────────┤
                   │ ws-1 │ my-workspace       │ ✕          │
                   │ ws-2 │ personal-workspace │ ✓          │
                   └──────┴────────────────────┴────────────┘
                                   Workspaces

                   ┌──────┬────────────────────┬────────────┐
                   │ Id   │ Name               │ IsPersonal │
                   ├──────┼────────────────────┼────────────┤
                   │ ws-1 │ my-workspace       │ ✕          │
                   │ ws-2 │ personal-workspace │ ✓          │
                   └──────┴────────────────────┴────────────┘
                                   Workspaces

                   ┌──────┬────────────────────┬────────────┐
                   │ Id   │ Name               │ IsPersonal │
                   ├──────┼────────────────────┼────────────┤
                   │ ws-1 │ my-workspace       │ ✕          │
                   │ ws-2 │ personal-workspace │ ✓          │
                   └──────┴────────────────────┴────────────┘
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_NonInteractive()
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
            .AddInteractionMode(InteractionMode.NonInteractive)
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
    public async Task WithApiKey_ReturnsSuccess_JsonOutput()
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
            .AddInteractionMode(InteractionMode.JsonOutput)
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

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_NonInteractive()
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
            .AddInteractionMode(InteractionMode.NonInteractive)
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
    public async Task WithApiKey_NoData_ReturnsSuccess_JsonOutput()
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
            .AddInteractionMode(InteractionMode.JsonOutput)
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
                      └──────┴──────────────┴────────────┘
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
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
            .AddInteractionMode(InteractionMode.NonInteractive)
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

    [Fact]
    public async Task WithCursor_ReturnsSuccess_JsonOutput()
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
            .AddInteractionMode(InteractionMode.JsonOutput)
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

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments("workspace", "list")
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
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
