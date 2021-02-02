using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class WebSocketClientTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            string name = "Foo";
            IReadOnlyList<ISocketProtocolFactory> protocolFactories =
                Array.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new WebSocketClient(name, protocolFactories));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_MonitorNull_CreateObject()
        {
            // arrange
            string name = null!;
            IReadOnlyList<ISocketProtocolFactory> protocolFactories =
                Array.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new WebSocketClient(name, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Constructor_FactoriesNull_CreateObject()
        {
            // arrange
            string name = "Foo";
            IReadOnlyList<ISocketProtocolFactory> protocolFactories = null!;

            // act
            Exception? exception = Record.Exception(() =>
                new WebSocketClient(name, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task OpenAsync_Disposed_ThrowsException()
        {
            // arrange
            string name = "Foo";
            IReadOnlyList<ISocketProtocolFactory> protocolFactories =
                Array.Empty<ISocketProtocolFactory>();
            var socket = new WebSocketClient(name, protocolFactories);
            await socket.DisposeAsync();

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => socket.OpenAsync(CancellationToken.None));

            // assert
            Assert.IsType<ObjectDisposedException>(exception);
        }

        [Fact]
        public async Task OpenAsync_UriIsNull_ThrowsException()
        {
            // arrange
            string name = "Foo";
            IReadOnlyList<ISocketProtocolFactory> protocolFactories =
                Array.Empty<ISocketProtocolFactory>();
            var socket = new WebSocketClient(name, protocolFactories);
            await socket.DisposeAsync();

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => socket.OpenAsync(CancellationToken.None));

            // assert
            Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
        }
    }
}
