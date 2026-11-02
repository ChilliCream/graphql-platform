using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Tests.IntegrationTests;

public class SendIntegrationTests : ConsumerIntegrationTestsBase
{
    [Fact]
    public async Task ProcessPaymentHandler_Should_ReceiveRequest_When_RequestSent()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.RequestAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 99.99m }, CancellationToken.None);

        // assert
        // if we got here without exception, the acknowledgement round-trip succeeded
        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-1", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task AddHandler_Should_DetectRequestHandler_When_CalledForRequestHandler()
    {
        // arrange - use AddHandler<T> for a fire-and-forget request handler
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddScoped<ProcessPaymentHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<ProcessPaymentHandler>());
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.RequestAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 10m }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout),
            "Handler was not called - AddHandler did not create SendConsumer");
    }

    [Fact]
    public async Task SendAsync_Should_DeliverMessage_When_FireAndForget()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(new ProcessPayment { OrderId = "SEND-1", Amount = 99.99m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        var msg = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(msg);
        Assert.Equal("SEND-1", payment.OrderId);
    }
}
