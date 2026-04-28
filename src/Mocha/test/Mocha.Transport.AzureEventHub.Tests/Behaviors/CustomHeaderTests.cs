using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class CustomHeaderTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public CustomHeaderTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_PropagateHeaders_When_CustomHeadersSet()
    {
        // arrange
        var capture = new HeaderCapture();
        var hubName = _fixture.GetHubForTest("headers");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<HeaderSpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName)
                    .Consumer<HeaderSpyConsumer>()
                    .ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-HDR" },
            new PublishOptions
            {
                Headers = new() { ["x-tenant"] = "acme", ["x-trace-id"] = "trace-123" }
            },
            CancellationToken.None);

        // assert
        var headers = await capture.WaitForOrderAsync("ORD-HDR", s_timeout);
        Assert.NotNull(headers);

        Assert.True(headers.TryGetValue("x-tenant", out var tenant), "Custom header 'x-tenant' not found");
        Assert.Equal("acme", tenant);

        Assert.True(headers.TryGetValue("x-trace-id", out var traceId), "Custom header 'x-trace-id' not found");
        Assert.Equal("trace-123", traceId);
    }

    [Fact]
    public async Task PublishAsync_Should_RoundTripDateTimeOffset_When_HeaderContainsDateTimeOffset()
    {
        // arrange
        var capture = new HeaderCapture();
        var hubName = _fixture.GetHubForTest("headers");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<HeaderSpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName)
                    .Consumer<HeaderSpyConsumer>()
                    .ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var timestamp = new DateTimeOffset(2026, 4, 28, 10, 30, 0, TimeSpan.Zero);

        // act
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-DTO" },
            new PublishOptions
            {
                Headers = new() { ["x-timestamp"] = timestamp }
            },
            CancellationToken.None);

        // assert
        var headers = await capture.WaitForOrderAsync("ORD-DTO", s_timeout);
        Assert.NotNull(headers);

        Assert.True(headers.TryGetValue("x-timestamp", out var raw), "Custom header 'x-timestamp' not found");
        var received = Assert.IsType<DateTimeOffset>(raw);
        Assert.Equal(timestamp, received);
    }

    public sealed class HeaderCapture
    {
        public ConcurrentDictionary<string, Dictionary<string, object?>> CapturedByOrderId { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var h in context.Headers)
            {
                dict[h.Key] = h.Value;
            }
            CapturedByOrderId[context.Message.OrderId] = dict;
        }

        public async Task<Dictionary<string, object?>?> WaitForOrderAsync(
            string orderId,
            TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            while (!cts.IsCancellationRequested)
            {
                if (CapturedByOrderId.TryGetValue(orderId, out var headers))
                {
                    return headers;
                }
                await Task.Delay(50, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            return null;
        }
    }

    public sealed class HeaderSpyConsumer(HeaderCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
