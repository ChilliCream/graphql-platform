using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class StubTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WhenSent_Should_PublishResponse_When_StubRegistered()
    {
        // arrange — register a handler for the response, but NOT for the request.
        // The stub will act as the consumer for the request.
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<PaymentResultHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // Register the stub before sending
        tracker.WhenSent<StubPayment>().RespondWith(
            msg => new StubPaymentResult { OrderId = msg.OrderId, Approved = true });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — send the message (stub intercepts and publishes response)
        await bus.SendAsync(new StubPayment { OrderId = "PAY-STUB" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — the stub response should have been published and consumed
        Assert.True(result.Completed);
        var payment = result.ShouldHaveSent<StubPayment>();
        Assert.Equal("PAY-STUB", payment.OrderId);

        var response = result.ShouldHaveConsumed<StubPaymentResult>();
        Assert.Equal("PAY-STUB", response.OrderId);
        Assert.True(response.Approved);
    }

    [Fact]
    public async Task WhenSent_Should_Timeout_When_NoStubAndNoConsumer()
    {
        // arrange — no handler and no stub for the message type
        await using var provider = await CreateBusAsync(_ => { });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — send a message with no handler and no stub
        await bus.SendAsync(new StubPayment { OrderId = "PAY-NONE" }, CancellationToken.None);

        // assert — should complete gracefully (dispatched-only) after grace period,
        // since the sent message has no subscriber
        var result = await tracker.WaitForCompletionAsync(Timeout);
        Assert.True(result.Completed);
        Assert.Single(result.Dispatched);
        Assert.Empty(result.Consumed);
    }

    // --- Helpers ---

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        services.AddMessageTracking();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }
}

// --- Test message and handler types ---

public sealed class StubPayment
{
    public required string OrderId { get; init; }
}

public sealed class StubPaymentResult
{
    public required string OrderId { get; init; }
    public required bool Approved { get; init; }
}

public sealed class PaymentResultHandler : IEventHandler<StubPaymentResult>
{
    public ValueTask HandleAsync(StubPaymentResult _, CancellationToken __) => default;
}
