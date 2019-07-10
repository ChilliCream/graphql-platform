using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class ConnectionTerminateHandlerTests
    {
        [Fact]
        public void CanHandle_TerminateMessage_True()
        {
            // arrange
            var handler = new ConnectionTerminateHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Connection.Terminate
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
            var handler = new ConnectionTerminateHandler();
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
        public async Task Handle_TerminateMessage_True()
        {
            // arrange
            (WebSocketContext context, WebSocketMock socket) =
                WebSocketContextHelper.Create();
            var handler = new ConnectionTerminateHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Connection.Terminate
            };

            // act
            await handler.HandleAsync(
                context,
                message,
                CancellationToken.None);

            // assert
            Assert.True(socket.Closed);
        }
    }
}
