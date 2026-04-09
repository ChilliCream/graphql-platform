using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class CreateClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new client.

            Usage:
              nitro client create [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --name <name>            The name of the client [env: NITRO_CLIENT_NAME]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client create \
                --name "my-client" \
                --api-id "<api-id>"
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
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupSession();
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--name",
            ClientName);

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnSuccess()
    {
        // arrange
        SetupSelectApisPrompt(("api-1", "products"));
        SetupCreateClientMutation(CreateClientNode(ClientId, ClientName, ApiName));

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "create");

        // act
        command.SelectOption(0);
        command.Input(ClientName);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithApiId_AndName_ReturnSuccess_NonInteractive()
    {
        // arrange
        SetupCreateClientMutation(CreateClientNode(ClientId, ClientName, ApiName));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.AssertSuccess(
            """
            Creating client 'web-client' for API 'api-1'
            └── ✓ Created client 'web-client'.

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
    public async Task WithApiId_AndName_ReturnSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateClientMutation(CreateClientNode(ClientId, ClientName, ApiName));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);
    }

    [Theory]
    [MemberData(nameof(GetCreateClientErrors))]
    public async Task CreateClientHasErrors_ReturnsError(
        ICreateClientCommandMutation_CreateClient_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupCreateClientMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating client 'web-client' for API 'api-1'
            └── ✕ Failed to create the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNoClient_ReturnsError()
    {
        // arrange
        SetupCreateClientMutation();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating client 'web-client' for API 'api-1'
            └── ✕ Failed to create the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateClientThrows_ReturnsError()
    {
        // arrange
        SetupCreateClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "create",
            "--api-id",
            ApiId,
            "--name",
            ClientName);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating client 'web-client' for API 'api-1'
            └── ✕ Failed to create the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    private static ICreateClientCommandMutation_CreateClient_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([apiName]);

        var clientNode = new Mock<ICreateClientCommandMutation_CreateClient_Client>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
    }

    public static TheoryData<ICreateClientCommandMutation_CreateClient_Errors, string> GetCreateClientErrors()
    {
        var apiNotFound = new Mock<ICreateClientCommandMutation_CreateClient_Errors_ApiNotFoundError>(MockBehavior.Strict);
        apiNotFound.As<IApiNotFoundError>().SetupGet(x => x.Message).Returns("The API was not found.");

        var unauthorized = new Mock<ICreateClientCommandMutation_CreateClient_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        unauthorized.As<IUnauthorizedOperation>().SetupGet(x => x.Message).Returns("Unauthorized operation.");

        var genericError = new Mock<ICreateClientCommandMutation_CreateClient_Errors>(MockBehavior.Strict);
        genericError.As<IError>().SetupGet(x => x.Message).Returns("something bad happened");

        return new()
        {
            { apiNotFound.Object, "The API was not found." },
            { unauthorized.Object, "Unauthorized operation." },
            { genericError.Object, "Unexpected mutation error: something bad happened" }
        };
    }
}
