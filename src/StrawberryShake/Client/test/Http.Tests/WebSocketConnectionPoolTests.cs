using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.Http
{
    public class WebSocketConnectionPoolTests
    {
        [Fact]
        public async Task Rent_Single_Connection()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                // arrange
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();

                // act
                ISocketConnection connection =
                    await connectionPool.RentAsync("Foo");

                // assert
                Assert.False(connection.IsClosed);
            }
        }

        [Fact]
        public async Task Return_Last_Connection()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                // arrange
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();
                ISocketConnection connection =
                    await connectionPool.RentAsync("Foo");

                // act
                await connectionPool.ReturnAsync(connection);

                // assert
                Assert.True(connection.IsClosed);
            }
        }

        [Fact]
        public async Task Rent_Two_Connections()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                // arrange
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();
                ISocketConnection firstConnection =
                    await connectionPool.RentAsync("Foo");

                // act
                ISocketConnection secondConnection =
                    await connectionPool.RentAsync("Foo");

                // assert
                Assert.Equal(firstConnection, secondConnection);
            }
        }

        [Fact]
        public async Task Return_One_Of_Many_Connections()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                // arrange
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();
                await connectionPool.RentAsync("Foo");
                ISocketConnection connection =
                    await connectionPool.RentAsync("Foo");

                // act
                await connectionPool.ReturnAsync(connection);

                // assert
                Assert.False(connection.IsClosed);
            }
        }

        [Fact]
        public async Task Dispose_Connection_Pool()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                // arrange
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();
                ISocketConnection connection =
                    await connectionPool.RentAsync("Foo");

                // act
                connectionPool.Dispose();

                // assert
                Assert.True(connection.IsClosed);
            }
        }
    }
}
