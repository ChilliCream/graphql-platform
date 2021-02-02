using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class WebSocketClientFactoryServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddProtocol_NonNullArgs_RegisterProtocol()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddProtocol<GraphQlWsProtocolFactory>();

            // assert
            Assert.Single(
                services.BuildServiceProvider()
                    .GetRequiredService<IEnumerable<ISocketProtocolFactory>>());
        }

        [Fact]
        public void AddProtocol_SerivcesNull_ThrowException()
        {
            // arrange
            ServiceCollection services = null!;

            // act
            Exception? ex =
                Record.Exception(() => services.AddProtocol<GraphQlWsProtocolFactory>());

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddWebSocketClient_NonNullArgs_RegisterProtocol()
        {
            // arrange
            var clientName = "Foo";
            var services = new ServiceCollection();

            // act
            IWebSocketClientBuilder builder = services.AddWebSocketClient(clientName);

            // assert
            ISocketClient client = services.BuildServiceProvider()
                .GetService<ISocketClientFactory>()
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
            Exception? ex =
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
            Exception? ex =
                Record.Exception(() => services.AddWebSocketClient(null!));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddWebSocketClientWithConfigure_NonNullArgs_RegisterProtocol()
        {
            // arrange
            var clientName = "Foo";
            var services = new ServiceCollection();
            var uri = new Uri("wss://localhost:1234");
            Action<ISocketClient> configure = x => x.Uri = uri;

            // act
            IWebSocketClientBuilder builder =
                services.AddWebSocketClient(clientName, configure);

            // assert
            ISocketClient client = services.BuildServiceProvider()
                .GetService<ISocketClientFactory>()
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
            Action<ISocketClient> configure = x => x.Uri = uri;

            // act
            Exception? ex =
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
            Action<ISocketClient> configure = x => x.Uri = uri;

            // act
            Exception? ex =
                Record.Exception(() => services.AddWebSocketClient(null!, configure));

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
            Exception? ex =
                Record.Exception(() => services.AddWebSocketClient("Foo", configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void AddWebSocketClientWithConfigureAndSp_NonNullArgs_RegisterProtocol()
        {
            // arrange
            var clientName = "Foo";
            var services = new ServiceCollection();
            var uri = new Uri("wss://localhost:1234");
            Action<IServiceProvider, ISocketClient> configure = (_, x) => x.Uri = uri;

            // act
            IWebSocketClientBuilder builder =
                services.AddWebSocketClient(clientName, configure);

            // assert
            ISocketClient client = services.BuildServiceProvider()
                .GetService<ISocketClientFactory>()
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
            Exception? ex =
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
            Exception? ex =
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
            Exception? ex =
                Record.Exception(() => services.AddWebSocketClient("Foo", configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
