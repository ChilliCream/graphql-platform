using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class CreateApiCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspace_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client, session: new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "create",
            "--name",
            "products",
            "--path",
            "/products",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `nitro login` to sign in or specify the workspace ID with the --workspace-id option (if available).
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_InvalidKind_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        var host = ApiCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "create",
            "--workspace-id",
            "ws-1",
            "--name",
            "products",
            "--path",
            "/products",
            "--kind",
            "invalid");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Argument 'invalid' not recognized. Must be one of:
            	'collection'
            	'service'
            	'gateway'
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithOptions_JsonOutput_ReturnsApi()
    {
        // arrange
        var api = ApiCommandTestHelper.CreateCreateApiResult("api-1", "products", ["catalog", "products"]);
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "catalog", "products" })),
                "products",
                ChilliCream.Nitro.Client.ApiKind.Service,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(api);

        var host = ApiCommandTestHelper.CreateHost(client, session: null);

        // act
        var exitCode = await host.InvokeAsync(
            "api",
            "create",
            "--workspace-id",
            "ws-1",
            "--name",
            "products",
            "--path",
            "/catalog/products",
            "--kind",
            "service",
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
}
