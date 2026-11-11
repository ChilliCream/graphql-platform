using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
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
    public async Task SendAsync_Should_FailWithoutSending_When_HeadersAreLargerThanAFrame()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // act
        // the broker closes the connection of a header frame above the frame size, so the frame is not sent
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 }, headerSize: 200_000));
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Matches(
            "^The headers of the message need [0-9]+ bytes\\. The connection allows frames of at most "
            + $"{original.FrameMax} bytes, of which 1024 bytes are kept for what the client adds\\. "
            + "The message was not sent\\.$",
            exception.Message);
        Assert.Same(original, current);
        Assert.Equal(["warmup", "ORD-2"], await ReadOrderIdsAsync(vhost, "payments"));
    }

    [Fact]
    public async Task SendAsync_Should_Deliver_When_HeadersAreAsLargeAsTheFrameSizeAllows()
    {
        // arrange
        // the size that a refused send reports tells what the headers need besides the value of the large one
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 }, headerSize: 200_000));
        var reported = int.Parse(refused.Message.Split(' ')[6], CultureInfo.InvariantCulture);
        const int reserve = 1024;
        var largest = (int)original.FrameMax - reserve - (reported - 200_000);

        // act
        // the broker would close the connection if the largest headers that pass the check were too large for it
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 }, headerSize: largest);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-3", Amount = 1 }, headerSize: largest + 1));
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Same(original, current);
        Assert.Equal(["warmup", "ORD-2"], await ReadOrderIdsAsync(vhost, "payments"));
    }

    [Fact]
    public async Task SendAsync_Should_FailOnlyTheSendWithOversizedHeaders_When_OtherSendsAreInFlight()
    {
        // arrange
        const int count = 50;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var failures = new ConcurrentQueue<Exception>();
        var oversizedFailures = new ConcurrentQueue<Exception>();

        // act
        // the send with the oversized headers starts while half of the other sends wait for the broker
        await Task.WhenAll(Enumerable.Range(0, count + 1).Select(async i =>
        {
            var isOversized = i == count / 2;
            try
            {
                await SendAsync(
                    bus,
                    new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 },
                    headerSize: isOversized ? 200_000 : 0);
            }
            catch (Exception ex)
            {
                (isOversized ? oversizedFailures : failures).Enqueue(ex);
            }
        }));
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(failures);
        Assert.IsType<InvalidOperationException>(Assert.Single(oversizedFailures));
        Assert.Same(original, current);
        Assert.Equal(count + 1, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
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
    public async Task SendAsync_Should_NotReachTheQueue_When_CancelledWhileBrokerBlocksTheConnection()
    {
        // arrange
        // every concurrent send opens a channel of its own, which is idle afterwards
        const int warmups = 10;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreatePaymentsBusAsync(vhost);
        await Task.WhenAll(Enumerable.Range(0, warmups)
            .Select(i => SendAsync(bus, new ProcessPayment { OrderId = $"warmup-{i}", Amount = 1 })));
        var idleChannels = (await vhost.Broker.GetStateAsync()).IdleChannels;
        Task first;
        bool[] cancelled;

        // act
        // the first send makes the broker block the connection, the next three start while it is blocked
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            await vhost.Broker.WaitForAlarmAsync();
            first = SendAsync(bus, new ProcessPayment { OrderId = "A", Amount = 1 });
            await vhost.Broker.WaitForBlockedPublisherAsync();
            await Wait.UntilAsync(() => bus.Transport.Dispatcher.Connection.IsBlocked);
            cancelled = await Task.WhenAll(new[] { "B", "C", "D" }.Select(async orderId =>
            {
                try
                {
                    await SendAsync(bus, new ProcessPayment { OrderId = orderId, Amount = 1 }, TimeSpan.FromSeconds(2));
                    return false;
                }
                catch (OperationCanceledException)
                {
                    return true;
                }
            }));
        }

        await first.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        await SendAsync(bus, new ProcessPayment { OrderId = "E", Amount = 1 });
        var delivered = await ReadOrderIdsAsync(vhost, "payments");

        // assert
        Assert.InRange(idleChannels, 4, warmups);
        Assert.Equal([true, true, true], cancelled);
        Assert.Equal(
            Enumerable.Range(0, warmups).Select(i => $"warmup-{i}").Append("A").Append("E").Order(),
            delivered.Order());
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
        // the send waits for a connection until the publish timeout elapses
        await using (await fixture.Faults.StopBrokerAsync())
        {
            await Wait.UntilAsync(() => !bus.Transport.Dispatcher.IsConnected);
            exception = await Assert.ThrowsAsync<TransportUnavailableException>(
                () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 }));
        }

        // the next attempt to reconnect can come later than the publish timeout allows a send to wait
        await Wait.UntilAsync(() => bus.Transport.Dispatcher.IsConnected);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });

        // assert
        Assert.Equal(
            "The message could not be sent within the publish timeout of 00:00:02. "
            + "It was not written to the broker.",
            exception.Message);
        Assert.Equal(["ORD-2"], await ReadOrderIdsAsync(vhost, "payments"));
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

    /// <summary>
    /// Takes every message out of the queue and returns the order ids in the order of the queue.
    /// </summary>
    private static async Task<List<string?>> ReadOrderIdsAsync(VhostContext vhost, string queue)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var orderIds = new List<string?>();
        while (await channel.BasicGetAsync(queue, autoAck: true, TestContext.Current.CancellationToken) is { } message)
        {
            orderIds.Add(JsonSerializer.Deserialize<ProcessPayment>(message.Body.Span, options)?.OrderId);
        }

        return orderIds;
    }

    private static async Task SendAsync(
        TestBus bus,
        ProcessPayment message,
        TimeSpan? timeout = null,
        int headerSize = 0)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? TestTimeouts.Signal);
        var options = headerSize > 0
            ? new SendOptions { Headers = new() { ["x-big"] = new string('x', headerSize) } }
            : SendOptions.Default;
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, options, cts.Token);
    }
}
