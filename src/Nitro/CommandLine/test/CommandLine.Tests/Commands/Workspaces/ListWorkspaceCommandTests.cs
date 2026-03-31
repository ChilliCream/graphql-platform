using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ListWorkspaceCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
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
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

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
        var result = await new CommandBuilder(fixture)
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

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
        var result = await new CommandBuilder(fixture)
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

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
        var result = await new CommandBuilder(fixture)
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
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments("workspace", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
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
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
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
