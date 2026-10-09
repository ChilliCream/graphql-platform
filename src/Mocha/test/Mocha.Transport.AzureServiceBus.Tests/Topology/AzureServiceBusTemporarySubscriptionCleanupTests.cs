using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

/// <summary>
/// Covers removal of Mocha-owned, auto-provisioned convention forwarding subscriptions when a
/// temporary receive endpoint stops. Complements
/// <see cref="AzureServiceBusForwardingProvisioningTests"/>, which covers provisioning order,
/// with the mirrored teardown path. These tests run against a <see cref="FakeServiceBusClient"/>
/// without a live namespace or emulator.
/// </summary>
public sealed class AzureServiceBusTemporarySubscriptionCleanupTests
{
    [Fact]
    public async Task OnStopAsync_Should_DeleteForwardingSubscription_When_TemporaryEndpointStops()
    {
        // arrange - implicit binding wires the consumed message type through a convention topic
        // and forwarding subscription targeting the temporary endpoint's queue
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
                t.Endpoint("temp-ep").Consumer<NoOpConsumer>().Queue("temp-q").Temporary());
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var subscription = topology.Subscriptions.Single(s => s.Destination.Name == "temp-q");
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == "temp-q");

        // act
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([(subscription.Source.Name, subscription.Name)], admin.DeletedSubscriptions);
        Assert.Equal(["temp-q"], admin.DeletedQueues);
    }

    [Fact]
    public async Task OnStopAsync_Should_NotDeleteSubscription_When_EndpointIsNotTemporary()
    {
        // arrange - same implicit convention subscription, but the endpoint never opts into Temporary()
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t => t.Endpoint("durable-ep").Consumer<NoOpConsumer>().Queue("durable-q"));
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == "durable-q");

        // act
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(admin.DeletedSubscriptions);
        Assert.Empty(admin.DeletedQueues);
    }

    [Fact]
    public async Task OnStopAsync_Should_NotDeleteDeclaredSubscription_When_TemporaryEndpointStops()
    {
        // arrange - "declared-topic" is explicitly declared and subscribed to the temporary queue;
        // the endpoint also consumes OrderCreated, which the implicit binder wires through its own
        // convention topic and forwarding subscription targeting the same queue
        const string declaredTopic = "declared-topic";
        const string queueName = "declared-q";
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.DeclareTopic(declaredTopic);
                t.DeclareQueue(queueName);
                t.DeclareSubscription(declaredTopic, queueName);
                t.Endpoint("declared-ep").Consumer<NoOpConsumer>().Queue(queueName).Temporary();
            });
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var conventionSubscription = topology.Subscriptions.Single(s => s.Source.Name != declaredTopic);
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == queueName);

        // act
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert - only the convention subscription is removed, the declared one is left alone
        Assert.Equal(
            [(conventionSubscription.Source.Name, conventionSubscription.Name)],
            admin.DeletedSubscriptions);
        Assert.Empty(admin.DeletedQueues);
    }

    [Fact]
    public async Task OnStopAsync_Should_NotAttemptCleanup_When_AutoProvisionDisabled()
    {
        // arrange - the convention subscription still exists in the in-memory topology model, but
        // was never provisioned on the broker, so cleanup must not attempt to delete it either
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("unmanaged-ep").Consumer<NoOpConsumer>().Queue("unmanaged-q").Temporary();
            });
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == "unmanaged-q");

        // act
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(admin.DeletedSubscriptions);
        Assert.Empty(admin.DeletedQueues);
    }

    [Fact]
    public async Task OnStopAsync_Should_TreatMissingSubscriptionAsSuccess_When_AlreadyRemoved()
    {
        // arrange - simulates a repeated cleanup attempt (e.g. after a prior partial shutdown)
        // finding the subscription already gone
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient
        {
            DeleteSubscriptionFailure = (_, _) =>
                new ServiceBusException("not found", ServiceBusFailureReason.MessagingEntityNotFound)
        };
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
                t.Endpoint("idempotent-ep").Consumer<NoOpConsumer>().Queue("idempotent-q").Temporary());
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == "idempotent-q");
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "idempotent-q").Processor;

        // act & assert - no exception propagates
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        Assert.True(processor.IsClosed);
        Assert.Equal(["idempotent-q"], admin.DeletedQueues);
    }

    [Fact]
    public async Task OnStopAsync_Should_LogAndContinue_When_SubscriptionDeletionFailsNonBenignly()
    {
        // arrange - a non-benign deletion failure must be reported but must not prevent the
        // heartbeat/processor from being disposed or escape as an exception from StopAsync
        var failure = new InvalidOperationException("boom");
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient { DeleteSubscriptionFailure = (_, _) => failure };
        var loggerProvider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(loggerProvider));
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
                t.Endpoint("failing-ep").Consumer<NoOpConsumer>().Queue("failing-q").Temporary());
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Queue.Name == "failing-q");
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "failing-q").Processor;

        // act - no exception propagates despite the deletion failure
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert - the failure was logged and the processor was still disposed
        Assert.Contains(
            loggerProvider.Entries,
            e => e.Level == LogLevel.Warning && e.Exception == failure);
        Assert.True(processor.IsClosed);
        Assert.Empty(admin.DeletedQueues);
    }

    public sealed class NoOpConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
