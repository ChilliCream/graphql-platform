using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class DefaultSocketClientFactoryTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_MonitorNull_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor = null!;
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Constructor_FactoriesNull_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
            IEnumerable<ISocketProtocolFactory> protocolFactories = null!;

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void CreateClient_OptionsRegistered_CreateClient()
        {
            // arrange
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => { })
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Foo");

            // assert
            Assert.IsType<WebSocketClient>(client);
        }

        [Fact]
        public void CreateClient_OptionsRegistered_ApplyConfig()
        {
            // arrange
            var uri = new Uri("wss://localhost:123");
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => x.SocketClientActions.Add(x => x.Uri = uri))
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Foo");

            // assert
            Assert.Equal(uri, client.Uri);
        }

        [Fact]
        public void CreateClient_NoOptionsRegistered_CreateClient()
        {
            // arrange
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => { })
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Baz");

            // assert
            Assert.IsType<WebSocketClient>(client);
        }
    }
}
