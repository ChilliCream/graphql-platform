using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests;

public sealed class SqlServerSchedulingIntegrationTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Scheduler_Should_DispatchMessage_When_ScheduledTimeReached()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.PublishAsync(
            new TestEvent { Payload = "hello" },
            new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
            default);

        // Assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should have received the scheduled message");
        var received = Assert.Single(recorder.Messages.OfType<TestEvent>());
        Assert.Equal("hello", received.Payload);
    }

    [Fact]
    public async Task Scheduler_Should_DispatchAllMessages_When_MultipleMessagesScheduled()
    {
        // Arrange
        const int count = 5;
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        for (var i = 0; i < count; i++)
        {
            await bus.PublishAsync(
                new TestEvent { Payload = $"msg-{i}" },
                new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
                default);
        }

        // Assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, count),
            $"Handler should have received all {count} scheduled messages");
        var payloads = recorder.Messages.OfType<TestEvent>().Select(e => e.Payload).OrderBy(p => p).ToList();
        Assert.Equal(count, payloads.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Contains($"msg-{i}", payloads);
        }
    }

    [Fact]
    public async Task Scheduler_Should_ProcessPendingMessages_When_WorkerStartsAfterPersist()
    {
        // Arrange - persist messages before the worker starts
        const int count = 3;
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new MessageRecorder();

        // Phase 1: build bus but don't start the hosted services (worker)
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseSqlServer(connectionString)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton<ISchedulerSignal>(new ResilientSchedulerSignal());

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UseSqlServerScheduling());
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

        // Persist messages via IMessageBus (scheduling middleware captures them)
        for (var i = 0; i < count; i++)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new TestEvent { Payload = $"pending-{i}" },
                new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
                default);
        }

        // Phase 2: start the scheduling worker (hosted services)
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
    public async Task Scheduler_Should_DeleteMessage_When_DispatchSucceeds()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.PublishAsync(
            new TestEvent { Payload = "delete-me" },
            new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
            default);

        // Wait for handler to receive the message
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should have received the scheduled message");

        // Assert - the row should have been deleted after successful dispatch.
        // Give a brief moment for the DELETE to commit after the handler returns.
        await Task.Delay(500);

        using var verifyScope = env.Provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var remaining = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [dbo].[scheduled_messages]")
            .SingleAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Scheduler_Should_RecordLastError_When_DispatchFails()
    {
        // Arrange - use a dispatch middleware that always throws AFTER the scheduling middleware.
        // During initial publish the scheduling middleware intercepts (never calling next),
        // so the throwing middleware is inactive. During re-dispatch from the worker the
        // scheduling middleware is skipped, causing the throwing middleware to fire.
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder, AddFailingDispatchMiddleware);

        using (var scope = env.Provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new TestEvent { Payload = "will-fail" },
                new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
                default);
        }

        // Wait for at least one dispatch attempt to record the error
        using var waitCts = new CancellationTokenSource(s_timeout);

        while (!waitCts.Token.IsCancellationRequested)
        {
            await Task.Delay(250, waitCts.Token);

            using var scope = env.Provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var rows = await db.Database
                .SqlQueryRaw<string?>(
                    "SELECT TOP(1) [last_error] AS [Value] FROM [dbo].[scheduled_messages]")
                .ToListAsync(waitCts.Token);

            if (rows.Count > 0 && rows[0] is not null)
            {
                // Assert - parse the JSON error
                using var doc = JsonDocument.Parse(rows[0]!);
                Assert.True(
                    doc.RootElement.TryGetProperty("message", out _),
                    "last_error should contain 'message'");
                Assert.True(
                    doc.RootElement.TryGetProperty("exceptionType", out _),
                    "last_error should contain 'exceptionType'");
                return;
            }
        }

        Assert.Fail("Timed out waiting for last_error to be recorded");
    }

    [Fact]
    public async Task Scheduler_Should_IncrementTimesSent_When_DispatchFails()
    {
        // Arrange - same failing middleware approach as RecordLastError test
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder, AddFailingDispatchMiddleware);

        using (var scope = env.Provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new TestEvent { Payload = "always-fails" },
                new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
                default);
        }

        // Wait for at least 2 dispatch attempts
        using var waitCts = new CancellationTokenSource(s_timeout);

        while (!waitCts.Token.IsCancellationRequested)
        {
            await Task.Delay(250, waitCts.Token);

            using var scope = env.Provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var timesSent = await db.Database
                .SqlQueryRaw<int>(
                    "SELECT TOP(1) [times_sent] AS [Value] FROM [dbo].[scheduled_messages]")
                .FirstOrDefaultAsync(waitCts.Token);

            if (timesSent >= 2)
            {
                return;
            }
        }

        Assert.Fail("Timed out waiting for times_sent to reach 2");
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_ReturnCancellableResult_When_SqlServerSchedulingConfigured()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        var result = await bus.SchedulePublishAsync(
            new TestEvent { Payload = "cancellable" },
            scheduledTime,
            default);

        // Assert
        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.StartsWith("sqlserver-scheduler:", result.Token);
        Assert.Equal(scheduledTime, result.ScheduledTime);

        // Verify row exists in the database
        using var verifyScope = env.Provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var count = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [dbo].[scheduled_messages]")
            .SingleAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_DeleteRow_When_ValidToken()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(10);

        var result = await bus.SchedulePublishAsync(
            new TestEvent { Payload = "to-cancel" },
            scheduledTime,
            default);

        // Act
        var cancelled = await bus.CancelScheduledMessageAsync(result.Token!, default);

        // Assert
        Assert.True(cancelled);

        using var verifyScope = env.Provider.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var remaining = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [dbo].[scheduled_messages]")
            .SingleAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_AlreadyDispatched()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.SchedulePublishAsync(
            new TestEvent { Payload = "dispatch-then-cancel" },
            DateTimeOffset.UtcNow,
            default);

        // Wait for handler to receive the message
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should have received the scheduled message");

        // Wait for row to be deleted after dispatch
        using var waitCts = new CancellationTokenSource(s_timeout);

        while (!waitCts.Token.IsCancellationRequested)
        {
            await Task.Delay(250, waitCts.Token);

            using var verifyScope = env.Provider.CreateScope();
            var db = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();
            var count = await db.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM [dbo].[scheduled_messages]")
                .SingleAsync(waitCts.Token);

            if (count == 0)
            {
                break;
            }
        }

        // Act
        var cancelled = await bus.CancelScheduledMessageAsync(result.Token!, default);

        // Assert
        Assert.False(cancelled);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_AlreadyCancelled()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(10);

        var result = await bus.SchedulePublishAsync(
            new TestEvent { Payload = "double-cancel" },
            scheduledTime,
            default);

        // Act
        var firstCancel = await bus.CancelScheduledMessageAsync(result.Token!, default);
        var secondCancel = await bus.CancelScheduledMessageAsync(result.Token!, default);

        // Assert
        Assert.True(firstCancel);
        Assert.False(secondCancel);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_InvalidToken()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var invalidToken = $"sqlserver-scheduler:{Guid.NewGuid()}";

        // Act
        var cancelled = await bus.CancelScheduledMessageAsync(invalidToken, default);

        // Assert
        Assert.False(cancelled);
    }

    private static void AddFailingDispatchMiddleware(IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(h =>
            h.UseDispatch(new DispatchMiddlewareConfiguration(
                static (_, _) => static _ =>
                    throw new InvalidOperationException("Simulated dispatch failure"),
                "FailingTransport")));
    }

    [Fact]
    public async Task Scheduler_Should_ProcessNewMessages_When_PublishedWhileWorkerRunning()
    {
        // Arrange
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

        // Act - publish messages at intervals while worker is running
        for (var i = 0; i < 5; i++)
        {
            using var scope = env.Provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new TestEvent { Payload = $"live-{i}" },
                new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
                default);

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
    public async Task Scheduler_Should_HandleConcurrentPublishers_When_MultipleScopes()
    {
        // Arrange
        const int scopeCount = 10;
        const int messagesPerScope = 5;
        const int totalMessages = scopeCount * messagesPerScope;
        var recorder = new MessageRecorder();
        await using var env = await CreateBusWithSchedulingAsync(recorder);

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
                        new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
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

    private async Task<TestEnvironment> CreateBusWithSchedulingAsync(
        MessageRecorder recorder,
        Action<IMessageBusHostBuilder>? configure = null)
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseSqlServer(connectionString)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

        // Register the resilient signal BEFORE UseSqlServerScheduling() so that
        // TryAddSingleton<ISchedulerSignal> in UseSchedulerCore() is a no-op.
        // This prevents ObjectDisposedException during teardown when the
        // dispatcher's own transactions fire the interceptor.
        services.AddSingleton<ISchedulerSignal>(new ResilientSchedulerSignal());

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UseSqlServerScheduling());
        builder.AddEventHandler<TestEventHandler>();
        builder.AddInMemory();

        configure?.Invoke(builder);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(default);

        // Ensure schema exists
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(default);
        }

        // Start hosted services (scheduling worker)
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var svc in hostedServices)
        {
            await svc.StartAsync(default);
        }

        return new TestEnvironment(provider, hostedServices);
    }

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
    /// A scheduler signal whose <see cref="Notify"/> never throws
    /// <see cref="ObjectDisposedException"/>. The production
    /// <c>MessageBusSchedulerSignal</c> wraps <c>AsyncAutoResetEvent</c> which
    /// throws on <c>Set()</c> after disposal. In integration tests the
    /// dispatcher's own transaction commits fire the EF Core interceptor that
    /// calls <c>Notify()</c>, and this can race with provider disposal.
    /// <para>
    /// Uses a <c>TaskCompletionSource</c> to implement auto-reset semantics.
    /// Tracks the current wake target so <c>Notify</c> only signals when the
    /// scheduled time is at or before the target, mirroring production behavior.
    /// </para>
    /// </summary>
    private sealed class ResilientSchedulerSignal : ISchedulerSignal
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        private TaskCompletionSource _tcs;
        private long _currentWakeTargetTicks = long.MaxValue;

        public ResilientSchedulerSignal()
        {
            // Start signaled so the first WaitUntilAsync returns immediately.
            _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _tcs.TrySetResult();
        }

        public void Notify(DateTimeOffset scheduledTime)
        {
            lock (_lock)
            {
                var currentTarget = Volatile.Read(ref _currentWakeTargetTicks);

                if (scheduledTime.UtcTicks <= currentTarget)
                {
                    if (!_tcs.TrySetResult())
                    {
                        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        _tcs.TrySetResult();
                    }
                }
            }
        }

        public async Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
        {
            Task notifyTask;

            lock (_lock)
            {
                Volatile.Write(ref _currentWakeTargetTicks, wakeTime.UtcTicks);

                var task = _tcs.Task;

                if (task.IsCompletedSuccessfully)
                {
                    // Reset: replace with a new, unsignaled TCS
                    _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    return;
                }

                if (task.IsCanceled || task.IsFaulted)
                {
                    // Previous waiter was cancelled - replace with fresh TCS
                    _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                // Register cancellation on the current TCS
                var tcs = _tcs;
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(
                        static state => ((TaskCompletionSource)state!).TrySetCanceled(),
                        tcs);
                }

                notifyTask = tcs.Task;
            }

            // Wait until wake time OR until Notify() signals, whichever comes first.
            var delay = wakeTime - DateTimeOffset.UtcNow;

            if (delay <= TimeSpan.Zero)
            {
                return;
            }

            if (delay > TimeSpan.FromMinutes(5))
            {
                delay = TimeSpan.FromMinutes(5);
            }

            await Task.WhenAny(notifyTask, Task.Delay(delay, cancellationToken));
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
