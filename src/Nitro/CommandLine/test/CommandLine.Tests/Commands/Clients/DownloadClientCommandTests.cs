using System.Text;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DownloadClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    private const string OutputPath = "/some/working/directory/queries.json";
    private const string OutputDir = "/some/working/directory/output-dir";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download the queries from a stage.

            Usage:
              nitro client download [options]

            Options:
              --api-id <api-id>         The ID of the API [env: NITRO_API_ID]
              --stage <stage>           The name of the stage [env: NITRO_STAGE]
              --path <path> (REQUIRED)  The path where the client is stored
              --format <folder|relay>   The format in which the client is stored [default: relay]
              --cloud-url <cloud-url>   The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>       The API key used for authentication [env: NITRO_API_KEY]
              --output <json>           The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help            Show help and usage information

            Example:
              nitro client download \
                --api-id "<api-id>" \
                --stage "dev" \
                --path ./operations.json
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--path",
            OutputPath);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--stage'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Download_Should_ReturnError_When_ApiIdNotProvided(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--stage",
            Stage,
            "--path",
            OutputPath);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--api-id'.
            """);
        Assert.Equal(1, result.ExitCode);
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
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputPath);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task DownloadThrows_ReturnsError()
    {
        // arrange
        SetupDownloadPersistedQueriesException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputPath);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Fact]
    public async Task NoPublishedClient_ReturnsError()
    {
        // arrange
        SetupDownloadPersistedQueries(null);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputPath);

        // assert
        result.AssertError(
            """
            Could not find a published client on stage 'dev'.
            """);
    }

    [Fact]
    public async Task Success_RelayFormat_WritesJsonFile()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"),
            ("doc-2", Guid.Empty, "query { world }"));

        SetupDownloadPersistedQueries(queryStream);

        var outputStream = SetupCreateFile("queries.json");

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputPath,
            "--format",
            "relay");

        // assert
        result.AssertSuccess(
            """
            Downloaded client to '/some/working/directory/queries.json'.
            """);

        Encoding.UTF8.GetString(outputStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "doc-1": "query { hello }",
              "doc-2": "query { world }"
            }
            """);
    }

    [Fact]
    public async Task Success_FolderFormat_WritesFiles()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        SetupDownloadPersistedQueries(queryStream);

        var outputStream = SetupCreateFile(Path.Combine("output-dir", "doc-1.graphql"));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputDir,
            "--format",
            "folder");

        // assert
        result.AssertSuccess(
            """
            Downloaded client to '/some/working/directory/output-dir'.
            """);

        Assert.Equal("query { hello }", Encoding.UTF8.GetString(outputStream.ToArray()));
    }

    [Fact]
    public async Task Success_FolderFormat_ExistingDirectory_SkipsCreate()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        SetupDownloadPersistedQueries(queryStream);

        SetupDirectory("output-dir");
        var outputStream = SetupCreateFile(Path.Combine("output-dir", "doc-1.graphql"));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--path",
            OutputDir,
            "--format",
            "folder");

        // assert
        result.AssertSuccess(
            """
            Downloaded client to '/some/working/directory/output-dir'.
            """);

        Assert.Equal("query { hello }", Encoding.UTF8.GetString(outputStream.ToArray()));
    }

    [Fact]
    public async Task Download_Should_PromptForStage_When_ApiProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListStagesQuery(("stage-1", Stage));

        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));
        SetupDownloadPersistedQueries(queryStream);
        SetupCreateFile("queries.json");

        var command = StartInteractiveCommand(
            "client",
            "download",
            "--api-id",
            ApiId,
            "--path",
            OutputPath,
            "--format",
            "relay");

        // act
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Download_Should_PromptForApi_When_StageProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));

        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));
        SetupDownloadPersistedQueries(queryStream);
        SetupCreateFile("queries.json");

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "download",
            "--stage",
            Stage,
            "--path",
            OutputPath,
            "--format",
            "relay");

        // act
        command.SelectOption(0); // Select API
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Download_Should_PromptForApiAndStage_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListStagesQuery(("stage-1", Stage));

        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));
        SetupDownloadPersistedQueries(queryStream);
        SetupCreateFile("queries.json");

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "download",
            "--path",
            OutputPath,
            "--format",
            "relay");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    private static Stream CreatePersistedQueryStream(
        params (string DocumentId, Guid ApiId, string Content)[] queries)
    {
        var jsonArray = queries
            .Select(q => new { apiId = q.ApiId, documentIds = new[] { q.DocumentId }, content = q.Content });

        var json = JsonSerializer.Serialize(jsonArray);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
