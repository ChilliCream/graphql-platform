using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Moq;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets.Messages;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class SocketOperationManagerTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            var client = new SocketClientStub() { Protocol = new Mock<ISocketProtocol>().Object };

            // act
            Exception? exception = Record.Exception(() => new SocketOperationManager(client));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_ClientNull_ThrowException()
        {
            // arrange
            ISocketClient client = null!;

            // act
            Exception? exception = Record.Exception(() => new SocketOperationManager(client));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Constructor_SocketNotInitialized_ThrowException()
        {
            // arrange
            var client = new SocketClientStub { Protocol = null! };

            // act
            Exception? exception = Record.Exception(() => new SocketOperationManager(client));

            // assert
            Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
        }

        [Fact]
        public void Constructor_AllArgs_SubscribeToProtocol()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));

            // act
            new SocketOperationManager(client);

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task StartOperationAsync_RequestNull_ThrowException()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));
            OperationRequest request = null!;
            var manager = new SocketOperationManager(client);

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => manager.StartOperationAsync(request));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task StartOperationAsync_RequestNotNull_StartOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new SocketOperationManager(client);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await manager.StartOperationAsync(request);

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task StartOperationAsync_RequestNotNull_RegistersStopEvent()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new SocketOperationManager(client);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            protocolMock
                .Setup(x => x.StopOperationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await manager.StartOperationAsync(request);
            protocolMock.Raise(x => x.Disposed += null, new EventArgs());

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task StopOperationAsync_RequestNotNull_StopOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new SocketOperationManager(client);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            ISocketOperation operation = await manager.StartOperationAsync(request);
            protocolMock
                .Setup(x => x.StopOperationAsync(operation.Id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            await manager.StopOperationAsync(operation.Id);

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task StopOperationAsync_RequestNotNull_DisposeOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new SocketOperationManager(client);
            List<OperationMessage> messages = new();

            // act
            ISocketOperation operation = await manager.StartOperationAsync(request);
            await manager.StopOperationAsync(operation.Id);

            // should return immediately
            await foreach (var elm in operation.ReadAsync(CancellationToken.None))
            {
                messages.Push(elm);
            }

            // assert
            Assert.Empty(messages);
        }

        [Fact]
        public async Task ReceiveMessage_OperationRegistered_ForwardMessageToOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OnReceiveAsync listener = null!;
            protocolMock
                .Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()))
                .Callback((OnReceiveAsync subscribe) =>
                {
                    listener = subscribe;
                });
            var manager = new SocketOperationManager(client);
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            List<OperationMessage> messages = new();

            // act
            ISocketOperation operation = await manager.StartOperationAsync(request);
            await listener(operation.Id,
                ErrorOperationMessage.ConnectionError,
                CancellationToken.None);
            await foreach (var elm in operation.ReadAsync(CancellationToken.None))
            {
                messages.Push(elm);
                break;
            }

            // assert
            Assert.Single(messages);
        }

        [Fact]
        public async Task Dispose_Subscribed_Unsubscribe()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            protocolMock.Setup(x => x.Unsubscribe(It.IsAny<OnReceiveAsync>()));
            OperationRequest request = null!;
            var manager = new SocketOperationManager(client);

            // act
            await manager.DisposeAsync();

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task Dispose_RegisteredOperations_DisposeOperations()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new SocketOperationManager(client);
            ISocketOperation operation = await manager.StartOperationAsync(request);
            List<OperationMessage> messages = new();

            // act
            await manager.DisposeAsync();
            await foreach (var elm in operation.ReadAsync(CancellationToken.None))
            {
                messages.Push(elm);
            }

            // assert
            Assert.Empty(messages);
        }

        private class GetHeroQueryDocument : IDocument
        {
            private const string _bodyString =
                @"query GetHero {
                hero {
                    __typename
                    id
                    name
                    friends {
                        nodes {
                            __typename
                            id
                            name
                        }
                        totalCount
                    }
                }
                version
            }";

            private static readonly byte[] _body = Encoding.UTF8.GetBytes(_bodyString);

            private GetHeroQueryDocument() { }

            public OperationKind Kind => OperationKind.Query;

            public ReadOnlySpan<byte> Body => _body;

            public override string ToString() => _bodyString;

            public static GetHeroQueryDocument Instance { get; } = new();
        }
    }
}
