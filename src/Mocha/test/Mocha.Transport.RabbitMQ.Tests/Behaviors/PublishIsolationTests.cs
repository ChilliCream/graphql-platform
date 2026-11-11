using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class PublishIsolationTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task SendAsync_Should_FailOnlyTheOversizedMessage_When_SentConcurrentlyWithOthers()
    {
        // arrange
        const int rounds = 5;
        const int smallPerRound = 100;
        await using var maxMessageSize = await fixture.Faults.SetMaxMessageSizeAsync(64 * 1024);
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        var smallFailures = new ConcurrentQueue<Exception>();
        var oversizedFailures = new ConcurrentQueue<Exception>();

        // act
        // the broker closes the channel of a message above its size limit
        for (var round = 0; round < rounds; round++)
        {
            await Task.WhenAll(Enumerable.Range(0, smallPerRound + 1).Select(async i =>
            {
                var isOversized = i == smallPerRound / 2;
                var orderId = isOversized ? new string('x', 200_000) : $"ORD-{i}";
                try
                {
                    await SendAsync(bus, new ProcessPayment { OrderId = orderId, Amount = 1 });
                }
                catch (Exception ex)
                {
                    (isOversized ? oversizedFailures : smallFailures).Enqueue(ex);
                }
            }));
        }

        // assert
        Assert.Empty(smallFailures);
        Assert.Equal(
            Enumerable.Repeat<ushort?>(Constants.PreconditionFailed, rounds),
            oversizedFailures.Select(failure => (failure as AlreadyClosedException)?.ShutdownReason?.ReplyCode));
        Assert.Equal(rounds * smallPerRound, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_Complete_When_ConcurrentSendsAreCancelled()
    {
        // arrange
        const int waves = 20;
        const int sendsPerWave = 200;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        var failures = new ConcurrentQueue<Exception>();

        // act
        // every second send is cancelled at a random moment of its publish, the others are not cancelled
        for (var wave = 0; wave < waves; wave++)
        {
            await Task.WhenAll(Enumerable.Range(0, sendsPerWave).Select(i => Task.Run(
                async () =>
                {
                    var isCancelled = i % 2 == 0;
                    var timeout = isCancelled
                        ? TimeSpan.FromMilliseconds(Random.Shared.Next(0, 6))
                        : TestTimeouts.Signal;
                    try
                    {
                        await SendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }, timeout);
                    }
                    catch (Exception ex) when (!isCancelled)
                    {
                        failures.Enqueue(ex);
                    }
                    catch (OperationCanceledException)
                    {
                        // the cancelled half
                    }
                },
                TestContext.Current.CancellationToken)));
        }

        // assert
        // a cancelled send can have reached the broker, so the queue holds at least the other half
        Assert.Empty(failures);
        Assert.InRange(
            (await vhost.Broker.GetQueueAsync("payments"))?.Messages ?? 0,
            waves * sendsPerWave / 2,
            waves * sendsPerWave);
    }

    [Fact]
    public async Task SendAsync_Should_FailWithBrokerReason_When_HeaderIsLargerThanAFrame()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var options = new SendOptions { Headers = new() { ["x-big"] = new string('x', 200_000) } };

        // act
        // the broker closes the connection of a frame it cannot read, and every further attempt would do the same
        using var scope = bus.Provider.CreateScope();
        var exception = await Assert.ThrowsAsync<AlreadyClosedException>(
            () => scope.ServiceProvider.GetRequiredService<IMessageBus>()
                .SendAsync(
                    new ProcessPayment { OrderId = "ORD-1", Amount = 1 },
                    options,
                    TestContext.Current.CancellationToken)
                .AsTask()
                .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });

        // assert
        Assert.Equal<ushort?>(Constants.FrameError, exception.ShutdownReason?.ReplyCode);
        Assert.Equal(2, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_KeepConnectionAndDeliverOnce_When_BrokerBlocksLongerThanTheClientWaits()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(1);
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        List<Task> sends;

        // act
        // every send needs a channel of its own, and the broker answers no request on a connection it blocked
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            sends = [.. Enumerable.Range(0, 5).Select(i => SendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }))];
            await vhost.Broker.WaitForBlockedPublisherAsync();
            sends.Add(SendAsync(bus, new ProcessPayment { OrderId = "ORD-late", Amount = 1 }));

            // the alarm lasts three times as long as the client waits for an answer to a request
            await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(sends).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Same(original, current);
        Assert.Equal(7, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowTransportUnavailable_When_BrokerDoesNotConfirmWithinPublishTimeout()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost, TimeSpan.FromSeconds(2));
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        TransportUnavailableException exception;

        // act
        // a broker under a memory alarm takes no message from a publishing connection
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            exception = await Assert.ThrowsAsync<TransportUnavailableException>(
                () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 }));
        }

        // assert
        Assert.Equal(
            "The broker did not confirm the message within the publish timeout of 00:00:02. "
            + "The broker may or may not have received the message.",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowTransportUnavailable_When_BrokerStaysDownLongerThanPublishTimeout()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost, TimeSpan.FromSeconds(2));
        TransportUnavailableException exception;

        // act
        await using (await fixture.Faults.StopBrokerAsync())
        {
            exception = await Assert.ThrowsAsync<TransportUnavailableException>(
                () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 }));
        }

        // the next attempt to reconnect can come later than the publish timeout allows a send to wait
        await Wait.UntilAsync(() => bus.Transport.Dispatcher.IsConnected);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });

        // assert
        Assert.Equal(
            "The broker did not confirm the message within the publish timeout of 00:00:02. "
            + "The broker may or may not have received the message.",
            exception.Message);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    private static Task<TestBus> CreatePaymentsBusAsync(VhostContext vhost, TimeSpan? publishTimeout = null)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                if (publishTimeout is { } timeout)
                {
                    t.PublishTimeout(timeout);
                }

                t.DeclareQueue("payments");
                t.DispatchEndpoint("payment-send").ToQueue("payments").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

    private static async Task SendAsync(TestBus bus, ProcessPayment message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
    }
}
