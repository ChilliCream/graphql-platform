using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class CustomHeaderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public CustomHeaderTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_PropagateHeaders_When_CustomHeadersSet()
    {
        // arrange
        var capture = new HeaderCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<HeaderSpyConsumer>()
            .AddRabbitMQ()
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
        Assert.True(await capture.WaitAsync(Timeout), "Consumer did not receive the published message");

        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.TryGetValue("x-tenant", out var tenant), "Custom header 'x-tenant' not found");
        Assert.Equal("acme", tenant);

        Assert.True(headers.TryGetValue("x-trace-id", out var traceId), "Custom header 'x-trace-id' not found");
        Assert.Equal("trace-123", traceId);
    }

    public sealed class HeaderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var h in context.Headers)
            {
                dict[h.Key] = h.Value;
            }
            CapturedHeaders.Add(dict);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                    return false;
            }
            return true;
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
