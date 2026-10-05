using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record ProcessPayment(string OrderId, decimal Amount);

public sealed record ReserveStock(string Sku);

public sealed record ReleaseStock(string Sku);

[Collection(JetStreamCollection.Name)]
public class SendTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddNats(nats => nats.StreamName("e2e-send-request"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .SendAsync(new ProcessPayment("ORD-1", 99.99m), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the message.");
            Assert.Equal(new ProcessPayment("ORD-1", 99.99m), Assert.Single(recorder.Messages));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_EventHandlerRegistered()
    {
        // arrange
        // Send and Publish must converge on the same endpoint. The naming conventions give a Send
        // route the bare message name and a Publish route a namespace-qualified one, so a transport
        // that binds only one of the two silently drops the other.
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ReserveStockHandler>()
            .AddNats(nats => nats.StreamName("e2e-send-event"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .SendAsync(new ReserveStock("SKU-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the message.");
            Assert.Equal(new ReserveStock("SKU-1"), Assert.Single(recorder.Messages));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToHandler_When_EventHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ReleaseStockHandler>()
            .AddNats(nats => nats.StreamName("e2e-publish-event"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new ReleaseStock("SKU-2"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the message.");
            Assert.Equal(new ReleaseStock("SKU-2"), Assert.Single(recorder.Messages));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder)
        : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ReserveStockHandler(MessageRecorder recorder) : IEventHandler<ReserveStock>
    {
        public ValueTask HandleAsync(ReserveStock message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ReleaseStockHandler(MessageRecorder recorder) : IEventHandler<ReleaseStock>
    {
        public ValueTask HandleAsync(ReleaseStock message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
