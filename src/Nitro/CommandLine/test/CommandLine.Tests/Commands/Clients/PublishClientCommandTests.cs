using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class PublishClientCommandTests
{
    [Fact]
    public async Task Publish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "publish");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--client-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_WithForceAndApproval_ReturnsSuccess()
    {
        // arrange
        var sourceMetadata = CreateSourceMetadataJson();

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                "client-1",
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
            .ReturnsAsync(CreateClientPublishRequest("publish-1"));

        client.Setup(x => x.SubscribeToClientPublishAsync("publish-1", It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateQueuedUpdate(2),
                CreateReadyUpdate(),
                CreateSuccessUpdate()));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "publish",
            "--client-id",
            "client-1",
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
    public async Task Publish_WithJsonOutputAndNoResult_ReturnsEmptyObject()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                "client-1",
                "prod",
                "v2",
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPublishRequest("publish-json-1"));

        client.Setup(x => x.SubscribeToClientPublishAsync("publish-json-1", It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateQueuedUpdate(1),
                CreateReadyUpdate(),
                CreateSuccessUpdate()));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "publish",
            "--client-id",
            "client-1",
            "--stage",
            "prod",
            "--tag",
            "v2",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {}
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Publish_FailedUpdate_ReturnsError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                "client-1",
                "prod",
                "v2",
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientPublishRequest("publish-2"));

        client.Setup(x => x.SubscribeToClientPublishAsync("publish-2", It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateFailedUpdate("Deployment failed", "Persisted query conflict")));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "publish",
            "--client-id",
            "client-1",
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
            Client publish failed

            Deployment failed
            Persisted query conflict
            Publishing...
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static async IAsyncEnumerable<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate>
        ToPublishUpdates(
            params IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IPublishClientVersion_PublishClient CreateClientPublishRequest(string requestId)
    {
        var mock = new Mock<IPublishClientVersion_PublishClient>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsQueued
        CreateQueuedUpdate(int queuePosition)
    {
        var update = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsQueued>();
        update.SetupGet(x => x.QueuePosition).Returns(queuePosition);
        return update.Object;
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsReady
        CreateReadyUpdate()
        => Mock.Of<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsReady>();

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishSuccess
        CreateSuccessUpdate()
        => Mock.Of<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishSuccess>();

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishFailed
        CreateFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandTestHost CreateHost(
        Mock<IClientsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IClientsClient>(client.Object)
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
