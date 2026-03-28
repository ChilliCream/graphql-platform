using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class SetApiSettingsCommandTests
{
    [Fact]
    public async Task SetSettings_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("api", "set-settings");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'set-settings'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetSettings_WithExplicitOptions_JsonOutput_ReturnsApi()
    {
        // arrange
        var updatedApi = ApiCommandTestHelper.CreateSetApiSettingsResult(
            "api-1",
            "products",
            ["catalog"],
            treatDangerousAsBreaking: true,
            allowBreakingSchemaChanges: true);

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedApi);

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "set-settings",
            "api-1",
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "true",
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
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": true
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task SetSettings_ClientThrowsNitroClientException_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateApiSettingsAsync(
                "api-1",
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("update failed"));

        var host = ApiCommandTestHelper.CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "set-settings",
            "api-1",
            "--treat-dangerous-as-breaking",
            "true",
            "--allow-breaking-schema-changes",
            "false",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Set settings for API api-1

            update failed
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }
}
