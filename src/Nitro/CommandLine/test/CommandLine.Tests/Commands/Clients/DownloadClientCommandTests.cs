using System.Text;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DownloadClientCommandTests
{
    [Fact]
    public async Task Download_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "download");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--stage' is required.
            Option '--output' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_RelayFormat_WritesRelayJsonFile()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputFile = CreateFilePath(".json");
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePersistedQueryStream());

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output",
            outputFile,
            "--format",
            "relay");

        // assert
        Assert.Equal(0, exitCode);
        (await fileSystem.ReadAllTextAsync(outputFile, CancellationToken.None)).ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            {
              "doc-1": "query A { field }",
              "doc-2": "query A { field }",
              "doc-3": "query B { other }"
            }
        """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Download_FolderFormat_WritesGraphqlFiles()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputDirectory = CreateDirectoryPath();
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePersistedQueryStream());

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output",
            outputDirectory,
            "--format",
            "folder");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("query A { field }", await fileSystem.ReadAllTextAsync(Path.Combine(outputDirectory, "doc-1.graphql"), CancellationToken.None));
        Assert.Equal("query A { field }", await fileSystem.ReadAllTextAsync(Path.Combine(outputDirectory, "doc-2.graphql"), CancellationToken.None));
        Assert.Equal("query B { other }", await fileSystem.ReadAllTextAsync(Path.Combine(outputDirectory, "doc-3.graphql"), CancellationToken.None));
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Download_WhenNoPublishedClientExists_ReturnsError()
    {
        // arrange
        var fileSystem = new TestFileSystem();
        var outputFile = CreateFilePath(".json");
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadPersistedQueriesAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output",
            outputFile);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Fetching queries...
            """);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Could not find a published client on stage prod
            """);
        client.VerifyAll();
        Assert.False(fileSystem.Files.ContainsKey(Path.GetFullPath(outputFile)));
    }

    private static Stream CreatePersistedQueryStream()
    {
        const string json =
            """
            [
              {
                "apiId": "11111111-1111-1111-1111-111111111111",
                "documentIds": [ "doc-1", "doc-2" ],
                "content": "query A { field }"
              },
              {
                "apiId": "22222222-2222-2222-2222-222222222222",
                "documentIds": [ "doc-3" ],
                "content": "query B { other }"
              }
            ]
            """;

        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    private static CommandBuilder CreateHost(
        Mock<IClientsClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IClientsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static string CreateDirectoryPath()
        => Path.Combine("/tmp", $"client-download-{Path.GetRandomFileName()}");

    private static TestFileSystem CreateOutputFileSystem() => new();
}
