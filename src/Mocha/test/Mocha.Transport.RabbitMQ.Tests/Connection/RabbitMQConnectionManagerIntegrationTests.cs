using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class RabbitMQConnectionManagerIntegrationTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task DisposeAsync_Should_CloseConnection_When_ConnectionAttemptCompletesAfterDisposal()
    {
        // arrange
        // a connection provider that does not observe cancellation hands its connection over after the disposal
        await using var vhost = await fixture.CreateVhostAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new RabbitMQConnectionManager(
            NullLogger.Instance,
            RabbitMQMessagingTransport.ConsumerRole,
            TimeProvider.System,
            async _ =>
            {
                entered.TrySetResult();
                await release.Task;
                return await vhost.ConnectionFactory.CreateConnectionAsync(CancellationToken.None);
            },
            static (_, _) => Task.CompletedTask);
        var starting = manager.StartAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // act
        var disposing = manager.DisposeAsync().AsTask();
        var connectionsBeforeRelease = await vhost.Broker.GetConnectionCountAsync();
        var disposeWaitedForAttempt = !disposing.IsCompleted;
        release.TrySetResult();
        await disposing.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.True(disposeWaitedForAttempt);
        Assert.Equal(0, connectionsBeforeRelease);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => starting);
        await vhost.Broker.WaitForNoConnectionAsync();
    }
}
