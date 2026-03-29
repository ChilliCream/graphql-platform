using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DownloadClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "download",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download the queries from a stage

            Usage:
              nitro client download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --path <path> (REQUIRED)      The path where the client is stored
              --format <folder|relay>       The format in which the client is stored. [default: relay]
              --cloud-url <cloud-url>       The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>               The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        Assert.NotEmpty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--stage' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching queries...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: download failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: download failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: download failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching queries...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task NoPublishedClient_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching queries...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find a published client on stage 'production'.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task NoPublishedClient_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find a published client on stage 'production'.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task NoPublishedClient_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not find a published client on stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_RelayFormat_WritesJsonFile_Interactive()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"),
            ("doc-2", Guid.Empty, "query { world }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var fileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("queries.json")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("queries.json")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json",
                "--format",
                "relay")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Downloaded client to 'queries.json'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(fileStream.ToArray());
        Assert.Contains("doc-1", written);
        Assert.Contains("doc-2", written);
        Assert.Contains("query { hello }", written);
        Assert.Contains("query { world }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_RelayFormat_WritesJsonFile_NonInteractive()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"),
            ("doc-2", Guid.Empty, "query { world }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var fileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("queries.json")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("queries.json")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json",
                "--format",
                "relay")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✓ Downloaded client to 'queries.json'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(fileStream.ToArray());
        Assert.Contains("doc-1", written);
        Assert.Contains("doc-2", written);
        Assert.Contains("query { hello }", written);
        Assert.Contains("query { world }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_RelayFormat_WritesJsonFile_JsonOutput()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"),
            ("doc-2", Guid.Empty, "query { world }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var fileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("queries.json")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("queries.json")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json",
                "--format",
                "relay")
            .ExecuteAsync();

        // assert
        result.AssertSuccess("{}");

        var written = Encoding.UTF8.GetString(fileStream.ToArray());
        Assert.Contains("doc-1", written);
        Assert.Contains("doc-2", written);
        Assert.Contains("query { hello }", written);
        Assert.Contains("query { world }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_RelayFormat_DeletesExistingFile()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var fileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("queries.json")).Returns(true);
        fileSystem.Setup(x => x.DeleteFile("queries.json"));
        fileSystem.Setup(x => x.CreateFile("queries.json")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "queries.json",
                "--format",
                "relay")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✓ Downloaded client to 'queries.json'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        fileSystem.Verify(x => x.DeleteFile("queries.json"), Times.Once);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_FolderFormat_WritesFiles_Interactive()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var docFileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists("output-dir")).Returns(false);
        fileSystem.Setup(x => x.CreateDirectory("output-dir"));
        fileSystem.Setup(x => x.FileExists(Path.Combine("output-dir", "doc-1.graphql"))).Returns(false);
        fileSystem.Setup(x => x.CreateFile(Path.Combine("output-dir", "doc-1.graphql"))).Returns(docFileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "output-dir",
                "--format",
                "folder")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Downloaded client to 'output-dir'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(docFileStream.ToArray());
        Assert.Equal("query { hello }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_FolderFormat_WritesFiles_NonInteractive()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var docFileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists("output-dir")).Returns(false);
        fileSystem.Setup(x => x.CreateDirectory("output-dir"));
        fileSystem.Setup(x => x.FileExists(Path.Combine("output-dir", "doc-1.graphql"))).Returns(false);
        fileSystem.Setup(x => x.CreateFile(Path.Combine("output-dir", "doc-1.graphql"))).Returns(docFileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "output-dir",
                "--format",
                "folder")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✓ Downloaded client to 'output-dir'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(docFileStream.ToArray());
        Assert.Equal("query { hello }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_FolderFormat_WritesFiles_JsonOutput()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var docFileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists("output-dir")).Returns(false);
        fileSystem.Setup(x => x.CreateDirectory("output-dir"));
        fileSystem.Setup(x => x.FileExists(Path.Combine("output-dir", "doc-1.graphql"))).Returns(false);
        fileSystem.Setup(x => x.CreateFile(Path.Combine("output-dir", "doc-1.graphql"))).Returns(docFileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "output-dir",
                "--format",
                "folder")
            .ExecuteAsync();

        // assert
        result.AssertSuccess("{}");

        var written = Encoding.UTF8.GetString(docFileStream.ToArray());
        Assert.Equal("query { hello }", written);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_FolderFormat_DeletesExistingFiles()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var docFileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists("output-dir")).Returns(true);
        fileSystem.Setup(x => x.FileExists(Path.Combine("output-dir", "doc-1.graphql"))).Returns(true);
        fileSystem.Setup(x => x.DeleteFile(Path.Combine("output-dir", "doc-1.graphql")));
        fileSystem.Setup(x => x.CreateFile(Path.Combine("output-dir", "doc-1.graphql"))).Returns(docFileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "output-dir",
                "--format",
                "folder")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✓ Downloaded client to 'output-dir'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(docFileStream.ToArray());
        Assert.Equal("query { hello }", written);
        fileSystem.Verify(x => x.DeleteFile(Path.Combine("output-dir", "doc-1.graphql")), Times.Once);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_FolderFormat_ExistingDirectory_SkipsCreate()
    {
        // arrange
        var queryStream = CreatePersistedQueryStream(
            ("doc-1", Guid.Empty, "query { hello }"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryStream);

        var docFileStream = new MemoryStream();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.DirectoryExists("output-dir")).Returns(true);
        fileSystem.Setup(x => x.FileExists(Path.Combine("output-dir", "doc-1.graphql"))).Returns(false);
        fileSystem.Setup(x => x.CreateFile(Path.Combine("output-dir", "doc-1.graphql"))).Returns(docFileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--path",
                "output-dir",
                "--format",
                "folder")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching queries...
            └── ✓ Downloaded client to 'output-dir'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        var written = Encoding.UTF8.GetString(docFileStream.ToArray());
        Assert.Equal("query { hello }", written);
        fileSystem.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    private static Stream CreatePersistedQueryStream(
        params (string DocumentId, Guid ApiId, string Content)[] queries)
    {
        var jsonArray = queries
            .Select(q => new { apiId = q.ApiId, documentIds = new[] { q.DocumentId }, content = q.Content });

        var json = JsonSerializer.Serialize(jsonArray);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    private static Mock<IClientsClient> CreateDownloadExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
