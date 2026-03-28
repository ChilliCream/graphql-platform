using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ValidateOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Validate_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient);

        // act
        var exitCode = await host.InvokeAsync("openapi", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--stage' is required.
            Option '--openapi-collection-id' is required.
            Option '--pattern' is required.


            """);
        openApiClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_NoMatchingFiles_ReturnsErrorWithoutClientCall()
    {
        // arrange
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var host = CreateHost(openApiClient);
        var pattern = $"does-not-exist-{Guid.NewGuid():N}/*.graphql";

        // act
        var exitCode = await host.InvokeAsync(
            "openapi",
            "validate",
            "--stage",
            "prod",
            "--openapi-collection-id",
            "openapi-1",
            "--pattern",
            pattern);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Empty(host.StdErr);
        openApiClient.VerifyNoOtherCalls();
    }

    private static CommandTestHost CreateHost(
        Mock<IOpenApiClient> openApiClient,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(openApiClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }
}
