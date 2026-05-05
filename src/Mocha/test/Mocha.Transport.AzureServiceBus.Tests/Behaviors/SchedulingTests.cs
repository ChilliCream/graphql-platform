using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class SchedulingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public SchedulingTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_DelayDelivery_When_ScheduledTimeInFuture()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(5);

        // act
        await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
            scheduledTime,
            CancellationToken.None);

        // assert - message should NOT arrive before the scheduled time
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(2)),
            "Handler should not have received the message before its scheduled time");
        Assert.Empty(recorder.Messages);

        // ...but it should arrive once the scheduled time elapses
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the scheduled message");

        var payment = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-1", payment.OrderId);
        Assert.Equal(50.00m, payment.Amount);
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_ReturnCancellableToken_When_TransportSupportsNative()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(10);

        // act
        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 25.00m },
            scheduledTime,
            CancellationToken.None);

        // assert
        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.StartsWith("asb:", result.Token);
        Assert.Equal(scheduledTime, result.ScheduledTime);

        // cleanup so the message does not get delivered to a future test run
        await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_PreventDelivery_When_CalledBeforeScheduledTime()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(10);

        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-CANCEL", Amount = 10.00m },
            scheduledTime,
            CancellationToken.None);

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.True(cancelled);
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(15)),
            "Handler should not have received a cancelled scheduled message");
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_TokenIsMalformed()
    {
        // arrange
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act + assert
        Assert.False(await messageBus.CancelScheduledMessageAsync("not-a-token", CancellationToken.None));
        Assert.False(await messageBus.CancelScheduledMessageAsync("asb:", CancellationToken.None));
        Assert.False(await messageBus.CancelScheduledMessageAsync("asb:queue:", CancellationToken.None));
        Assert.False(await messageBus.CancelScheduledMessageAsync("asb:queue:not-a-number", CancellationToken.None));
        Assert.False(await messageBus.CancelScheduledMessageAsync("asb:noseparator", CancellationToken.None));
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_MessageAlreadyDispatched()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(2);

        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-LATE", Amount = 1.00m },
            scheduledTime,
            CancellationToken.None);

        // wait for delivery
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the scheduled message");

        // act - cancel after dispatch
        var cancelled = await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.False(cancelled);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
