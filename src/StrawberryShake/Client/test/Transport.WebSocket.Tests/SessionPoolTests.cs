using System;
using System.Threading.Tasks;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class SessionPoolTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            Mock<ISocketProtocol> protocol = new();
            ISocketClientFactory optionsMonitor = new Mock<ISocketClientFactory>().Object;

            // act
            Exception? exception = Record.Exception(() =>
                new SessionPool(optionsMonitor));

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
                new SessionPool(optionsMonitor));

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
            var pool = new SessionPool(optionsMonitor);

            // act
            ISession rented = await pool.CreateAsync("Foo");

            // assert
            optionsMonitorMock.VerifyAll();
            Assert.NotNull(rented);
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
            var pool = new SessionPool(optionsMonitor);
            ISession first = await pool.CreateAsync("Foo");

            // act
            ISession second = await pool.CreateAsync("Foo");

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
            var pool = new SessionPool(optionsMonitor);

            // act
            await pool.CreateAsync("Foo");

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
            var pool = new SessionPool(optionsMonitor);
            ISession first = await pool.CreateAsync("Foo");
            ISession second = await pool.CreateAsync("Foo");

            // act
            await first.DisposeAsync();

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
            var pool = new SessionPool(optionsMonitor);
            ISession first = await pool.CreateAsync("Foo");
            ISession second = await pool.CreateAsync("Foo");

            // act
            await first.DisposeAsync();
            await second.DisposeAsync();

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
            var pool = new SessionPool(optionsMonitor);
            ISession rented = await pool.CreateAsync("Foo");

            // act
            await rented.DisposeAsync();

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
            var pool = new SessionPool(optionsMonitor);
            ISession rented = await pool.CreateAsync("Foo");

            // act
            await rented.DisposeAsync();

            // assert
            Assert.True(socket.IsDisposed);
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
            var pool = new SessionPool(optionsMonitor);

            // act
            await pool.CreateAsync("Foo");
            await pool.DisposeAsync();

            // assert
            Assert.True(socket.IsDisposed);
        }
    }
}
