using System.Text;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class SchemaDownloadCommandTests
{
    [Fact]
    public async Task Download_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("schema", "download");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--stage' is required.
            Option '--file' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_WithStream_WritesFile()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputPath = CreateFilePath(".graphql");
        const string schemaText = "type Query { hello: String }";

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(schemaText)));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--file",
            outputPath);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(schemaText, await fileSystem.ReadAllTextAsync(outputPath, CancellationToken.None));
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Download_WhenNoSchemaExists_ReturnsError()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputPath = CreateFilePath(".graphql");

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--file",
            outputPath);

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Fetching Schema...
            """);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Could not find a published schema on stage prod
            """);
        client.VerifyAll();
        Assert.False(fileSystem.Files.ContainsKey(Path.GetFullPath(outputPath)));
    }

    private static CommandBuilder CreateHost(
        Mock<ISchemasClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<ISchemasClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static TestFileSystem CreateOutputFileSystem()
        => new();
}
