namespace Mocha.Hosting.Tests.Health;

public sealed class HealthRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_ReturnOKMessage_When_Called()
    {
        // Arrange
        var handler = new HealthRequestHandler();
        var request = new HealthRequest("Health Check");

        // Act
        var response = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("OK", response.Message);
    }
}
