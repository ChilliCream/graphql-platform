using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ValidateMcpFeatureCollectionCommandTests
{
    [Fact]
    public async Task Validate_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);

        // act
        var exitCode = await host.InvokeAsync("mcp", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--stage' is required.
            Option '--mcp-feature-collection-id' is required.


            """);
        mcpClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_NoMatchingFiles_ReturnsErrorWithoutClientCall()
    {
        // arrange
        var mcpClient = new Mock<IMcpClient>(MockBehavior.Strict);
        var host = CreateHost(mcpClient);
        var promptPattern = $"does-not-exist-{Guid.NewGuid():N}/*.json";
        var toolPattern = $"does-not-exist-{Guid.NewGuid():N}/*.graphql";

        // act
        var exitCode = await host.InvokeAsync(
            "mcp",
            "validate",
            "--stage",
            "prod",
            "--mcp-feature-collection-id",
            "mcp-1",
            "--prompt-pattern",
            promptPattern,
            "--tool-pattern",
            toolPattern);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Empty(host.StdErr);
        mcpClient.VerifyNoOtherCalls();
    }

    private static CommandTestHost CreateHost(
        Mock<IMcpClient> mcpClient,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(mcpClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
