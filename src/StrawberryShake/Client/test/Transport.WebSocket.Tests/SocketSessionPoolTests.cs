using System;
using System.Threading.Tasks;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class SocketSessionPoolTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = new Mock<ISocketClientFactory>().Object;

            // act
            Exception? exception = Record.Exception(() =>
                new SocketSessionPool(optionsMonitor));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_MonitorNull_CreateObject()
        {
            // arrange
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = null!;

            // act
            Exception? exception = Record.Exception(() =>
                new SocketSessionPool(optionsMonitor));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task RentAsync_NoRentals_ReturnClient()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" });
            var pool = new SocketSessionPool(optionsMonitor);

            // act
            ISessionManager rented = await pool.RentAsync("Foo");

            // assert
            optionsMonitorMock.VerifyAll();
            Assert.IsType<SessionManager>(rented);
        }

        [Fact]
        public async Task RentAsync_OneRentals_ReturnSameClient()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" });
            var pool = new SocketSessionPool(optionsMonitor);
            ISessionManager first = await pool.RentAsync("Foo");

            // act
            ISessionManager second = await pool.RentAsync("Foo");

            // assert
            Assert.Equal(first, second);
        }

        [Fact]
        public async Task RentAsync_NoRentals_OpenSocketConnection()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);

            // act
            await pool.RentAsync("Foo");

            // assert
            Assert.Equal(1, socket.GetCallCount(x => x.OpenAsync(default!)));
        }

        [Fact]
        public async Task ReturnAsync_RentedFromPool_NotDisposeWhenNotAllReturned()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);
            ISessionManager first = await pool.RentAsync("Foo");
            ISessionManager second = await pool.RentAsync("Foo");

            // act
            await pool.ReturnAsync(first);

            // assert
            Assert.False(socket.IsDisposed);
        }

        [Fact]
        public async Task ReturnAsync_RentedFromPool_DisposeWhenAllReturned()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);
            ISessionManager first = await pool.RentAsync("Foo");
            ISessionManager second = await pool.RentAsync("Foo");

            // act
            await pool.ReturnAsync(first);
            await pool.ReturnAsync(second);

            // assert
            Assert.True(socket.IsDisposed);
        }

        [Fact]
        public async Task ReturnAsync_Rentals_CloseSocketConnection()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);
            ISessionManager rented = await pool.RentAsync("Foo");

            // act
            await pool.ReturnAsync(rented);

            // assert
            Assert.Equal(1, socket.GetCallCount(x => x.CloseAsync(default!, default!, default!)));
        }

        [Fact]
        public async Task ReturnAsync_Rentals_DisposeSocket()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);
            ISessionManager rented = await pool.RentAsync("Foo");

            // act
            await pool.ReturnAsync(rented);

            // assert
            Assert.True(socket.IsDisposed);
        }

        [Fact]
        public async Task ReturnAsync_EmptyPool_ReturnFromOtherPool()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);

            // act
            Exception? ex =
                await Record.ExceptionAsync(() => pool.ReturnAsync(new SessionManager(socket)));

            // assert
            Assert.IsType<ArgumentException>(ex).Message.MatchSnapshot();
        }

        [Fact]
        public async Task Dispose_RentedClients_DisposeClients()
        {
            // arrange
            Mock<ISocketClientFactory> optionsMonitorMock = new(MockBehavior.Strict);
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = optionsMonitorMock.Object;
            var socket = new SocketClientStub() { Protocol = protocol.Object, Name = "Foo" };
            optionsMonitorMock
                .Setup(x => x.CreateClient("Foo"))
                .Returns(() => socket);
            var pool = new SocketSessionPool(optionsMonitor);

            // act
            ISessionManager rented = await pool.RentAsync("Foo");
            await pool.DisposeAsync();

            // assert
            Assert.True(socket.IsDisposed);
        }
    }
}
