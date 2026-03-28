using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UploadClientCommandTests
{
    [Fact]
    public async Task Upload_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "upload");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--operations-file' is required.
            Option '--client-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_WithSourceMetadata_UsesClient()
    {
        // arrange
        var operationsPath = CreateFilePath(".json");
        var fileSystem = CreateInputFileSystem((operationsPath, "{\"doc-1\":\"query A { field }\"}"));
        var sourceMetadata = CreateSourceMetadataJson();

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadClientVersionAsync(
                "client-1",
                "v1",
                It.IsAny<Stream>(),
                It.Is<SourceMetadata?>(source =>
                    source != null
                    && source.GitHub != null
                    && source.GitHub.Actor == "actor-1"
                    && source.GitHub.CommitHash == "commit-1"
                    && source.GitHub.WorkflowName == "deploy"
                    && source.GitHub.RunNumber == "42"
                    && source.GitHub.RunId == "run-9"
                    && source.GitHub.JobId == "job-2"
                    && source.GitHub.RepositoryUrl == new Uri("https://github.com/chillicream/platform")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IUploadClient_UploadClient>());

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "upload",
            "--client-id",
            "client-1",
            "--tag",
            "v1",
            "--operations-file",
            operationsPath,
            "--source-metadata",
            sourceMetadata);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Upload_WhenOperationsFileDoesNotExist_ReturnsError()
    {
        // arrange
        var fileSystem = CreateInputFileSystem();
        const string missingOperationsPath = "/tmp/nitro-missing-operations.json";

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "upload",
            "--client-id",
            "client-1",
            "--tag",
            "v1",
            "--operations-file",
            missingOperationsPath);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(
            $"""
             LOG: Initialized
             LOG: Reading file {missingOperationsPath}
             Uploading operations...
              File {missingOperationsPath} was not found!
             
             """.ReplaceLineEndings("\n"),
            host.Output.ReplaceLineEndings("\n"));
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    private static CommandTestHost CreateHost(
        Mock<IClientsClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IClientsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateSourceMetadataJson()
        =>
            """
            {
              "actor": "actor-1",
              "commitHash": "commit-1",
              "workflowName": "deploy",
              "runNumber": "42",
              "runId": "run-9",
              "jobId": "job-2",
              "repositoryUrl": "https://github.com/chillicream/platform"
            }
            """;

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static TestFileSystem CreateInputFileSystem(params (string path, string content)[] seededFiles)
        => new(seededFiles.Select(x => new KeyValuePair<string, string>(x.path, x.content)).ToArray());
}
