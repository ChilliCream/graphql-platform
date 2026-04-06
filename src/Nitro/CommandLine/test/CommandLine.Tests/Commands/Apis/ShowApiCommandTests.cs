namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class ShowApiCommandTests(NitroCommandFixture fixture) : ApisCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api",
            "show",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show details of an API.

            Usage:
              nitro api show <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro api show "<api-id>"
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
            "api",
            "show",
            ApiId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ApiNotFound_ReturnsError()
    {
        // arrange
        SetupShowApiQuery(null);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "show",
            ApiId);

        // assert
        result.AssertError(
            """
            The API with ID 'api-1' was not found.
            """);
    }

    [Fact]
    public async Task ShowApiThrows_ReturnsError()
    {
        // arrange
        SetupShowApiQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "show",
            ApiId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess()
    {
        // arrange
        SetupShowApiQuery(CreateShowApiNode(ApiId, ApiName, ["products"]));

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "show",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "api-1",
              "name": "my-api",
              "path": "products",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }
}
