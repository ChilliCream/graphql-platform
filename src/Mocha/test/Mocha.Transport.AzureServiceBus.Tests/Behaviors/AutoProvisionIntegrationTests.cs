using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class AutoProvisionIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly AzureServiceBusFixture _fixture;

    public AutoProvisionIntegrationTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_AutoProvisionEnabledByDefault()
    {
        // arrange - default auto-provision (true)
        var recorder = new MessageRecorder();
        await using var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(ctx)
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
        await using var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
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
        await using var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx)
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
    public async Task DeclareQueue_Should_ProvisionQueueOnBroker_When_AutoProvisionEnabled()
    {
        // arrange - declare a custom queue with non-default knobs and verify it lands on the broker
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("explicit-q");
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(true);
                t.DeclareQueue(queueName)
                    .WithLockDuration(TimeSpan.FromSeconds(45))
                    .WithMaxDeliveryCount(7);
            })
            .BuildTestBusAsync();

        // act - inspect the broker via the admin client
        var adminClient = new ServiceBusAdministrationClient(ctx.AdminConnectionString);
        var properties = await adminClient.GetQueueAsync(queueName, Xunit.TestContext.Current.CancellationToken);

        // assert - the queue exists with the configured knobs propagated
        Assert.Equal(queueName, properties.Value.Name);
        Assert.Equal(TimeSpan.FromSeconds(45), properties.Value.LockDuration);
        Assert.Equal(7, properties.Value.MaxDeliveryCount);
    }

    [Fact]
    public async Task DeclareSubscription_Should_ProvisionTopicAndSubscriptionOnBroker_When_AutoProvisionEnabled()
    {
        // arrange - declare a topic, queue, and subscription that links them
        await using var ctx = _fixture.CreateTestContext();
        var topicName = ctx.TopicName("topic");
        var queueName = ctx.QueueName("q");
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(true);
                t.DeclareTopic(topicName);
                t.DeclareQueue(queueName);
                t.DeclareSubscription(topicName, queueName);
            })
            .BuildTestBusAsync();

        // act - inspect the broker
        var adminClient = new ServiceBusAdministrationClient(ctx.AdminConnectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var topicExists = (await adminClient.TopicExistsAsync(topicName, cancellationToken)).Value;
        var queueExists = (await adminClient.QueueExistsAsync(queueName, cancellationToken)).Value;

        // assert - the topic, queue, and forwarding subscription all exist
        Assert.True(topicExists, $"Topic '{topicName}' was not provisioned");
        Assert.True(queueExists, $"Queue '{queueName}' was not provisioned");

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var subscriptionName = topology.Subscriptions.Single(s =>
            s.Source.Name == topicName && s.Destination.Name == queueName).Name;
        var subscriptionExists =
            (await adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken)).Value;
        Assert.True(
            subscriptionExists,
            $"Subscription '{subscriptionName}' on topic '{topicName}' was not provisioned");
    }

    [Fact]
    public async Task ExplicitTopology_Should_Deliver_When_AutoProvisionEnabledOnResources()
    {
        // arrange - transport auto-provision disabled, but individual resources enabled
        var capture = new OrderCapture();
        await using var ctx = _fixture.CreateTestContext();
        var topicName = ctx.TopicName("ap-topic");
        var queueName = ctx.QueueName("ap-q");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(false);
                t.BindExplicitly();
                t.DeclareTopic(topicName).AutoProvision(true);
                t.DeclareQueue(queueName).AutoProvision(true);
                t.DeclareSubscription(topicName, queueName).AutoProvision(true);

                t.Endpoint("ap-ep").Consumer<OrderSpyConsumer>().Queue(queueName);
                t.DispatchEndpoint("ap-dispatch").ToTopic(topicName).Publish<OrderCreated>();
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

    [Fact]
    public async Task DeclareQueue_Should_NotProvisionOnBroker_When_AutoProvisionDisabled()
    {
        // arrange - transport auto-provision disabled, no resource overrides
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("not-provisioned");
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(false);
                t.BindExplicitly();
                t.DeclareQueue(queueName);
            })
            .BuildTestBusAsync();

        // act - inspect the broker
        var adminClient = new ServiceBusAdministrationClient(ctx.AdminConnectionString);
        var queueExists =
            (await adminClient.QueueExistsAsync(queueName, Xunit.TestContext.Current.CancellationToken)).Value;

        // assert - the queue should not exist because auto-provision was disabled
        Assert.False(queueExists, $"Queue '{queueName}' should NOT have been provisioned");
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
