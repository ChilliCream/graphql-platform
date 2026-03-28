using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;
using SourceMetadata = ChilliCream.Nitro.Client.SourceMetadata;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionUploadCommandTests
{
    [Fact]
    public async Task Upload_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("fusion", "upload");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--tag' is required.
            Option '--source-schema-file' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_WithSourceMetadata_UploadsSubgraph()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadFusionSubgraphAsync(
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
            .ReturnsAsync(CreateUploadFusionSubgraphResult("version-1"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "upload",
            "--api-id",
            "api-1",
            "--tag",
            "v1",
            "--working-directory",
            "__resources__/valid-example-1",
            "--source-schema-file",
            "source-schema-1.graphqls",
            "--source-metadata",
            CreateSourceMetadataJson());

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Upload_WhenVersionIdIsEmpty_ReturnsError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadFusionSubgraphAsync(
                "api-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUploadFusionSubgraphResult(string.Empty));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "upload",
            "--api-id",
            "api-1",
            "--tag",
            "v1",
            "--working-directory",
            "__resources__/valid-example-1",
            "--source-schema-file",
            "source-schema-1.graphqls");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Uploading source schema at
            '__resources__/valid-example-1/source-schema-1.graphqls'...
            Uploading source schema...
            Upload of source schema failed!
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IFusionConfigurationClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

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

    private static IUploadFusionSubgraph_UploadFusionSubgraph CreateUploadFusionSubgraphResult(
        string versionId)
    {
        var version = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph_FusionSubgraphVersion>();
        version.SetupGet(x => x.Id).Returns(versionId);

        var result = new Mock<IUploadFusionSubgraph_UploadFusionSubgraph>();
        result.SetupGet(x => x.FusionSubgraphVersion).Returns(version.Object);
        result.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadFusionSubgraph_UploadFusionSubgraph_Errors>?)null);
        return result.Object;
    }
}
