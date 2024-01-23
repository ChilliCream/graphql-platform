using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Language;
using Moq;
using StrawberryShake.Transport.WebSockets.Messages;
using static HotChocolate.Tests.TestHelper;

namespace StrawberryShake.Transport.WebSockets;

public class SessionTests
{
    [Fact]
    public void Constructor_AllArgs_CreateObject()
    {
        // arrange
        var client = new SocketClientStub { Protocol = new Mock<ISocketProtocol>().Object, };

        // act
        var exception = Record.Exception(() => new Session(client));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ClientNull_ThrowException()
    {
        // arrange
        ISocketClient client = null!;

        // act
        var exception = Record.Exception(() => new Session(client));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task OpenSessionAsync_NoProtocolNegotiated_ThrowException()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var client = new SocketClientStub { Protocol = null!, };
            var manager = new Session(client);

            // act
            var exception = await Record.ExceptionAsync(
            () => manager.OpenSessionAsync(ct));

            // assert
            await snapshot
                .Add(Assert.IsType<SocketOperationException>(exception).Message)
                .MatchAsync(ct);
        });
    }

    [Fact]
    public async Task OpenSessionAsync_ValidSocket_SubscribeToProtocol()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));
            var manager = new Session(client);

            // act
            await manager.OpenSessionAsync(ct).ConfigureAwait(false);

            // assert
            protocolMock.VerifyAll();
        });
    }

    [Fact]
    public async Task OpenSessionAsync_ValidSocket_OpenSocket()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            var manager = new Session(client);

            // act
            await manager.OpenSessionAsync(ct).ConfigureAwait(false);

            // assert
            Assert.Equal(1, client.GetCallCount(x => x.OpenAsync(default!)));
        });
    }


    [Fact]
    public async Task CloseSessionAsync_OpenSocket_CloseSocket()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            var manager = new Session(client);

            // act
            await manager.CloseSessionAsync(ct).ConfigureAwait(false);

            // assert
            Assert.Equal(1, client.GetCallCount(
            x => x.CloseAsync(default!, default!, default!)));
        });
    }

    [Fact]
    public async Task StartOperationAsync_RequestNull_ThrowException()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            protocolMock.Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()));
            OperationRequest request = null!;
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);

            // act
            var exception = await Record.ExceptionAsync(
            () => manager.StartOperationAsync(request, ct));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        });
    }

    [Fact]
    public async Task StartOperationAsync_SocketCloses_ThrowException()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);

            // act
            var exception =
            await Record.ExceptionAsync(() => manager.StartOperationAsync(request, ct));

            // assert
            await snapshot
                .Add(Assert.IsType<SocketOperationException>(exception).Message)
                .MatchAsync(ct);
        });
    }

    [Fact]
    public async Task StartOperationAsync_RequestNotNull_StartOperation()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await manager.StartOperationAsync(request, ct);

            // assert
            protocolMock.VerifyAll();
        });
    }

    [Fact]
    public async Task StartOperationAsync_RequestNotNull_RegistersStopEvent()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            protocolMock
                .Setup(x =>
                    x.StopOperationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await manager.StartOperationAsync(request, ct);
            protocolMock.Raise(x => x.Disposed += null, new EventArgs());

            await Task.Delay(500, ct);

            // assert
            protocolMock.VerifyAll();
        });
    }

    [Fact]
    public async Task StopOperationAsync_RequestNotNull_StopOperation()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            protocolMock
                .Setup(x =>
                    x.StartOperationAsync(It.IsAny<string>(),
                        request,
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var operation = await manager.StartOperationAsync(request, ct);
            protocolMock
                .Setup(x => x.StopOperationAsync(operation.Id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            await manager.StopOperationAsync(operation.Id, ct);

            // assert
            protocolMock.VerifyAll();
        });
    }

    [Fact]
    public async Task StopOperationAsync_SocketCloses_ThrowException()
    {
        var snapshot = new Snapshot();

        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>(MockBehavior.Strict);
            var client = new SocketClientStub() { Protocol = protocolMock.Object, };
            var manager = new Session(client);

            // act
            var exception = await Record.ExceptionAsync(
            () => manager.StopOperationAsync("123", ct));

            // assert
            await snapshot
                .Add(Assert.IsType<SocketOperationException>(exception).Message)
                .MatchAsync(ct);
        });
    }

    [Fact]
    public async Task StopOperationAsync_RequestNotNull_DisposeOperation()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            List<OperationMessage> messages = [];

            // act
            var operation = await manager.StartOperationAsync(request, ct);
            await manager.StopOperationAsync(operation.Id, ct);

            // should return immediately
            await foreach (var elm in operation.ReadAsync().WithCancellation(ct))
            {
                messages.Push(elm);
            }

            // assert
            Assert.Empty(messages);
        });
    }

    [Fact]
    public async Task ReceiveMessage_OperationRegistered_ForwardMessageToOperation()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub() { Protocol = protocolMock.Object, };
            OnReceiveAsync listener = null!;
            protocolMock
                .Setup(x => x.Subscribe(It.IsAny<OnReceiveAsync>()))
                .Callback((OnReceiveAsync subscribe) => listener = subscribe);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            List<OperationMessage> messages = [];

            // act
            var operation = await manager.StartOperationAsync(request, ct);
            await listener(operation.Id, ErrorOperationMessage.ConnectionInitializationError, ct);

            await foreach (var elm in operation.ReadAsync().WithCancellation(ct))
            {
                messages.Push(elm);
                break;
            }

            // assert
            Assert.Single(messages);
        });
    }

    [Fact]
    public async Task Dispose_Subscribed_Unsubscribe()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            protocolMock.Setup(x => x.Unsubscribe(It.IsAny<OnReceiveAsync>()));
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);

            // act
            await manager.DisposeAsync();

            // assert
            protocolMock.VerifyAll();
        });
    }

    [Fact]
    public async Task Dispose_RegisteredOperations_DisposeOperations()
    {
        await TryTest(async ct =>
        {
            // arrange
            var protocolMock = new Mock<ISocketProtocol>();
            var client = new SocketClientStub { Protocol = protocolMock.Object, };
            OperationRequest request = new("Foo", GetHeroQueryDocument.Instance);
            var manager = new Session(client);
            await manager.OpenSessionAsync(ct);
            var operation = await manager.StartOperationAsync(request, ct);
            List<OperationMessage> messages = [];

            // act
            await manager.DisposeAsync();
            await foreach (var elm in operation.ReadAsync().WithCancellation(ct))
            {
                messages.Push(elm);
            }

            // assert
            Assert.Empty(messages);
        });
    }

    private sealed class GetHeroQueryDocument : IDocument
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
