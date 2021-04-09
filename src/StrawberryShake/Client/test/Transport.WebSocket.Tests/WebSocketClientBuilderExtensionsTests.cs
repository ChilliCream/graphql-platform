using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
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
            IOptionsMonitor<SocketClientFactoryOptions> monitor = services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();

            Assert.Single(monitor.Get(name).SocketClientActions);
        }

        [Fact]
        public void ConfigureWebSocketClient_BuilderNull_ThrowException()
        {
            // arrange
            var name = "Foo";
            Action<ISocketClient> configure = _ => throw new InvalidOperationException();
            DefaultWebSocketClientBuilder builder = null!;

            // act
            Exception? ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void ConfigureWebSocketClient_ConfigureNull_ThrowException()
        {
            // arrange
            var name = "Foo";
            Action<ISocketClient> configure = null!;
            var services = new ServiceCollection();
            var builder = new DefaultWebSocketClientBuilder(services, "Foo");

            // act
            Exception? ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void ConfigureWebSocketClientWithSp_NonNullArgs_ConfigureClient()
        {
            // arrange
            var name = "Foo";
            Action<IServiceProvider, ISocketClient> configure = (sp, c) =>
                throw new InvalidOperationException();
            var services = new ServiceCollection();
            var builder = new DefaultWebSocketClientBuilder(services, "Foo");

            // act
            builder.ConfigureWebSocketClient(configure);

            // assert
            IEnumerable<IConfigureOptions<SocketClientFactoryOptions>> monitors = services
                .BuildServiceProvider()
                .GetRequiredService<IEnumerable<IConfigureOptions<SocketClientFactoryOptions>>>();

            Assert.Single(monitors);
        }

        [Fact]
        public void ConfigureWebSocketClientWithSp_BuilderNull_ThrowException()
        {
            // arrange
            var name = "Foo";
            Action<IServiceProvider, ISocketClient> configure = (sp, c) =>
                throw new InvalidOperationException();
            DefaultWebSocketClientBuilder builder = null!;

            // act
            Exception? ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void ConfigureWebSocketClientWithSp_ConfigureNull_ThrowException()
        {
            // arrange
            var name = "Foo";
            Action<IServiceProvider, ISocketClient> configure = null!;
            var services = new ServiceCollection();
            var builder = new DefaultWebSocketClientBuilder(services, "Foo");

            // act
            Exception? ex = Record.Exception(() => builder.ConfigureWebSocketClient(configure));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
