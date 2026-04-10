using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DeleteClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete a client.

            Usage:
              nitro client delete [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client delete "<client-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            ClientId,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task Delete_Should_SucceedWithConfirmation_When_UserConfirms_Interactive()
    {
        // arrange
        SetupDeleteClientMutation(CreateClientNode(ClientId, ClientName, ApiName));
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "delete",
            ClientId);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Delete_Should_PromptForApiAndClient_When_NoIdProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt(("api-1", "products"));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupDeleteClientMutation(CreateClientNode(ClientId, ClientName, ApiName));

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "delete");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select Client
        command.Confirm(true);   // Confirm deletion
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingClientId_NonInteractive_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "delete");

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task WithClientId_AndForce_ReturnSuccess()
    {
        // arrange
        SetupDeleteClientMutation(CreateClientNode(ClientId, ClientName, ApiName));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            ClientId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            ? Which client do you want to delete?: client-1
            Deleting client 'client-1'
            └── ✓ Deleted client 'client-1'.

            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "delete",
            ClientId);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The client was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsClientNotFoundError_ReturnsError()
    {
        // arrange
        var notFound = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Errors_ClientNotFoundError>(
            MockBehavior.Strict);
        notFound.As<IClientNotFoundError>().SetupGet(x => x.Message).Returns("Client not found.");

        SetupDeleteClientMutation(errors: notFound.Object);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            ClientId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Client not found.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which client do you want to delete?: client-1
            Deleting client 'client-1'
            └── ✕ Failed to delete the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNoClient_ReturnsError()
    {
        // arrange
        SetupDeleteClientMutation();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            ClientId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which client do you want to delete?: client-1
            Deleting client 'client-1'
            └── ✕ Failed to delete the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DeleteClientThrows_ReturnsError()
    {
        // arrange
        SetupDeleteClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "delete",
            ClientId,
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which client do you want to delete?: client-1
            Deleting client 'client-1'
            └── ✕ Failed to delete the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    private static IDeleteClientByIdCommandMutation_DeleteClientById_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([apiName]);

        var clientNode = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }
}
