using System.Text;
using System.Text.Json;
using Moq;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets.Protocols;

public class GraphQlWsProtocolTests
{
    [Fact]
    public async Task Constructor_AllArgs_SubscribeToChanges()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };

        // act
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        await protocol.InitializeAsync(CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Equal(1, socketClient.GetCallCount(x => x.ReceiveAsync(default!, default!)));
    }

    [Fact]
    public void Constructor_SocketClientNull_ThrowException()
    {
        // arrange
        ISocketClient socketClient = null!;

        // act
        var exception =
            Record.Exception((Action)(() => new GraphQLWebSocketProtocol(socketClient)));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task InitializeAsync_SocketIsClosed_ThrowException()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = true, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        var exception = await Record.ExceptionAsync(
            () => protocol.InitializeAsync(CancellationToken.None));

        // assert
        Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
    }

    [Fact]
    public async Task InitializeAsync_SocketIsOpen_SendInitializeMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task InitializeAsync_SocketIsOpen_SendConnectionInitPayload()
    {
        // arrange
        var connectionInterceptorMock = new Mock<ISocketConnectionInterceptor>();
        connectionInterceptorMock
            .Setup(x => x
                .CreateConnectionInitPayload(
                    It.IsAny<ISocketProtocol>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync("Payload");

        var socketClient = new SocketClientStub
        {
            IsClosed = false,
            ConnectionInterceptor = connectionInterceptorMock.Object,
        };

        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task TerminateAsync_ConnectionOpen_SendTerminationMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        await protocol.TerminateAsync(CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task TerminateAsync_ConnectionClosed_SendTerminationMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        await protocol.TerminateAsync(CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task StartOperationAsync_SocketIsClosed_ThrowException()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = true, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);

        // act
        var exception = await Record.ExceptionAsync(
            () => protocol.InitializeAsync(CancellationToken.None));

        // assert
        Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
    }

    [Fact]
    public async Task StartOperationAsync_SocketIsOpen_SendMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        var operationId = "b1b416a5-8d1b-4855-b186-6de39809caea";

        // act
        await protocol.StartOperationAsync(
            operationId,
            new OperationRequest(
                "GetHero",
                GetHeroQueryDocument.Instance
            ),
            CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task StopOperationAsync_SocketIsOpen_SendMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        var operationId = "b1b416a5-8d1b-4855-b186-6de39809caea";

        // act
        await protocol.StopOperationAsync(operationId, CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Single(socketClient.SentMessages).MatchSnapshot();
    }

    [Fact]
    public async Task StopOperationAsync_SocketIsClosed_NotSendMessage()
    {
        // arrange
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        var operationId = "b1b416a5-8d1b-4855-b186-6de39809caea";

        // act
        socketClient.IsClosed = true;
        await protocol.StopOperationAsync(operationId, CancellationToken.None);
        await protocol.DisposeAsync();

        // assert
        Assert.Empty(socketClient.SentMessages);
    }

    [Fact]
    public async Task ProcessAsync_ParseError_CloseSocket()
    {
        // arrange
        var message = @"{""type:""}";
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        SpinWait.SpinUntil(() => socketClient.IsClosed, 1000);

        // assert
        Assert.True(socketClient.IsClosed);
        Assert.Equal(SocketCloseStatus.ProtocolError, socketClient.CloseStatus);
        socketClient.CloseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessAsync_InvalidMessageType_CloseSocket()
    {
        // arrange
        var message = @"{""type"":""Start""}";
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        SpinWait.SpinUntil(() => socketClient.IsClosed, 1000);

        // assert
        Assert.True(socketClient.IsClosed);
        Assert.Equal(SocketCloseStatus.ProtocolError, socketClient.CloseStatus);
        socketClient.CloseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessAsync_NotifyFailed_CloseSocket()
    {
        // arrange
        var message = @"{""type"":""Start""}";
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        socketClient.MessagesReceive.Enqueue(message);
        protocol.Subscribe((_, _, _) => throw new InvalidOperationException());

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        SpinWait.SpinUntil(() => socketClient.IsClosed, 1000);

        // assert
        Assert.True(socketClient.IsClosed);
        Assert.Equal(SocketCloseStatus.ProtocolError, socketClient.CloseStatus);
        socketClient.CloseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessAsync_Data_ReceiveOperationMessage()
    {
        // arrange
        SemaphoreSlim semaphoreSlim = new(0);
        string? id = null;
        string? payload = null;
        var message = @"{""type"":""data"", ""payload"":""Foo"", ""id"":""123""}";
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        protocol.Subscribe((operationId, operationMessage, _) =>
        {
            if (operationMessage is DataDocumentOperationMessage<JsonDocument> msg)
            {
                id = operationId;
                payload = msg.Payload.RootElement.ToString();
            }

            semaphoreSlim.Release();
            return default;
        });
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await semaphoreSlim.WaitAsync();

        // assert
        Assert.Equal("123", id);
        Assert.Equal("Foo", payload);
    }

    [Fact]
    public async Task ProcessAsync_Complete_ReceiveOperationMessage()
    {
        // arrange
        SemaphoreSlim semaphoreSlim = new(0);
        var received = false;
        var message = @"{""type"":""complete"", ""id"":""123""}";
        var socketClient = new SocketClientStub { IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        protocol.Subscribe((_, operationMessage, _) =>
        {
            if (operationMessage is CompleteOperationMessage)
            {
                received = true;
            }

            semaphoreSlim.Release();
            return default;
        });
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await semaphoreSlim.WaitAsync();

        // assert
        Assert.True(received);
    }

    [Fact]
    public async Task ProcessAsync_Error_ReceiveOperationMessage()
    {
        // arrange
        SemaphoreSlim semaphoreSlim = new(0);
        string? error = null;
        var message = @"{
            ""type"": ""error"",
            ""id"": ""123"",
            ""payload"": {
                ""message"": ""test message""
            }
        }";
        var socketClient = new SocketClientStub { KeepOpen = true, IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        protocol.Subscribe((_, operationMessage, _) =>
        {
            if (operationMessage is ErrorOperationMessage msg)
            {
                error = msg.Payload.Message;
            }

            semaphoreSlim.Release();
            return default;
        });
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await semaphoreSlim.WaitAsync();

        // assert
        error.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessAsync_ConnectionError_ReceiveOperationMessage()
    {
        // arrange
        SemaphoreSlim semaphoreSlim = new(0);
        string? error = null;
        var message = @"{""type"":""connection_error"", ""id"":""123""}";
        var socketClient = new SocketClientStub { KeepOpen = true, IsClosed = false, };
        var protocol = new GraphQLWebSocketProtocol(socketClient);
        protocol.Subscribe((_, operationMessage, _) =>
        {
            if (operationMessage is ErrorOperationMessage msg)
            {
                error = msg.Payload.Message;
            }

            semaphoreSlim.Release();
            return default;
        });
        socketClient.MessagesReceive.Enqueue(message);

        // act
        await protocol.InitializeAsync(CancellationToken.None);
        await semaphoreSlim.WaitAsync();

        // assert
        error.MatchSnapshot();
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
