using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets;

public class WebSocketClientBuilderExtensionsTests
{
    [Fact]
    public void ConfigureWebSocketClient_NonNullArgs_ConfigureClient()
    {
        // arrange
        var name = "Foo";
        Action<ISocketClient> configure = x => throw new InvalidOperationException();
        var services = new ServiceCollection();
        var builder = new DefaultWebSocketClientBuilder(services, "Foo");

        // act
        builder.ConfigureWebSocketClient(configure);

        // assert
        var monitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();

        Assert.Single(monitor.Get(name).SocketClientActions);
    }

    [Fact]
    public void ConfigureWebSocketClient_BuilderNull_ThrowException()
    {
        Action<ISocketClient> configure = _ => throw new InvalidOperationException();
        DefaultWebSocketClientBuilder builder = null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureWebSocketClient_ConfigureNull_ThrowException()
    {
        Action<ISocketClient> configure = null!;
        var services = new ServiceCollection();
        var builder = new DefaultWebSocketClientBuilder(services, "Foo");

        // act
        var ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureWebSocketClientWithSp_NonNullArgs_ConfigureClient()
    {
        Action<IServiceProvider, ISocketClient> configure = (sp, c) =>
            throw new InvalidOperationException();
        var services = new ServiceCollection();
        var builder = new DefaultWebSocketClientBuilder(services, "Foo");

        // act
        builder.ConfigureWebSocketClient(configure);

        // assert
        var monitors = services
            .BuildServiceProvider()
            .GetRequiredService<IEnumerable<IConfigureOptions<SocketClientFactoryOptions>>>();

        Assert.Single(monitors);
    }

    [Fact]
    public void ConfigureWebSocketClientWithSp_BuilderNull_ThrowException()
    {
        Action<IServiceProvider, ISocketClient> configure = (sp, c) =>
            throw new InvalidOperationException();
        DefaultWebSocketClientBuilder builder = null!;

        // act
        var ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void ConfigureWebSocketClientWithSp_ConfigureNull_ThrowException()
    {
        Action<IServiceProvider, ISocketClient> configure = null!;
        var services = new ServiceCollection();
        var builder = new DefaultWebSocketClientBuilder(services, "Foo");

        // act
        var ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }
}
