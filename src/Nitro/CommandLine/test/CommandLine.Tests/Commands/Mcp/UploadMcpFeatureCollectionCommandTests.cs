using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class UploadMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Upload_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync("mcp", "upload");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--mcp-feature-collection-id' is required.
            """);
        mcpClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_NoMatchingFiles_ReturnsSuccessWithoutClientCall()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);
        var promptPattern = $"does-not-exist-{Guid.NewGuid():N}/*.json";
        var toolPattern = $"does-not-exist-{Guid.NewGuid():N}/*.graphql";

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "upload",
            "--tag",
            "v1",
            "--mcp-feature-collection-id",
            "mcp-1",
            "--prompt-pattern",
            promptPattern,
            "--tool-pattern",
            toolPattern);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyNoOtherCalls();
    }

    private static CommandBuilder CreateHost(
        Mock<IMcpClient> mcpClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(mcpClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
