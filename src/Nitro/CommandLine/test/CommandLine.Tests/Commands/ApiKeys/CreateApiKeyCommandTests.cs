using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class CreateApiKeyCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspaceAndApi_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var host = ApiKeyCommandTestHelper.CreateHost(client, session: new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "create",
            "--name",
            "key-1",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """

            Creating an API key...

            The workspace ID or API ID is required in non-interactive mode.
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithWorkspaceOption_JsonOutput_ReturnsResult()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "ws-1",
                null,
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var host = ApiKeyCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "create",
            "--workspace-id",
            "ws-1",
            "--name",
            "integration",
            "--stage-condition",
            "prod",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "integration",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Create_WithApiId_UsesDefaultWorkspaceFromSession()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var host = ApiKeyCommandTestHelper.CreateHost(
            client,
            TestSessionService.WithWorkspace("workspace-from-session"));

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "create",
            "--api-id",
            "api-1",
            "--name",
            "tenant-key",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
        client.VerifyAll();
    }

    [Fact]
    public async Task Create_ClientThrowsNitroClientException_ReturnsError()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "ws-1",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientException("create failed"));

        var host = ApiKeyCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api-key",
            "create",
            "--workspace-id",
            "ws-1",
            "--name",
            "broken",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """

            Creating an API key...

            create failed
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }
}
