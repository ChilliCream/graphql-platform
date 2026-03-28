using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class ShowApiCommandTests
{
    [Fact]
    public async Task Show_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("api", "show");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Show_WithData_JsonOutput_ReturnsApi()
    {
        // arrange
        var api = ApiCommandTestHelper.CreateShowApiNode("api-1", "products", ["catalog", "products"]);
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowApiAsync("api-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(api);

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "show",
            "api-1",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "api-1",
              "name": "products",
              "path": "catalog/products",
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

    [Fact]
    public async Task Show_WithoutData_ReturnsSuccessAndErrorMessage()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowApiAsync("api-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowApiCommandQuery_Node?)null);

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "show",
            "api-missing");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ Could not find an API with ID api-missing
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }
}
