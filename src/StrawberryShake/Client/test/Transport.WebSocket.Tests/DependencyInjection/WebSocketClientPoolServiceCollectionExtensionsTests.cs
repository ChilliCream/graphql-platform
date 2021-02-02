using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class WebSocketClientPoolServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddWebSocketClientPool_NonNullArgs_RegisterProtocol()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddSingleton<ISocketClientFactory>(new Mock<ISocketClientFactory>().Object);
            services.AddWebSocketClientPool();
            services.AddWebSocketClientPool();
            services.AddWebSocketClientPool();

            // assert
            Assert.Single(
                services.BuildServiceProvider()
                    .GetRequiredService<IEnumerable<ISocketClientPool>>());
        }

        [Fact]
        public void AddWebSocketClientPool_ServicesNull_ThrowException()
        {
            // arrange
            ServiceCollection services = null!;

            // act
            Exception? ex =
                Record.Exception(() => services.AddWebSocketClientPool());

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
