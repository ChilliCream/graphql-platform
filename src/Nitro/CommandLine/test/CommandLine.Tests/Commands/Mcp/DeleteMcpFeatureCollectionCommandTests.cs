using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class DeleteMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an MCP feature collection.

            Usage:
              nitro mcp delete [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro mcp delete "<mcp-feature-collection-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            "--force");

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "mcp",
            "delete",
            McpFeatureCollectionId);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The MCP Feature Collection was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DeleteMcpFeatureCollectionThrows_ReturnsError()
    {
        // arrange
        SetupDeleteMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetDeleteMcpFeatureCollectionErrors))]
    public async Task DeleteMcpFeatureCollectionHasErrors_ReturnsError(
        IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupDeleteMcpFeatureCollectionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DeleteMcpFeatureCollectionReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupDeleteMcpFeatureCollectionMutationNullResult();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✕ Failed to delete the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupDeleteMcpFeatureCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Deleting MCP feature collection 'mcp-1'
            └── ✓ Deleted MCP feature collection 'mcp-1'.

            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupDeleteMcpFeatureCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "delete",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupDeleteMcpFeatureCollectionMutation();

        var command = StartInteractiveCommand(
            "mcp",
            "delete",
            McpFeatureCollectionId);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    public static TheoryData<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors, string>
        GetDeleteMcpFeatureCollectionErrors() => new()
    {
        {
            new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors_McpFeatureCollectionNotFoundError(
                "MCP Feature Collection not found", McpFeatureCollectionId),
            "MCP Feature Collection not found"
        },
        {
            new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors_UnauthorizedOperation(
                "Not authorized", "UnauthorizedOperation"),
            "Not authorized"
        }
    };
}
