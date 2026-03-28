using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class SchemaUploadCommandTests
{
    [Fact]
    public async Task Upload_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("schema", "upload");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--schema-file' is required.
            Option '--api-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_WithSourceMetadata_UsesClient()
    {
        // arrange
        var schemaPath = CreateFilePath(".graphql");
        var fileSystem = CreateInputFileSystem((schemaPath, "type Query { hello: String }"));
        var sourceMetadata = CreateSourceMetadataJson();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadSchemaAsync(
                "api-1",
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
            .ReturnsAsync(Mock.Of<IUploadSchema_UploadSchema>());

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "upload",
            "--api-id",
            "api-1",
            "--tag",
            "v1",
            "--schema-file",
            schemaPath,
            "--source-metadata",
            sourceMetadata);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Upload_WhenSchemaFileDoesNotExist_ReturnsError()
    {
        // arrange
        var fileSystem = CreateInputFileSystem();
        const string missingSchemaPath = "/tmp/nitro-missing-schema.graphql";

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "upload",
            "--api-id",
            "api-1",
            "--tag",
            "v1",
            "--schema-file",
            missingSchemaPath);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(
            $"""
                LOG: Initialized
                LOG: Reading file {missingSchemaPath}
                Uploading schema...

                """.Trim(),
            host.Output.Trim());
        host.StdErr.Trim().MatchInlineSnapshot(
            $"""
            [red] File {missingSchemaPath} was not found![/]
            """);
        client.VerifyNoOtherCalls();
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
