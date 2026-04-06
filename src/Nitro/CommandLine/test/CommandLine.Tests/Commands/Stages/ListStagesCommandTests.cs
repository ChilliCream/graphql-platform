namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class ListStagesCommandTests(NitroCommandFixture fixture) : StagesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "stage",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all stages of an API.

            Usage:
              nitro stage list [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro stage list --api-id "<api-id>"
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
            "stage",
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "list");

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task ListStagesThrows_ReturnsError()
    {
        // arrange
        SetupListStagesQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListStagesQuery(
            ("stage-1", "production", new[] { "staging" }),
            ("stage-2", "staging", Array.Empty<string>()));

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "production",
                  "conditions": [
                    {
                      "kind": "AfterStage",
                      "name": "staging"
                    }
                  ]
                },
                {
                  "id": "stage-2",
                  "name": "staging",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListStagesQuery();

        // act
        var result = await ExecuteCommandAsync(
            "stage",
            "list",
            "--api-id",
            ApiId);

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
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListStagesQuery(
            ("stage-1", "production", new[] { "staging" }),
            ("stage-2", "staging", Array.Empty<string>()));

        var command = StartInteractiveCommand(
            "stage",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListStagesQuery();

        var command = StartInteractiveCommand(
            "stage",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }
}
