using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class CreateOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspaceAndApi_ReturnsError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient, apisClient, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "create",
            "--name",
            "petstore",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or specify the workspace ID with the --workspace-id option (if available).
            """);
        openApiClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithApiIdAndName_JsonOutput_ReturnsCollection()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "petstore",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCollectionResult("openapi-1", "petstore"));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "create",
            "--api-id",
            "api-1",
            "--name",
            "petstore",
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

    private static TestSessionService NoSession() => new();

    private static ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection CreateCollectionResult(
        string id,
        string name)
    {
        var collection = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection_OpenApiCollection>();
        collection.SetupGet(x => x.Id).Returns(id);
        collection.SetupGet(x => x.Name).Returns(name);

        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>();
        payload.SetupGet(x => x.OpenApiCollection).Returns(collection.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
