using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace StrawberryShake.Transport.WebSockets;

public class WebSocketClientPoolServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebSocketClientPool_NonNullArgs_RegisterProtocol()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddSingleton(new Mock<ISocketClientFactory>().Object);
        services.AddWebSocketClientPool();
        services.AddWebSocketClientPool();
        services.AddWebSocketClientPool();

        // assert
        Assert.Single(
            services.BuildServiceProvider()
                .GetRequiredService<IEnumerable<ISessionPool>>());
    }

    [Fact]
    public void AddWebSocketClientPool_ServicesNull_ThrowException()
    {
        // arrange
        ServiceCollection services = null!;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClientPool());

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }
}
