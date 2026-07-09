using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Scheduling;

public class SchedulingMiddlewareIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Scheduling_Should_PersistToStore_When_ScheduledTimeSetAndStoreRegistered()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(store, _ => { }, timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

        // act
        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "persist-me" },
            new PublishOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        // assert - message captured by scheduling store, not delivered to transport
        await WaitUntilAsync(() => !store.Entries.IsEmpty, s_timeout);
        var entry = Assert.Single(store.Entries);
        Assert.Equal(scheduledTime, entry.ScheduledTime);
        Assert.NotNull(entry.Envelope);
    }

    [Fact]
    public async Task Scheduling_Should_SignalWorker_When_MessagePersisted()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var signal = new TestSchedulerSignal();
        var store = new InMemoryScheduledMessageStore(signal);
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            b => b.Services.AddSingleton<ISchedulerSignal>(signal),
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "signal-test" },
            new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddMinutes(5) },
            CancellationToken.None);

        // assert - signal was set after persist
        await WaitUntilAsync(() => signal.SignalCount > 0, s_timeout);
        Assert.True(signal.SignalCount >= 1, "Signal should have been set at least once");
    }

    [Fact]
    public async Task Scheduling_Should_PassThrough_When_ScheduledTimeIsNull()
    {
        // arrange
        var store = new InMemoryScheduledMessageStore();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<SchedulingTestEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish without ScheduledTime
        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "immediate" },
            CancellationToken.None);

        // assert - delivered to handler, not captured by store
        Assert.True(await recorder.WaitAsync(s_timeout), "Message should be delivered to handler");
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task Scheduling_Should_PassThrough_When_SkipSchedulerSet()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<SchedulingTestEventHandler>();

                // Add a middleware before scheduling that sets SkipScheduler
                b.ConfigureMessageBus(h =>
                    h.UseDispatch(
                        new DispatchMiddlewareConfiguration(
                            static (_, next) =>
                                ctx =>
                                {
                                    ctx.SkipScheduler();
                                    return next(ctx);
                                },
                            "SkipSchedulerCheck"),
                        before: "Scheduling"));
            },
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish with ScheduledTime but SkipScheduler set.
        // Use a past time so the InMemory transport's Task.Delay is zero when message passes through.
        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "skip-scheduler" },
            new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddSeconds(-1) },
            CancellationToken.None);

        // assert - delivered to handler, not captured by store
        Assert.True(await recorder.WaitAsync(s_timeout), "Message should be delivered to handler");
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_ReturnCancellableResult_When_StoreRegistered()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            _ => { },
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

        // act
        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "schedule-me" },
            scheduledTime,
            CancellationToken.None);

        // assert
        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.Equal(scheduledTime, result.ScheduledTime);
        await WaitUntilAsync(() => !store.Entries.IsEmpty, s_timeout);
        Assert.Single(store.Entries);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_RemoveFromStore_When_ValidToken()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAndProviderAsync(
            store,
            _ => { },
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "cancel-me" },
            scheduledTime,
            CancellationToken.None);

        await WaitUntilAsync(() => store.TrackedCount > 0, s_timeout);
        Assert.Equal(1, store.TrackedCount);

        // act
        var cancelled = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.True(cancelled);
        Assert.Equal(0, store.TrackedCount);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_AlreadyCancelled()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAndProviderAsync(
            store,
            _ => { },
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "cancel-twice" },
            scheduledTime,
            CancellationToken.None);

        await WaitUntilAsync(() => store.TrackedCount > 0, s_timeout);

        // act
        var firstCancel = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);
        var secondCancel = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.True(firstCancel);
        Assert.False(secondCancel);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_ReturnNonCancellable_When_NoStoreRegistered()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);

        var builder = services.AddMessageBus();
        builder.UseSchedulerCore();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        await using (provider)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

            // act
            var result = await bus.SchedulePublishAsync(
                new SchedulingTestEvent { Payload = "no-store" },
                scheduledTime,
                CancellationToken.None);

            // assert
            Assert.False(result.IsCancellable);
            Assert.Null(result.Token);
        }
    }

    [Fact]
    public async Task Scheduling_Should_PassThrough_When_NoStoreRegistered()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var recorder = new MessageRecorder();

        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddSingleton<TimeProvider>(timeProvider);

        var builder = services.AddMessageBus();
        builder.UseSchedulerCore();
        builder.AddEventHandler<SchedulingTestEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        await using (provider)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // act - publish with ScheduledTime but no store registered
            await bus.PublishAsync(
                new SchedulingTestEvent { Payload = "no-store" },
                new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddSeconds(-1) },
                CancellationToken.None);

            // assert - message delivered to handler since middleware was skipped (no store)
            Assert.True(await recorder.WaitAsync(s_timeout), "Message should be delivered to handler");
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    /// <summary>
    /// Creates a middleware configuration that always inserts the scheduling middleware,
    /// bypassing the factory-time IScheduledMessageStore check (which requires bus-internal DI).
    /// The middleware itself resolves the store at dispatch time from the scoped host DI.
    /// </summary>
    private static DispatchMiddlewareConfiguration CreateSchedulingMiddleware()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchSchedulingMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Scheduling");

    private static async Task<ServiceProvider> CreateBusWithSchedulingAsync(
        InMemoryScheduledMessageStore store,
        Action<IMessageBusHostBuilder> configure,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStore>(_ => store);
        services.AddSingleton<ISchedulerSignal, TestSchedulerSignal>();

        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        var builder = services.AddMessageBus();

        // Register middleware directly (bypassing factory-time DI check)
        builder.ConfigureMessageBus(x => x.UseDispatch(CreateSchedulingMiddleware()));

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private static async Task<ServiceProvider> CreateBusWithSchedulingAndProviderAsync(
        InMemoryScheduledMessageStore store,
        Action<IMessageBusHostBuilder> configure,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IScheduledMessageStore>(_ => store);
        services.AddSingleton<ISchedulerSignal, TestSchedulerSignal>();

        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        var builder = services.AddMessageBus();

        // Register middleware directly (bypassing factory-time DI check)
        builder.ConfigureMessageBus(x => x.UseDispatch(CreateSchedulingMiddleware()));

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class SchedulingTestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class SchedulingTestEventHandler(MessageRecorder recorder) : IEventHandler<SchedulingTestEvent>
    {
        public ValueTask HandleAsync(SchedulingTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// In-memory scheduled message store that captures envelopes for test assertions.
    /// Mirrors the real store's contract by signaling after persistence.
    /// </summary>
    public sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
    {
        private readonly ISchedulerSignal? _signal;
        private readonly ConcurrentDictionary<string, (MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> _entriesById = new();

        public InMemoryScheduledMessageStore(ISchedulerSignal? signal = null)
        {
            _signal = signal;
        }

        public ConcurrentBag<(MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> Entries { get; } = [];

        public ValueTask<string> PersistAsync(MessageEnvelope envelope, DateTimeOffset scheduledTime, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            Entries.Add((envelope, scheduledTime));
            _entriesById[id] = (envelope, scheduledTime);
            _signal?.Notify(scheduledTime);
            var token = $"in-memory:{id}";
            return ValueTask.FromResult(token);
        }

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            var value = token.StartsWith("in-memory:", StringComparison.Ordinal)
                ? token["in-memory:".Length..]
                : token;
            var removed = _entriesById.TryRemove(value, out _);
            return ValueTask.FromResult(removed);
        }

        public bool HasEntry(string id) => _entriesById.ContainsKey(id);

        public int TrackedCount => _entriesById.Count;
    }

    /// <summary>
    /// Test signal that records how many times it was set.
    /// </summary>
    public sealed class TestSchedulerSignal : ISchedulerSignal
    {
        private int _signalCount;

        public int SignalCount => _signalCount;

        public void Notify(DateTimeOffset scheduledTime) => Interlocked.Increment(ref _signalCount);

        public Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
