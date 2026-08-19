using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Covers dispatch-endpoint provisioning races, topology reprovisioning on reconnect, and
/// the mandatory-publish contract as exposed by RabbitMQ.Client, all against a mocked
/// connection so the outcomes are deterministic and do not depend on a live broker.
/// </summary>
public class RabbitMQDispatchProvisioningTests
{
    [Fact]
    public async Task PublishAsync_Should_SettleProvisioning_When_ConcurrentFirstDispatchesRace()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var bus = await BuildTestBusAsync(connectionMock.Object, t =>
        {
            t.DeclareExchange("concurrent-ex").AutoProvision(true);
            t.DispatchEndpoint("concurrent-ep").ToExchange("concurrent-ex").Publish<OrderCreated>();
        });

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // the runtime eagerly declares the whole topology once on start; only the racy
        // per-endpoint provisioning that happens on first dispatch is under test here
        channelMock.Invocations.Clear();

        // gate the exchange declare so every dispatch that reads the endpoint's
        // unsynchronized _isProvisioned field before the gate opens is genuinely still
        // in flight when the next one starts, instead of each finishing before the next begins
        var declareGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var declareCount = 0;
        channelMock
            .Setup(c => c.ExchangeDeclareAsync(
                "concurrent-ex",
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref declareCount);
                await declareGate.Task;
            });

        // act - start the first dispatches without awaiting them so each one reaches the
        // gated declare call while _isProvisioned is still false, proving they overlap,
        // then release them together so they genuinely race on that check
        var raceTasks = Enumerable.Range(0, 8)
            .Select(i => messageBus.PublishAsync(
                new OrderCreated { OrderId = $"RACE-{i}" },
                TestContext.Current.CancellationToken).AsTask())
            .ToArray();

        Assert.All(raceTasks, task => Assert.False(task.IsCompleted));

        declareGate.SetResult();

        await Task.WhenAll(raceTasks);

        // assert - the endpoint's unsynchronized _isProvisioned field lets more than one of
        // the racing dispatches observe it as false, so the exchange may be declared more
        // than once during the race; the settling below is what has teeth
        Assert.InRange(declareCount, 1, 8);

        // act - once the race has settled, provisioning must not repeat on later dispatches
        channelMock.Invocations.Clear();

        await messageBus.PublishAsync(new OrderCreated { OrderId = "RACE-SETTLED" }, TestContext.Current.CancellationToken);

        // assert
        channelMock.Verify(
            c => c.ExchangeDeclareAsync(
                "concurrent-ex",
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Dispatcher_Should_ReprovisionFullTopology_When_ConnectionRecovers()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        var recoveryHandlers = new List<AsyncEventHandler<AsyncEventArgs>>();
        connectionMock
            .SetupAdd(c => c.RecoverySucceededAsync += It.IsAny<AsyncEventHandler<AsyncEventArgs>>())
            .Callback<AsyncEventHandler<AsyncEventArgs>>(recoveryHandlers.Add);

        await using var bus = await BuildTestBusAsync(connectionMock.Object, t =>
        {
            t.DeclareExchange("recover-ex").AutoProvision(true);
            t.DeclareQueue("recover-q").AutoProvision(true);
            t.DeclareBinding("recover-ex", "recover-q").AutoProvision(true);
            t.DispatchEndpoint("recover-ep").ToExchange("recover-ex").Publish<OrderCreated>();
        });

        channelMock.Invocations.Clear();

        // act - await the recovery handlers directly instead of racing a real-time delay
        // against the fire-and-forget RabbitMQ.Client event, which is how automatic topology
        // recovery presents itself to the transport
        await Task.WhenAll(recoveryHandlers.Select(
            handler => handler(connectionMock.Object, AsyncEventArgs.Empty)));

        // assert - the exchange, queue, and binding are all redeclared, not just the resources
        // touched by a prior dispatch
        channelMock.Verify(
            c => c.ExchangeDeclareAsync(
                "recover-ex",
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());

        channelMock.Verify(
            c => c.QueueDeclareAsync(
                "recover-q",
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());

        channelMock.Verify(
            c => c.QueueBindAsync(
                "recover-q",
                "recover-ex",
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task PublishAsync_Should_NotReprovision_When_ResourceDeletedWhileConnectionStaysOpen()
    {
        // arrange - every rented channel is tracked as it is created, so the assertions
        // follow whichever channel the dispatcher actually publishes on rather than
        // assuming a fixed rent order
        var exchangeDeclares = new List<string>();
        var publishedOn = new List<Mock<IChannel>>();
        var connectionMock = CreateTrackingConnection(exchangeDeclares, publishedOn);
        await using var bus = await BuildTestBusAsync(connectionMock.Object, t =>
        {
            t.DeclareExchange("deleted-ex").AutoProvision(true);
            t.DispatchEndpoint("deleted-ep").ToExchange("deleted-ex").Publish<OrderCreated>();
        });

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await messageBus.PublishAsync(new OrderCreated { OrderId = "DEL-1" }, TestContext.Current.CancellationToken);

        var channelUsedForFirstSend = Assert.Single(publishedOn);

        // the broker closed the channel after the exchange was deleted; the dispatcher
        // connection itself remains open
        channelUsedForFirstSend.SetupGet(c => c.IsOpen).Returns(false);
        exchangeDeclares.Clear();
        publishedOn.Clear();

        // act - the next send must rent a fresh channel from the still-open connection
        // because the pool discards the one that now reports closed
        await messageBus.PublishAsync(new OrderCreated { OrderId = "DEL-2" }, TestContext.Current.CancellationToken);

        // assert - the endpoint's provisioning latch is permanent for the process lifetime,
        // so the deleted exchange is never redeclared; the publish is still attempted blind
        // on a channel distinct from the one that was closed
        Assert.Empty(exchangeDeclares);
        var channelUsedForSecondSend = Assert.Single(publishedOn);
        Assert.NotSame(channelUsedForFirstSend, channelUsedForSecondSend);
    }

    [Fact]
    public async Task PublishAsync_Should_CompleteWithoutObservingBasicReturn_When_MessageIsMandatory()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var bus = await BuildTestBusAsync(connectionMock.Object, t =>
        {
            t.DeclareExchange("mandatory-ex").AutoProvision(true);
            t.DispatchEndpoint("mandatory-ep").ToExchange("mandatory-ex").Publish<OrderCreated>();
        });

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - no BasicReturnAsync handler is attached to dispatch channels, and publisher
        // confirmations are not enabled on them either, so an unroutable mandatory publish
        // would go unnoticed; the mandatory flag is dropped rather than left as dead signal
        await messageBus.PublishAsync(new OrderCreated { OrderId = "MANDATORY-1" }, TestContext.Current.CancellationToken);

        // assert - the publish is sent with mandatory=false, and no BasicReturnAsync handler
        // is ever attached to the channel
        channelMock.Verify(
            c => c.BasicPublishAsync(
                It.IsAny<CachedString>(),
                It.IsAny<CachedString>(),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Once());

        channelMock.VerifyAdd(
            c => c.BasicReturnAsync += It.IsAny<AsyncEventHandler<BasicReturnEventArgs>>(),
            Times.Never());
    }

    private static Mock<IChannel> CreateOpenChannel()
    {
        var channelMock = new Mock<IChannel>();
        channelMock.SetupGet(c => c.IsOpen).Returns(true);
        return channelMock;
    }

    private static Mock<IConnection> CreateOpenConnection(Mock<IChannel> channelToReturn)
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.SetupGet(c => c.IsOpen).Returns(true);
        connectionMock.SetupGet(c => c.ClientProvidedName).Returns("test-connection");
        connectionMock
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelToReturn.Object);

        return connectionMock;
    }

    /// <summary>
    /// Builds a connection that hands out a fresh, tracked channel mock on every rent, so
    /// tests can follow which channel actually declares or publishes without assuming a
    /// fixed rent order.
    /// </summary>
    private static Mock<IConnection> CreateTrackingConnection(
        List<string> exchangeDeclares,
        List<Mock<IChannel>> publishedOn)
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.SetupGet(c => c.IsOpen).Returns(true);
        connectionMock.SetupGet(c => c.ClientProvidedName).Returns("test-connection");
        connectionMock
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var channelMock = CreateOpenChannel();
                channelMock
                    .Setup(c => c.ExchangeDeclareAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<bool>(),
                        It.IsAny<IDictionary<string, object?>>(),
                        It.IsAny<bool>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<string, string, bool, bool, IDictionary<string, object?>?, bool, bool, CancellationToken>(
                        (exchange, _, _, _, _, _, _, _) => exchangeDeclares.Add(exchange))
                    .Returns(Task.CompletedTask);
                channelMock
                    .Setup(c => c.BasicPublishAsync(
                        It.IsAny<CachedString>(),
                        It.IsAny<CachedString>(),
                        It.IsAny<bool>(),
                        It.IsAny<BasicProperties>(),
                        It.IsAny<ReadOnlyMemory<byte>>(),
                        It.IsAny<CancellationToken>()))
                    .Callback(() => publishedOn.Add(channelMock))
                    .Returns(ValueTask.CompletedTask);
                return channelMock.Object;
            });

        return connectionMock;
    }

    private static async Task<TestBus> BuildTestBusAsync(
        IConnection connection,
        Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var provider = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new FakeConnectionProvider(connection));
                configure(t);
            })
            .Services
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        return new TestBus(provider, runtime);
    }

    private sealed class FakeConnectionProvider(IConnection connection) : IRabbitMQConnectionProvider
    {
        public string Host => "localhost";
        public string VirtualHost => "/";
        public int Port => 5672;

        public ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken) => new(connection);
    }
}
