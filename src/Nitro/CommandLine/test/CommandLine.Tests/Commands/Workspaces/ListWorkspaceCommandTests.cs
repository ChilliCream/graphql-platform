namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ListWorkspaceCommandTests(NitroCommandFixture fixture)
    : WorkspacesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "list",
            "--help");

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

            Example:
              nitro workspace list
            """);
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListWorkspacesQuery(
            endCursor: null,
            hasNextPage: false,
            workspaces:
            [
                ("ws-1", "my-workspace", false),
                ("ws-2", "personal-workspace", true)
            ]);

        var command = StartInteractiveCommand(
            "workspace",
            "list");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListWorkspacesQuery(
            endCursor: "cursor-2",
            hasNextPage: true,
            workspaces:
            [
                ("ws-1", "my-workspace", false),
                ("ws-2", "personal-workspace", true)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "list");

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
    }

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListWorkspacesQuery();

        var command = StartInteractiveCommand(
            "workspace",
            "list");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListWorkspacesQuery();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListWorkspacesQuery(
            cursor: "cursor-1",
            workspaces:
            [
                ("ws-1", "my-workspace", false)
            ]);

        var command = StartInteractiveCommand(
            "workspace",
            "list",
            "--cursor",
            "cursor-1");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListWorkspacesQuery(
            cursor: "cursor-1",
            workspaces:
            [
                ("ws-1", "my-workspace", false)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "list",
            "--cursor",
            "cursor-1");

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
    }

    [Fact]
    public async Task ListWorkspacesThrows_ReturnsError()
    {
        // arrange
        SetupListWorkspacesQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "list");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
