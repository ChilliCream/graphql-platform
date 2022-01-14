using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Messages;

public class DataStopMessageHandlerTests
{
    [Fact]
    public void CanHandle_DataStartMessage_True()
    {
        // arrange
        var handler = new DataStopMessageHandler();
        var message = new DataStopMessage("123");

        // act
        var result = handler.CanHandle(message);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_KeepAliveMessage_False()
    {
        // arrange
        var handler = new DataStopMessageHandler();
        KeepConnectionAliveMessage message = KeepConnectionAliveMessage.Default;

        // act
        var result = handler.CanHandle(message);

        // assert
        Assert.False(result);
    }


    [Fact]
    public async Task Handle_Stop_Subscription()
    {
        // arrange
        var connection = new SocketConnectionMock();
        var subscription = new Mock<ISubscription>();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddStarWarsTypes()
            .AddStarWarsRepositories()
            .AddInMemorySubscriptions()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        var stream =
            (IResponseStream)await executor.ExecuteAsync(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

        var interceptor = new SocketSessionInterceptorMock();
        var subscriptionSession = new SubscriptionSession(
            new CancellationTokenSource(),
            interceptor,
            connection,
            stream,
            subscription.Object,
            new NoopExecutionDiagnosticEvents(),
            "123");
        connection.Subscriptions.Register(subscriptionSession);

        var handler = new DataStopMessageHandler();
        var message = new DataStopMessage("123");

        // act
        await handler.HandleAsync(
            connection,
            message,
            CancellationToken.None);

        // assert
        Assert.Empty(connection.Subscriptions);
    }
}
