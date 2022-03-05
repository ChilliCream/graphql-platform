using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Messages;

public class ConnectionTerminateHandlerTests
{
    [Fact]
    public void CanHandle_TerminateMessage_True()
    {
        // arrange
        var interceptor = new SocketSessionInterceptorMock();
        var handler = new TerminateConnectionOperationMessageHandler(interceptor);
        var message = new TerminateConnectionMessage();

        // act
        var result = handler.CanHandle(message);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_AcceptMessage_False()
    {
        // arrange
        var interceptor = new SocketSessionInterceptorMock();
        var handler = new TerminateConnectionOperationMessageHandler(interceptor);
        KeepConnectionAliveMessage message = KeepConnectionAliveMessage.Default;

        // act
        var result = handler.CanHandle(message);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_TerminateMessage_ConnectionClosed()
    {
        // arrange
        var connection = new SocketConnectionMock { IsClosed = false };
        var interceptor = new SocketSessionInterceptorMock();
        var handler = new TerminateConnectionOperationMessageHandler(interceptor);
        var message = new TerminateConnectionMessage();

        // act
        await handler.HandleAsync(
            connection,
            message,
            CancellationToken.None);

        // assert
        Assert.True(connection.IsClosed);
    }
}
