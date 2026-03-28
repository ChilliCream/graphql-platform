using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class DeleteOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Delete_MissingId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "delete",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The OpenAPI collection ID is required in non-interactive mode.
            """);
        openApiClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_WithIdAndForce_JsonOutput_ReturnsDeletedCollection()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.DeleteOpenApiCollectionAsync("openapi-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCollectionResult("openapi-1", "petstore"));

        var host = CreateHost(openApiClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "delete",
            "openapi-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "openapi-1",
              "name": "petstore"
            }
            """);
        Assert.Empty(host.StdErr);
        openApiClient.VerifyAll();
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

    private static IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById CreateCollectionResult(
        string id,
        string name)
    {
        var collection = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection_OpenApiCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>();
        payload.SetupGet(x => x.OpenApiCollection).Returns(collection.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
