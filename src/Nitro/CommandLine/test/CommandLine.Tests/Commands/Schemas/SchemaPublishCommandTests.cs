using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class SchemaPublishCommandTests
{
    [Fact]
    public async Task Publish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("schema", "publish");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--api-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_WithForceAndApproval_ReturnsSuccess()
    {
        // arrange
        var sourceMetadata = CreateSourceMetadataJson();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaPublishAsync(
                "api-1",
                "prod",
                "v2",
                true,
                true,
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
            .ReturnsAsync(CreateSchemaPublishRequest("publish-1"));

        client.Setup(x => x.SubscribeToSchemaPublishAsync("publish-1", It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateQueuedUpdate(3),
                CreateReadyUpdate(),
                CreateSuccessUpdate()));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "publish",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v2",
            "--force",
            "--wait-for-approval",
            "true",
            "--source-metadata",
            sourceMetadata);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Publish_FailedUpdate_ReturnsError()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaPublishAsync(
                "api-1",
                "prod",
                "v2",
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSchemaPublishRequest("publish-2"));

        client.Setup(x => x.SubscribeToSchemaPublishAsync("publish-2", It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateFailedUpdate("Deployment failed", "Schema invalid")));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "publish",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--tag",
            "v2");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Initialized
            LOG: Create publish request
            LOG: Publish request created (ID: publish-2)
            Schema publish failed

            Deployment failed
            Schema invalid
            Publishing...
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static async IAsyncEnumerable<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate>
        ToPublishUpdates(
            params IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IPublishSchemaVersion_PublishSchema CreateSchemaPublishRequest(string requestId)
    {
        var mock = new Mock<IPublishSchemaVersion_PublishSchema>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsQueued
        CreateQueuedUpdate(int queuePosition)
    {
        var update = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsQueued>();
        update.SetupGet(x => x.QueuePosition).Returns(queuePosition);
        return update.Object;
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsReady
        CreateReadyUpdate()
        => Mock.Of<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsReady>();

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishSuccess
        CreateSuccessUpdate()
        => Mock.Of<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishSuccess>();

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishFailed
        CreateFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandTestHost CreateHost(
        Mock<ISchemasClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<ISchemasClient>(client.Object)
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
}
