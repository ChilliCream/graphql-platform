using System.Buffers;
using System.Text.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Language;
using Moq;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

public class ApolloSubscriptionProtocolHandlerTests
{
    [Fact]
    public async Task OnReceiveAsync_Should_DisposeRequest_When_StartIdIsNotUnique()
    {
        // arrange
        GraphQLRequest? enqueued = null;
        var operations = new Mock<IOperationManager>();
        operations
            .Setup(t => t.Enqueue("abc", It.IsAny<GraphQLRequest>()))
            .Callback<string, GraphQLRequest>((_, request) => enqueued = request)
            .Returns(false);

        var connection = new Mock<ISocketConnection>();
        connection.SetupGet(t => t.IsConnected).Returns(true);

        var session = new Mock<ISocketSession>();
        session.SetupGet(t => t.Connection).Returns(connection.Object);
        session.SetupGet(t => t.Operations).Returns(operations.Object);

        var handler = new ApolloSubscriptionProtocolHandler(
            Mock.Of<ISocketSessionInterceptor>(),
            Mock.Of<IWebSocketPayloadFormatter>(),
            Mock.Of<IServerDiagnosticEvents>());

        var message = new ReadOnlySequence<byte>(
            """
            {"type":"start","id":"abc","payload":{"query":"{ a }","variables":{"b":1}}}
            """u8.ToArray());

        // act
        await handler.OnReceiveAsync(
            session.Object,
            message,
            TestContext.Current.CancellationToken);

        // assert
        connection.Verify(
            t => t.CloseAsync(
                "The subscription id is not unique.",
                ConnectionCloseReason.InternalServerError,
                It.IsAny<CancellationToken>()));
        Assert.Throws<ObjectDisposedException>(() => enqueued!.Variables!.RootElement.ValueKind);
    }

    [Fact]
    public async Task OnReceiveAsync_Should_LeaveRequestUndisposed_When_StartIsEnqueued()
    {
        // arrange
        GraphQLRequest? enqueued = null;
        var operations = new Mock<IOperationManager>();
        operations
            .Setup(t => t.Enqueue("abc", It.IsAny<GraphQLRequest>()))
            .Callback<string, GraphQLRequest>((_, request) => enqueued = request)
            .Returns(true);

        var connection = new Mock<ISocketConnection>();
        connection.SetupGet(t => t.IsConnected).Returns(true);

        var session = new Mock<ISocketSession>();
        session.SetupGet(t => t.Connection).Returns(connection.Object);
        session.SetupGet(t => t.Operations).Returns(operations.Object);

        var handler = new ApolloSubscriptionProtocolHandler(
            Mock.Of<ISocketSessionInterceptor>(),
            Mock.Of<IWebSocketPayloadFormatter>(),
            Mock.Of<IServerDiagnosticEvents>());

        var message = new ReadOnlySequence<byte>(
            """
            {"type":"start","id":"abc","payload":{"query":"{ a }","variables":{"b":1}}}
            """u8.ToArray());

        // act
        await handler.OnReceiveAsync(
            session.Object,
            message,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(JsonValueKind.Object, enqueued!.Variables!.RootElement.ValueKind);
    }
}
