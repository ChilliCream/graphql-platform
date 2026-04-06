namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ShowEnvironmentCommandTests(NitroCommandFixture fixture)
    : EnvironmentsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "environment",
            "show",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show details of an environment.

            Usage:
              nitro environment show <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro environment show "<environment-id>"
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
            "environment",
            "show",
            EnvironmentId);

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
    public async Task EnvironmentNotFound_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupGetEnvironmentQuery("environment-1", null);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "show",
            "environment-1");

        // assert
        result.AssertError(
            """
            The environment with ID 'environment-1' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithEnvironmentId_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupGetEnvironmentQuery("environment-1",
            CreateShowEnvironmentNode("environment-1", EnvironmentName, WorkspaceName));

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "show",
            "environment-1");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "environment-1",
              "name": "production",
              "workspace": {
                "name": "workspace-a"
              }
            }
            """);
    }

    [Fact]
    public async Task ShowEnvironmentThrows_ReturnsError()
    {
        // arrange
        SetupGetEnvironmentQueryException("environment-1");

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "show",
            "environment-1");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
