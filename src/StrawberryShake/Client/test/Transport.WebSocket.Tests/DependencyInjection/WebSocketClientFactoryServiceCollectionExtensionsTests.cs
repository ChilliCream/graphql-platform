using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets.Protocols;

namespace StrawberryShake.Transport.WebSockets;

public class WebSocketClientFactoryServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProtocol_NonNullArgs_RegisterProtocol()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddProtocol<GraphQLWebSocketProtocolFactory>();

        // assert
        Assert.Single(
            services.BuildServiceProvider()
                .GetRequiredService<IEnumerable<ISocketProtocolFactory>>());
    }

    [Fact]
    public void AddProtocol_ServicesNull_ThrowException()
    {
        // arrange
        ServiceCollection services = null!;

        // act
        var ex =
            Record.Exception(() => services.AddProtocol<GraphQLWebSocketProtocolFactory>());

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClient_NonNullArgs_RegisterProtocol()
    {
        // arrange
        const string clientName = "Foo";
        var services = new ServiceCollection();

        // act
        var builder = services.AddWebSocketClient(clientName);

        // assert
        var client = services.BuildServiceProvider()
            .GetRequiredService<ISocketClientFactory>()
            .CreateClient(clientName);
        Assert.Equal(clientName, builder.Name);
        Assert.Equal(clientName, client.Name);
    }

    [Fact]
    public void AddWebSocketClient_ServicesNull_ThrowException()
    {
        // arrange
        ServiceCollection services = null!;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient("Foo"));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClient_NameNull_ThrowException()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient(null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigure_NonNullArgs_RegisterProtocol()
    {
        // arrange
        const string clientName = "Foo";
        var services = new ServiceCollection();
        var uri = new Uri("wss://localhost:1234");
        Action<ISocketClient> configure = x => x.Uri = uri;

        // act
        var builder =
            services.AddWebSocketClient(clientName, configure);

        // assert
        var client = services.BuildServiceProvider()
            .GetRequiredService<ISocketClientFactory>()
            .CreateClient(clientName);
        Assert.Equal(clientName, builder.Name);
        Assert.Equal(clientName, client.Name);
        Assert.Equal(uri, client.Uri);
    }

    [Fact]
    public void AddWebSocketClientWithConfigure_ServicesNull_ThrowException()
    {
        // arrange
        ServiceCollection services = null!;
        var uri = new Uri("wss://localhost:1234");
        void configure(ISocketClient x) => x.Uri = uri;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient("Foo", configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigure_NameNull_ThrowException()
    {
        // arrange
        var services = new ServiceCollection();
        var uri = new Uri("wss://localhost:1234");
        void configure(ISocketClient x) => x.Uri = uri;

        // act
        var ex = Record.Exception(() => services.AddWebSocketClient(null!, configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigure_ConfigureNull_ThrowException()
    {
        // arrange
        var services = new ServiceCollection();
        Action<ISocketClient> configure = null!;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient("Foo", configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigureAndSp_NonNullArgs_RegisterProtocol()
    {
        // arrange
        const string clientName = "Foo";
        var services = new ServiceCollection();
        var uri = new Uri("wss://localhost:1234");
        void configure(IServiceProvider _, ISocketClient x) => x.Uri = uri;

        // act
        var builder =
            services.AddWebSocketClient(clientName, configure);

        // assert
        var client = services.BuildServiceProvider()
            .GetRequiredService<ISocketClientFactory>()
            .CreateClient(clientName);
        Assert.Equal(clientName, builder.Name);
        Assert.Equal(clientName, client.Name);
        Assert.Equal(uri, client.Uri);
    }

    [Fact]
    public void AddWebSocketClientWithConfigureAndSp_ServicesNull_ThrowException()
    {
        // arrange
        ServiceCollection services = null!;
        var uri = new Uri("wss://localhost:1234");
        Action<IServiceProvider, ISocketClient> configure = (_, x) => x.Uri = uri;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient("Foo", configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigureAndSp_NameNull_ThrowException()
    {
        // arrange
        var services = new ServiceCollection();
        var uri = new Uri("wss://localhost:1234");
        Action<IServiceProvider, ISocketClient> configure = (_, x) => x.Uri = uri;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient(null!, configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void AddWebSocketClientWithConfigureAndSp_ConfigureNull_ThrowException()
    {
        // arrange
        var services = new ServiceCollection();
        Action<IServiceProvider, ISocketClient> configure = null!;

        // act
        var ex =
            Record.Exception(() => services.AddWebSocketClient("Foo", configure));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }
}
