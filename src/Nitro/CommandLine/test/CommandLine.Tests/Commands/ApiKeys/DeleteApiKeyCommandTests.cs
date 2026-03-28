using ChilliCream.Nitro.Client.ApiKeys;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class DeleteApiKeyCommandTests
{
    [Fact]
    public async Task Delete_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var host = ApiKeyCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("api-key", "delete", "--force");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'delete'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_Force_JsonOutput_ReturnsDeletedApiKey()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteApiKeyAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateDeleteApiKeyResult("key-1", "to-delete", "Workspace"));

        var host = ApiKeyCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "delete",
            "key-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "key-1",
              "name": "to-delete",
              "workspace": {
                "name": "Workspace"
              }
            }
            """);
        client.VerifyAll();
    }
}
