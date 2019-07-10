using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class ConnectionInitializeHandlerTests
    {
        [Fact]
        public void CanHandle_InitializeMessage_True()
        {
            // arrange
            var handler = new ConnectionInitializeHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Connection.Initialize
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_AcceptMessage_False()
        {
            // arrange
            var handler = new ConnectionInitializeHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Connection.Accept
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_InitializeMessage_True()
        {
            // arrange
            (WebSocketContext context, WebSocketMock socket) =
                WebSocketContextHelper.Create();
            var handler = new ConnectionInitializeHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Connection.Initialize
            };

            // act
            await handler.HandleAsync(
                context,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(socket.Outgoing,
                t =>
                {
                    Assert.Equal(MessageTypes.Connection.Accept, t.Type);
                },
                t =>
                {
                    Assert.Equal(MessageTypes.Connection.KeepAlive, t.Type);
                });
        }
    }
}
