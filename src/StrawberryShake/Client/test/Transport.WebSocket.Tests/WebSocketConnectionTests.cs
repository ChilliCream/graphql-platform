using System.Text;
using System.Text.Json;
using Moq;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

public class WebSocketConnectionTests
{
    [Fact]
    public void Constructor_AllArgs_CreateObject()
    {
        // arrange
        ValueTask<ISession> SessionFactory(CancellationToken cancellationToken)
            => new(new Mock<ISession>().Object);

        // act
        var exception = Record.Exception(() => new WebSocketConnection(SessionFactory));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ManagerNull_CreateObject()
    {
        // arrange
        Func<CancellationToken, ValueTask<ISession>> sessionFactory = null!;

        // act
        var exception = Record.Exception(() => new WebSocketConnection(sessionFactory));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task ExecuteAsync_Completed_Complete()
    {
        // arrange
#pragma warning disable CS1998
        async IAsyncEnumerable<OperationMessage> Producer()
        {
            yield break;
        }
#pragma warning restore CS1998

        var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
        var managerMock = new Mock<ISession>();
        var operationMock = new Mock<ISocketOperation>();
        managerMock
            .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
            .ReturnsAsync(operationMock.Object);
        operationMock.Setup(x => x.ReadAsync()).Returns(Producer());
        ValueTask<ISession> SessionFactory(CancellationToken ct) => new(managerMock.Object);
        var connection = new WebSocketConnection(SessionFactory);
        var results = new List<Response<JsonDocument>>();

        // act
        await foreach (var response in connection.ExecuteAsync(operationRequest))
        {
            results.Add(response);
        }

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteAsync_Data_ParseJson()
    {
        // arrange
#pragma warning disable CS1998
        async IAsyncEnumerable<OperationMessage> Producer()
        {
            var messageData = JsonDocument.Parse(@"{""Foo"": ""Bar""}");
            var msg =
                new DataDocumentOperationMessage<JsonDocument>(messageData);
            yield return msg;
        }
#pragma warning restore CS1998

        var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
        var managerMock = new Mock<ISession>();
        var operationMock = new Mock<ISocketOperation>();
        managerMock
            .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
            .ReturnsAsync(operationMock.Object);
        operationMock.Setup(x => x.ReadAsync()).Returns(Producer());
        ValueTask<ISession> SessionFactory(CancellationToken cancellationToken)
            => new(managerMock.Object);
        var connection = new WebSocketConnection(SessionFactory);
        var results = new List<Response<JsonDocument>>();

        // act
        await foreach (var response in connection.ExecuteAsync(operationRequest))
        {
            results.Add(response);
        }

        // assert
        Assert.Single(results)!.Body!.RootElement.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Error_ReturnResult()
    {
        // arrange
#pragma warning disable CS1998
        async IAsyncEnumerable<OperationMessage> Producer()
        {
            yield return ErrorOperationMessage.ConnectionInitializationError;
        }
#pragma warning restore CS1998

        var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
        var managerMock = new Mock<ISession>();
        var operationMock = new Mock<ISocketOperation>();
        managerMock
            .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
            .ReturnsAsync(operationMock.Object);
        operationMock.Setup(x => x.ReadAsync()).Returns(Producer());
        ValueTask<ISession> SessionFactory(CancellationToken cancellationToken)
            => new(managerMock.Object);
        var connection = new WebSocketConnection(SessionFactory);
        var results = new List<Response<JsonDocument>>();

        // act
        await foreach (var response in connection.ExecuteAsync(operationRequest))
        {
            results.Add(response);
        }

        // assert
        var res = Assert.Single(results);
        res?.Exception?.Message.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_ReturnResult()
    {
        // arrange
#pragma warning disable CS1998
        async IAsyncEnumerable<OperationMessage> Producer()
        {
            yield return CancelledOperationMessage.Default;
        }
#pragma warning restore CS1998

        var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
        var managerMock = new Mock<ISession>();
        var operationMock = new Mock<ISocketOperation>();
        managerMock
            .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
            .ReturnsAsync(operationMock.Object);
        operationMock.Setup(x => x.ReadAsync()).Returns(Producer());
        ValueTask<ISession> SessionFactory(CancellationToken cancellationToken)
            => new(managerMock.Object);
        var connection = new WebSocketConnection(SessionFactory);
        var results = new List<Response<JsonDocument>>();

        // act
        await foreach (var response in connection.ExecuteAsync(operationRequest))
        {
            results.Add(response);
        }

        // assert
        var res = Assert.Single(results);
        res?.Exception?.Message.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Completed_ReturnResult()
    {
        // arrange
#pragma warning disable CS1998
        async IAsyncEnumerable<OperationMessage> Producer()
        {
            yield return CompleteOperationMessage.Default;
        }
#pragma warning restore CS1998

        var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
        var managerMock = new Mock<ISession>();
        var operationMock = new Mock<ISocketOperation>();
        managerMock
            .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
            .ReturnsAsync(operationMock.Object);
        operationMock.Setup(x => x.ReadAsync()).Returns(Producer());
        ValueTask<ISession> SessionFactory(CancellationToken cancellationToken)
            => new(managerMock.Object);
        var connection = new WebSocketConnection(SessionFactory);
        var results = new List<Response<JsonDocument>>();

        // act
        await foreach (var response in connection.ExecuteAsync(operationRequest))
        {
            results.Add(response);
        }

        // assert
        Assert.Empty(results);
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
