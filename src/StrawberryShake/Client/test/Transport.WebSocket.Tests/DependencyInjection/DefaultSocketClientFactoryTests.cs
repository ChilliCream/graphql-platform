using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace StrawberryShake.Transport.WebSockets;

public class DefaultSocketClientFactoryTests
{
    [Fact]
    public void Constructor_AllArgs_CreateObject()
    {
        // arrange
        var optionsMonitor = new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
        var protocolFactories = Enumerable.Empty<ISocketProtocolFactory>();

        // act
        var exception = Record.Exception(() =>
            new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_MonitorNull_CreateObject()
    {
        // arrange
        IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor = null!;
        var protocolFactories =
            Enumerable.Empty<ISocketProtocolFactory>();

        // act
        var exception = Record.Exception(() =>
            new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Constructor_FactoriesNull_CreateObject()
    {
        // arrange
        var optionsMonitor =
            new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
        IEnumerable<ISocketProtocolFactory> protocolFactories = null!;

        // act
        var exception = Record.Exception(() =>
            new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void CreateClient_OptionsRegistered_CreateClient()
    {
        // arrange
        var sp = new ServiceCollection()
            .Configure<SocketClientFactoryOptions>(
                "Foo",
                x => { })
            .BuildServiceProvider();
        var optionsMonitor =
            sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
        var protocolFactories =
            Enumerable.Empty<ISocketProtocolFactory>();
        var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

        // act
        var client = factory.CreateClient("Foo");

        // assert
        Assert.IsType<WebSocketClient>(client);
    }

    [Fact]
    public void CreateClient_OptionsRegistered_ApplyConfig()
    {
        // arrange
        var uri = new Uri("wss://localhost:123");
        var sp = new ServiceCollection()
            .Configure<SocketClientFactoryOptions>(
                "Foo",
                x => x.SocketClientActions.Add(x => x.Uri = uri))
            .BuildServiceProvider();
        var optionsMonitor =
            sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
        var protocolFactories =
            Enumerable.Empty<ISocketProtocolFactory>();
        var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

        // act
        var client = factory.CreateClient("Foo");

        // assert
        Assert.Equal(uri, client.Uri);
    }

    [Fact]
    public void CreateClient_NoOptionsRegistered_CreateClient()
    {
        // arrange
        var sp = new ServiceCollection()
            .Configure<SocketClientFactoryOptions>(
                "Foo",
                x => { })
            .BuildServiceProvider();
        var optionsMonitor =
            sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
        var protocolFactories =
            Enumerable.Empty<ISocketProtocolFactory>();
        var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

        // act
        var client = factory.CreateClient("Baz");

        // assert
        Assert.IsType<WebSocketClient>(client);
    }
}
