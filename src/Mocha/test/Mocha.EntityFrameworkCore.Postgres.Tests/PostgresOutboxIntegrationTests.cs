using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Outbox_Should_DeliverMessage_When_EventPublished()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithOutboxAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.PublishAsync(new TestEvent { Payload = "hello" }, default);

        // Assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should have received the message");
        var received = Assert.Single(recorder.Messages.OfType<TestEvent>());
        Assert.Equal("hello", received.Payload);
    }

    [Fact]
    public async Task Outbox_Should_DeliverAllMessages_When_MultipleEventsPublished()
    {
        // Arrange
        const int count = 5;
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithOutboxAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        for (var i = 0; i < count; i++)
        {
            await bus.PublishAsync(new TestEvent { Payload = $"msg-{i}" }, default);
        }

        // Assert
        Assert.True(await recorder.WaitAsync(s_timeout, count), $"Handler should have received all {count} messages");
        var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).OrderBy(p => p).ToList();
        Assert.Equal(count, payloads.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Contains($"msg-{i}", payloads);
        }
    }

    [Fact]
    public async Task Outbox_Should_DeliverMessages_When_PublishedUnderLoad()
    {
        // Arrange
        const int count = 50;
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithOutboxAsync(recorder);

        // Act - publish concurrently from separate scopes
        var tasks = Enumerable
            .Range(0, count)
            .Select(async i =>
            {
                using var scope = env.Provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new TestEvent { Payload = $"load-{i}" }, default);
            });
        await Task.WhenAll(tasks);

        // Assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, count),
            $"Handler should have received all {count} messages under load");

        var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).ToHashSet();
        Assert.Equal(count, payloads.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Contains($"load-{i}", payloads);
        }
    }

    [Fact]
    public async Task Outbox_Should_ProcessPendingMessages_When_WorkerStartsAfterPersist()
    {
        // Arrange - persist messages before the worker starts
        const int count = 3;
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new MessageRecorder();

        // Phase 1: build bus but don't start the hosted services (worker)
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<TestEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(default);

        // Ensure schema exists
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(default);
        }

        // Persist messages via IMessageBus (outbox captures them)
        for (var i = 0; i < count; i++)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new TestEvent { Payload = $"pending-{i}" }, default);
        }

        // Verify that the messages are persisted
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var messages = await db.Set<OutboxMessage>()!.ToListAsync(default);
            Assert.Equal(count, messages.Count);
        }

        // Phase 2: start the outbox worker (hosted services)
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var svc in hostedServices)
        {
            await svc.StartAsync(default);
        }

        try
        {
            // Assert - all pre-existing messages are processed
            Assert.True(
                await recorder.WaitAsync(s_timeout, count),
                "Worker should process messages that were persisted before it started");

            var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).ToHashSet();
            Assert.Equal(count, payloads.Count);
        }
        finally
        {
            foreach (var svc in hostedServices)
            {
                await svc.StopAsync(default);
            }

            // Allow in-flight processor transactions to drain (see TestEnvironment comment)
            await Task.Delay(250, default);

            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task Outbox_Should_ResumeProcessing_When_WorkerRestartedAfterInterruption()
    {
        // Arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new MessageRecorder();

        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<TestEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(default);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(default);
        }

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var svc in hostedServices)
        {
            await svc.StartAsync(default);
        }

        try
        {
            // Phase 1: publish and let worker process
            using (var scope = provider.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new TestEvent { Payload = "before-stop" }, default);
            }

            Assert.True(await recorder.WaitAsync(s_timeout), "First message should be delivered before worker stops");

            // Phase 2: stop worker
            foreach (var svc in hostedServices)
            {
                await svc.StopAsync(default);
            }

            // Allow the background loop to fully drain before publishing.
            // ContinuousTask.DisposeAsync cancels but does not await the task.
            await Task.Delay(500, default);

            // Phase 3: publish more messages while worker is stopped
            using (var scope = provider.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new TestEvent { Payload = "during-stop" }, default);
            }

            // Phase 4: restart worker
            foreach (var svc in hostedServices)
            {
                await svc.StartAsync(default);
            }

            // Assert - message published during downtime is delivered.
            // Wait until "during-stop" appears in the recorder's messages.
            using var waitCts = new CancellationTokenSource(s_timeout);
            while (!recorder.Messages.OfType<TestEvent>().Any(e => e.Payload == "during-stop"))
            {
                await Task.Delay(50, waitCts.Token);
            }

            var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).ToHashSet();
            Assert.Contains("before-stop", payloads);
            Assert.Contains("during-stop", payloads);
        }
        finally
        {
            foreach (var svc in hostedServices)
            {
                await svc.StopAsync(default);
            }

            // Allow in-flight processor transactions to drain (see TestEnvironment comment)
            await Task.Delay(250, default);

            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task Outbox_Should_ProcessNewMessages_When_PublishedWhileWorkerRunning()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithOutboxAsync(recorder);

        // Act - publish messages at intervals while worker is running
        for (var i = 0; i < 5; i++)
        {
            using var scope = env.Provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new TestEvent { Payload = $"live-{i}" }, default);

            // Wait for this message to be delivered before publishing the next
            Assert.True(
                await recorder.WaitAsync(s_timeout),
                $"Message live-{i} should be delivered while worker is running");
        }

        // Assert
        var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).ToHashSet();
        Assert.Equal(5, payloads.Count);
    }

    [Fact]
    public async Task Outbox_Should_HandleConcurrentPublishers_When_MultipleScopes()
    {
        // Arrange
        const int scopeCount = 10;
        const int messagesPerScope = 5;
        const int totalMessages = scopeCount * messagesPerScope;
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithOutboxAsync(recorder);

        // Act - multiple scopes publishing simultaneously
        var tasks = Enumerable
            .Range(0, scopeCount)
            .Select(async scopeIndex =>
            {
                for (var i = 0; i < messagesPerScope; i++)
                {
                    using var scope = env.Provider.CreateScope();
                    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                    await bus.PublishAsync(
                        new TestEvent { Payload = $"scope-{scopeIndex}-msg-{i}" },
                        default);
                }
            });
        await Task.WhenAll(tasks);

        // Assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, totalMessages),
            $"All {totalMessages} messages from {scopeCount} scopes should be delivered");

        var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).ToHashSet();
        Assert.Equal(totalMessages, payloads.Count);
    }

    private async Task<TestEnvironment> CreateBusWithOutboxAsync(MessageRecorder recorder)
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

        // Register the resilient signal BEFORE UsePostgresOutbox() so that
        // TryAddSingleton<IOutboxSignal> in AddOutboxCore() is a no-op.
        // This prevents ObjectDisposedException during teardown when the
        // outbox processor's own transaction commits fire the interceptor.
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<TestEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(default);

        // Ensure schema exists
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(default);
        }

        // Start hosted services (outbox worker)
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var svc in hostedServices)
        {
            await svc.StartAsync(default);
        }

        return new TestEnvironment(provider, hostedServices);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Test types
    // ══════════════════════════════════════════════════════════════════════

    public sealed class TestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class TestEventHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentBag<object> Messages { get; } = [];

        public void Record(object message)
        {
            Messages.Add(message);
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

    /// <summary>
    /// Ensures hosted services are stopped before the provider is disposed,
    /// preventing ObjectDisposedException from background tasks.
    /// </summary>
    private sealed class TestEnvironment(ServiceProvider provider, List<IHostedService> hostedServices)
        : IAsyncDisposable
    {
        public ServiceProvider Provider => provider;

        public async ValueTask DisposeAsync()
        {
            foreach (var svc in hostedServices)
            {
                await svc.StopAsync(default);
            }

            // ContinuousTask.DisposeAsync cancels but doesn't await the background
            // loop, so in-flight processor transactions may still be committing.
            // Allow them to drain before disposing the provider's singletons.
            await Task.Delay(250);

            await provider.DisposeAsync();
        }
    }
}
