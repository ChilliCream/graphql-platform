using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Behaviors;

[Collection("Kafka")]
public class AutoProvisionIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly KafkaFixture _fixture;

    public AutoProvisionIntegrationTests(KafkaFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_AutoProvisionEnabledByDefault()
    {
        // arrange - default auto-provision (true)
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddKafka(t => t.BootstrapServers(ctx.BootstrapServers))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-1" }, CancellationToken.None);

        // assert - message is delivered because topology was auto-provisioned
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");
        var order = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-1", order.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_AutoProvisionExplicitlyEnabled()
    {
        // arrange - explicit auto-provision true
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddKafka(t =>
            {
                t.BootstrapServers(ctx.BootstrapServers);
                t.AutoProvision(true);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");
        var order = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-2", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_Deliver_When_AutoProvisionEnabledByDefault()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddKafka(t => t.BootstrapServers(ctx.BootstrapServers))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "AP-3", Amount = 42.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request");
        var payment = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-3", payment.OrderId);
    }

    [Fact]
    public async Task ExplicitTopology_Should_Deliver_When_AutoProvisionEnabledOnResources()
    {
        // arrange - transport auto-provision disabled, but individual resources enabled
        var capture = new OrderCapture();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddKafka(t =>
            {
                t.BootstrapServers(ctx.BootstrapServers);
                t.AutoProvision(false);
                t.BindHandlersExplicitly();
                t.DeclareTopic("ap-topic").AutoProvision(true);

                t.Endpoint("ap-ep").Consumer<OrderSpyConsumer>().Topic("ap-topic");
                t.DispatchEndpoint("ap-dispatch").ToTopic("ap-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-4" }, CancellationToken.None);

        // assert - resources were explicitly enabled, so message should be delivered
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message");
        var message = Assert.Single(capture.Messages);
        Assert.Equal("AP-4", message.OrderId);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class OrderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            Messages.Add(context.Message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class OrderSpyConsumer(OrderCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
