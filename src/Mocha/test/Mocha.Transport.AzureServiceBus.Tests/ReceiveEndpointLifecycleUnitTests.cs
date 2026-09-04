using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

/// <summary>
/// Covers the receive endpoint start/stop lifecycle against a <see cref="FakeServiceBusClient"/>:
/// the processor and session processor option values resolved at startup, start-failure cleanup,
/// stop/disposal ordering, and processor error handling after the endpoint has stopped. These
/// tests run without a live namespace or emulator.
/// </summary>
public sealed class ReceiveEndpointLifecycleUnitTests
{
    [Fact]
    public async Task OnStartAsync_Should_ResolveExplicitPrefetchAndConcurrency_When_Configured()
    {
        // arrange
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("explicit-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue("explicit-opts")
                    .MaxConcurrency(3)
                    .PrefetchCount(37)
                    .MaxAutoLockRenewalDuration(TimeSpan.FromMinutes(9));
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert - the always-present instance reply endpoint also creates a processor, so the
        // endpoint under test is singled out by its own queue name
        var created = Assert.Single(client.CreatedProcessors, p => p.QueueName == "explicit-opts");
        Assert.Equal(
            (PrefetchCount: 37,
                MaxConcurrentCalls: 3,
                MaxAutoLockRenewalDuration: TimeSpan.FromMinutes(9),
                ReceiveMode: ServiceBusReceiveMode.PeekLock,
                AutoCompleteMessages: false),
            (created.Options.PrefetchCount,
                created.Options.MaxConcurrentCalls,
                created.Options.MaxAutoLockRenewalDuration,
                created.Options.ReceiveMode,
                created.Options.AutoCompleteMessages));
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveDefaultPrefetchAndConcurrency_When_NotConfigured()
    {
        // arrange
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("default-opts-ep").Consumer<NoOpConsumer>().Queue("default-opts");
            });
        var expectedMaxConcurrency = Math.Clamp(ReceiveEndpointConfiguration.Defaults.MaxConcurrency, 1, 1000);

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert - the always-present instance reply endpoint also creates a processor, so the
        // endpoint under test is singled out by its own queue name
        var created = Assert.Single(client.CreatedProcessors, p => p.QueueName == "default-opts");
        Assert.Equal(
            (PrefetchCount: expectedMaxConcurrency * 2,
                MaxConcurrentCalls: expectedMaxConcurrency,
                MaxAutoLockRenewalDuration: TimeSpan.FromMinutes(5),
                ReceiveMode: ServiceBusReceiveMode.PeekLock,
                AutoCompleteMessages: false),
            (created.Options.PrefetchCount,
                created.Options.MaxConcurrentCalls,
                created.Options.MaxAutoLockRenewalDuration,
                created.Options.ReceiveMode,
                created.Options.AutoCompleteMessages));
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveExplicitSessionOptions_When_Configured()
    {
        // arrange
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.DeclareQueue("explicit-session-opts").RequiresSession();
                t.Endpoint("explicit-session-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue("explicit-session-opts")
                    .MaxConcurrentSessions(5)
                    .MaxConcurrentCallsPerSession(2)
                    .PrefetchCount(41)
                    .MaxAutoLockRenewalDuration(TimeSpan.FromMinutes(7))
                    .SessionIdleTimeout(TimeSpan.FromSeconds(45));
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert
        var created = Assert.Single(client.CreatedSessionProcessors);
        Assert.Equal(
            (PrefetchCount: 41,
                MaxConcurrentSessions: 5,
                MaxConcurrentCallsPerSession: 2,
                MaxAutoLockRenewalDuration: TimeSpan.FromMinutes(7),
                SessionIdleTimeout: (TimeSpan?)TimeSpan.FromSeconds(45),
                ReceiveMode: ServiceBusReceiveMode.PeekLock,
                AutoCompleteMessages: false),
            (created.Options.PrefetchCount,
                created.Options.MaxConcurrentSessions,
                created.Options.MaxConcurrentCallsPerSession,
                created.Options.MaxAutoLockRenewalDuration,
                created.Options.SessionIdleTimeout,
                created.Options.ReceiveMode,
                created.Options.AutoCompleteMessages));
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveDefaultSessionOptions_When_NotConfigured()
    {
        // arrange - default MaxConcurrentSessions falls back to MaxConcurrency, and
        // MaxConcurrentCallsPerSession defaults to 1 to preserve in-session ordering.
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.DeclareQueue("default-session-opts").RequiresSession();
                t.Endpoint("default-session-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue("default-session-opts")
                    .MaxConcurrency(6);
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert
        var created = Assert.Single(client.CreatedSessionProcessors);
        Assert.Equal(
            (PrefetchCount: 12,
                MaxConcurrentSessions: 6,
                MaxConcurrentCallsPerSession: 1,
                MaxAutoLockRenewalDuration: TimeSpan.FromMinutes(5),
                SessionIdleTimeout: (TimeSpan?)null,
                ReceiveMode: ServiceBusReceiveMode.PeekLock,
                AutoCompleteMessages: false),
            (created.Options.PrefetchCount,
                created.Options.MaxConcurrentSessions,
                created.Options.MaxConcurrentCallsPerSession,
                created.Options.MaxAutoLockRenewalDuration,
                created.Options.SessionIdleTimeout,
                created.Options.ReceiveMode,
                created.Options.AutoCompleteMessages));
    }

    [Fact]
    public async Task StartAsync_Should_DisposeAndClearProcessorAndHeartbeat_When_ReplyReceiverCreationFails()
    {
        // arrange - the reply queue heartbeat receiver is created only after the processor has
        // already started, so failing it exercises the catch block that must dispose and null
        // both resources before propagating the failure.
        var failure = new InvalidOperationException("receiver creation failed");
        var client = new FakeServiceBusClient(_ => null) { ReceiverFailure = _ => failure };
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var provider = services
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddAzureServiceBus(t => t.AutoProvision(false))
            .Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var bus = new TestBus(provider, runtime);
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var replyQueueName = ((AzureServiceBusReceiveEndpoint)(transport.ReplyReceiveEndpoint
            ?? throw new InvalidOperationException("Expected a reply receive endpoint to be configured."))).Queue.Name;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.StartAsync(Xunit.TestContext.Current.CancellationToken).AsTask());

        // assert - the request-handler endpoint also creates a processor, so the reply endpoint
        // under test is singled out by its own queue name
        var failedProcessor = Assert.Single(client.CreatedProcessors, p => p.QueueName == replyQueueName).Processor;
        Assert.Same(failure, thrown);
        Assert.False(runtime.IsStarted);
        Assert.True(failedProcessor.IsClosed);

        // act - retrying without the injected failure must succeed, proving the failed attempt's
        // processor and heartbeat were fully released instead of left bound to a disposed link
        client.ReceiverFailure = null;
        await runtime.StartAsync(Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task OnStopAsync_Should_DisposeHeartbeatBeforeProcessor_When_ReplyEndpointStops()
    {
        // arrange
        var order = new List<string>();
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddAzureServiceBus(t => t.AutoProvision(false));
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var replyEndpoint = (AzureServiceBusReceiveEndpoint)(transport.ReplyReceiveEndpoint
            ?? throw new InvalidOperationException("Expected a reply receive endpoint to be configured."));

        // the request-handler endpoint also creates a processor, so the reply endpoint under
        // test is singled out by its own queue name
        var processor = client.CreatedProcessors.Single(p => p.QueueName == replyEndpoint.Queue.Name).Processor;
        var receiver = client.CreatedReceivers.Single();
        var processorClosedWhenHeartbeatDisposed = true;
        processor.OnStopProcessing = () => order.Add("processor-stop-processing");
        receiver.OnDisposing = () =>
        {
            order.Add("heartbeat-dispose");
            processorClosedWhenHeartbeatDisposed = processor.IsClosed;
        };

        // act
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert - the processor stops accepting work, then the heartbeat is disposed while the
        // processor is still open, and only then is the processor itself disposed
        Assert.Equal(["processor-stop-processing", "heartbeat-dispose"], order);
        Assert.False(processorClosedWhenHeartbeatDisposed);
        Assert.True(processor.IsClosed);
    }

    [Fact]
    public async Task OnStopAsync_Should_CompleteStopAndLogWarning_When_ProcessorDisposeFails()
    {
        // arrange
        var loggerProvider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        services.AddLogging(b => b.AddProvider(loggerProvider));
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("dispose-failure-ep").Consumer<NoOpConsumer>().Queue("dispose-failure");
            });
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints.Single(e => e != transport.ReplyReceiveEndpoint);
        var failure = new TimeoutException("link drain timed out");
        var created = client.CreatedProcessors.Single(p => p.QueueName == "dispose-failure");
        created.Processor.CloseFailure = failure;

        // act - releasing the processor is best-effort cleanup; a failure while closing the
        // underlying AMQP link must not fail the stop
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.False(endpoint.IsStarted);
        var entry = Assert.Single(loggerProvider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task OnStopAsync_Should_DisposeProcessor_When_HeartbeatDisposeFails()
    {
        // arrange
        var loggerProvider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        services.AddLogging(b => b.AddProvider(loggerProvider));
        var builder = services
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddAzureServiceBus(t => t.AutoProvision(false));
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var replyEndpoint = (AzureServiceBusReceiveEndpoint)(transport.ReplyReceiveEndpoint
            ?? throw new InvalidOperationException("Expected a reply receive endpoint to be configured."));

        // the request-handler endpoint also creates a processor, so the reply endpoint under
        // test is singled out by its own queue name
        var processor = client.CreatedProcessors.Single(p => p.QueueName == replyEndpoint.Queue.Name).Processor;
        var receiver = client.CreatedReceivers.Single();
        var failure = new TimeoutException("receiver close timed out");
        receiver.OnDisposing = () => throw failure;

        // act
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert - the heartbeat's failure is logged and the processor is still disposed
        Assert.False(replyEndpoint.IsStarted);
        Assert.True(processor.IsClosed);
        var entry = Assert.Single(loggerProvider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task OnProcessorError_Should_NotResurrectEndpoint_When_RaisedAfterStop()
    {
        // arrange
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("late-error-ep").Consumer<NoOpConsumer>().Queue("late-error");
            });
        await using var bus = await builder.BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // the always-present instance reply endpoint is excluded; only the endpoint under test
        // is stopped and asserted on
        var endpoint = transport.ReceiveEndpoints.Single(e => e != transport.ReplyReceiveEndpoint);
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "late-error").Processor;
        await endpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // act - the SDK can still report an in-flight error after StopProcessingAsync returns
        await processor.RaiseProcessErrorAsync(
            new ProcessErrorEventArgs(
                new InvalidOperationException("late failure"),
                ServiceBusErrorSource.Receive,
                client.FullyQualifiedNamespace,
                "late-error",
                Xunit.TestContext.Current.CancellationToken));

        // assert - the error is only logged; the endpoint is not restarted
        Assert.False(endpoint.IsStarted);
    }

    public sealed class NoOpConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
