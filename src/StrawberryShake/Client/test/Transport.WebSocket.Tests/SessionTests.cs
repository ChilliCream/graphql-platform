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
    public class SessionTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            var client = new SocketClientStub() { Protocol = new Mock<ISocketProtocol>().Object };

            // act
            Exception? exception = Record.Exception(() => new Session(client));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_ClientNull_ThrowException()
        {
            // arrange
            ISocketClient client = null!;

            // act
            Exception? exception = Record.Exception(() => new Session(client));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task OpenSessionAsync_NoProtocolNeogiated_ThrowException()
        {
            // arrange
            var client = new SocketClientStub { Protocol = null! };
            var manager = new Session(client);

            // act
            Exception? exception = await Record.ExceptionAsync(() => manager.OpenSessionAsync());

            // assert
            Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
        }

        [Fact]
        public async Task OpenSessionAsync_ValidSocket_SubscribeToProtocol()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));
            var manager = new Session(client);

            // act
            await manager.OpenSessionAsync().ConfigureAwait(false);

            // assert
            protocolMock.VerifyAll();
        }

        [Fact]
        public async Task OpenSessionAsync_ValidSocket_OpenSocket()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            var manager = new Session(client);

            // act
            await manager.OpenSessionAsync().ConfigureAwait(false);

            // assert
            Assert.Equal(1, client.GetCallCount(x => x.OpenAsync(default!)));
        }


        [Fact]
        public async Task CloseSessionAsync_OpenSocket_CloseSocket()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            var manager = new Session(client);

            // act
            await manager.CloseSessionAsync().ConfigureAwait(false);

            // assert
            Assert.Equal(1, client.GetCallCount(x => x.CloseAsync(default!, default!, default!)));
        }

        [Fact]
        public async Task StartOperationAsync_RequestNull_ThrowException()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));
            OperationRequest request = null!;
            var manager = new Session(client);
            await manager.OpenSessionAsync();

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => manager.StartOperationAsync(request));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task StartOperationAsync_SocketCloses_ThrowException()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => manager.StartOperationAsync(request));

            // assert
            Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
        }

        [Fact]
        public async Task StartOperationAsync_RequestNotNull_StartOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync();
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
            var client = new SocketClientStub { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync();
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

            await Task.Delay(500);

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
            var manager = new Session(client);
            await manager.OpenSessionAsync();
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
        public async Task StopOperationAsync_SocketCloses_ThrowException()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);

            // act
            Exception? exception =
                await Record.ExceptionAsync(() => manager.StopOperationAsync("123"));

            // assert
            Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
        }

        [Fact]
        public async Task StopOperationAsync_RequestNotNull_DisposeOperation()
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync();
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
            var manager = new Session(client);
            await manager.OpenSessionAsync();
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            List<OperationMessage> messages = new();

            // act
            ISocketOperation operation = await manager.StartOperationAsync(request);
            await listener(operation.Id,
                ErrorOperationMessage.ConnectionInitializationError,
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
            var manager = new Session(client);
            await manager.OpenSessionAsync();

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
            var manager = new Session(client);
            await manager.OpenSessionAsync();
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

            public DocumentHash Hash { get; } = new("MD5", "ABC");

            public override string ToString() => _bodyString;

            public static GetHeroQueryDocument Instance { get; } = new();
        }
    }
}
