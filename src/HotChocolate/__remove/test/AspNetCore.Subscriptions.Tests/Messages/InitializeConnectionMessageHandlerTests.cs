using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;
using Xunit;
using HotChocolate.AspNetCore.Subscriptions.Interceptors;
using Moq;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class InitializeConnectionMessageHandlerTests
    {
        [Fact]
        public void CanHandle_InitializeMessage_True()
        {
            // arrange
            var handler = new InitializeConnectionMessageHandler(null);
            var message = new InitializeConnectionMessage(null);

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_AcceptMessage_False()
        {
            // arrange
            var handler = new InitializeConnectionMessageHandler(null);
            var message = KeepConnectionAliveMessage.Default;

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_InitializeMessage_Accepted()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var handler = new InitializeConnectionMessageHandler(null);
            var message = new InitializeConnectionMessage(null);

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        AcceptConnectionMessage.Default.Serialize()));
                },
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        KeepConnectionAliveMessage.Default.Serialize()));
                });
        }

        [Fact]
        public async Task Handle_InitializeMessage_Rejected()
        {
            // arrange
            var interceptor = new Mock<IConnectMessageInterceptor>();
            interceptor.Setup(t => t.OnReceiveAsync(
                It.IsAny<ISocketConnection>(),
                It.IsAny<InitializeConnectionMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ConnectionStatus.Reject());

            var connection = new SocketConnectionMock();
            var handler = new InitializeConnectionMessageHandler(
                interceptor.Object);
            var message = new InitializeConnectionMessage(null);

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new RejectConnectionMessage(
                        ConnectionStatus.Reject().Message)
                        .Serialize()));
                });
        }

        [Fact]
        public async Task Handle_InitializeMessage_Rejected_With_Extensions()
        {
            // arrange
            var connectionStatus = ConnectionStatus.Reject(
                "Foo",
                new Dictionary<string, object> { { "bar", "baz" } });

            var interceptor = new Mock<IConnectMessageInterceptor>();
            interceptor.Setup(t => t.OnReceiveAsync(
                It.IsAny<ISocketConnection>(),
                It.IsAny<InitializeConnectionMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(connectionStatus);

            var connection = new SocketConnectionMock();
            var handler = new InitializeConnectionMessageHandler(
                interceptor.Object);
            var message = new InitializeConnectionMessage(null);

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new RejectConnectionMessage(
                        connectionStatus.Message,
                        connectionStatus.Extensions)
                        .Serialize()));
                });
            Assert.True(connection.Closed);
        }
    }
}
