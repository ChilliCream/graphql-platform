using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ListOpenApiCollectionCommandTests
{
    [Fact]
    public async Task List_MissingApiId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "list",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The API ID is required in non-interactive mode.
            """);
        openApiClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var page = new ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>(
            [CreateCollectionNode("openapi-1", "petstore")],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "list",
            "--api-id",
            "api-1",
            "--cursor",
            "cursor-start",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "petstore"
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        Assert.Empty(host.StdErr);
        openApiClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_InteractivePath_UsesPagedTableBranch()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>(
                [],
                EndCursor: null,
                HasNextPage: false));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient, apisClient);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "list",
            "--api-id",
            "api-1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        openApiClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    private static CommandBuilder CreateHost(
        Mock<IOpenApiClient> openApiClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(openApiClient.Object)
            .AddService(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node_OpenApiCollection
        CreateCollectionNode(
            string id,
            string name)
    {
        var collection = new Mock<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node_OpenApiCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        return collection.Object;
    }
}
