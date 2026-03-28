using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class PublishOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Publish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient);

        // act
        var exitCode = await host.InvokeAsync("openapi", "publish");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--openapi-collection-id' is required.
            """);
        openApiClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Publish_FailedUpdate_ReturnsError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.StartOpenApiCollectionPublishAsync(
                "openapi-1",
                "prod",
                "v1",
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOpenApiCollectionPublishRequest("publish-1"));

        openApiClient.Setup(x => x.SubscribeToOpenApiCollectionPublishAsync(
                "publish-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToPublishUpdates(
                CreateFailedUpdate("Publish failed", "Invalid document")));

        var host = CreateHost(openApiClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "publish",
            "--tag",
            "v1",
            "--stage",
            "prod",
            "--openapi-collection-id",
            "openapi-1");

        // assert
        Assert.Equal(1, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            LOG: Create publish request
            LOG: Publish request created (ID: publish-1)
            OpenAPI collection publish failed

            Publish failed
            Invalid document
            Publishing...
            """);
        Assert.Empty(host.StdErr);
        openApiClient.VerifyAll();
    }

    private static async IAsyncEnumerable<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate>
        ToPublishUpdates(
            params IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection
        CreateOpenApiCollectionPublishRequest(string requestId)
    {
        var mock = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishFailed
        CreateFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandBuilder CreateHost(
        Mock<IOpenApiClient> openApiClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(openApiClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
