using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class DeleteApiCommandTests
{
    [Fact]
    public async Task Delete_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("api", "delete", "--force");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'delete'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ApiNotFound_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync("api-404", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDeleteApiCommandQuery_Node?)null);

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "delete",
            "api-404",
            "--force",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            API with ID api-404 was not found
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Delete_Force_JsonOutput_ReturnsDeletedApi()
    {
        // arrange
        var deletedApi = ApiCommandTestHelper.CreateDeleteApiResult("api-1", "products", ["catalog"]);
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.GetApiForDeleteAsync("api-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCommandTestHelper.CreateDeleteApiSelection("products"));
        client.Setup(x => x.DeleteApiAsync("api-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedApi);

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "delete",
            "api-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "api-1",
              "name": "products",
              "path": "catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": false,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
        client.VerifyAll();
    }
}
