using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(store, _ => { }, timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "persist-me" },
            new PublishOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        await WaitUntilAsync(() => !store.Entries.IsEmpty, s_timeout);
        var entry = Assert.Single(store.Entries);
        Assert.Equal(scheduledTime, entry.ScheduledTime);
        Assert.NotNull(entry.Envelope);
    }

    [Fact]
    public async Task Scheduling_Should_SignalWorker_When_MessagePersisted()
    {
        var timeProvider = new FakeTimeProvider();
        var signal = new TestSchedulerSignal();
        var store = new InMemoryScheduledMessageStore(signal);
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            b => b.Services.AddSingleton<ISchedulerSignal>(signal),
            timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "signal-test" },
            new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddMinutes(5) },
            CancellationToken.None);

        await WaitUntilAsync(() => signal.SignalCount > 0, s_timeout);
        Assert.True(signal.SignalCount >= 1, "Signal should have been set at least once");
    }

    [Fact]
    public async Task Scheduling_Should_PassThrough_When_ScheduledTimeIsNull()
    {
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

        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "immediate" },
            CancellationToken.None);

        Assert.True(await recorder.WaitAsync(s_timeout), "Message should be delivered to handler");
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task Scheduling_Should_PassThrough_When_SkipSchedulerSet()
    {
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithSchedulingAsync(
            store,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<SchedulingTestEventHandler>();

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

        await bus.PublishAsync(
            new SchedulingTestEvent { Payload = "skip-scheduler" },
            new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddSeconds(-1) },
            CancellationToken.None);

        Assert.True(await recorder.WaitAsync(s_timeout), "Message should be delivered to handler");
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_ReturnCancellableResult_When_StoreRegistered()
    {
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(store, _ => { }, timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "schedule-me" },
            scheduledTime,
            CancellationToken.None);

        Assert.True(result.IsCancellable);
        Assert.NotNull(result.Token);
        Assert.Equal(scheduledTime, result.ScheduledTime);
        await WaitUntilAsync(() => !store.Entries.IsEmpty, s_timeout);
        Assert.Single(store.Entries);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_RemoveFromStore_When_ValidToken()
    {
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(store, _ => { }, timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "cancel-me" },
            scheduledTime,
            CancellationToken.None);

        await WaitUntilAsync(() => store.TrackedCount > 0, s_timeout);
        Assert.Equal(1, store.TrackedCount);

        var cancelled = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        Assert.True(cancelled);
        Assert.Equal(0, store.TrackedCount);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_AlreadyCancelled()
    {
        var timeProvider = new FakeTimeProvider();
        var store = new InMemoryScheduledMessageStore();
        await using var provider = await CreateBusWithSchedulingAsync(store, _ => { }, timeProvider);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);
        var result = await bus.SchedulePublishAsync(
            new SchedulingTestEvent { Payload = "cancel-twice" },
            scheduledTime,
            CancellationToken.None);

        await WaitUntilAsync(() => store.TrackedCount > 0, s_timeout);

        var firstCancel = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);
        var secondCancel = await bus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        Assert.True(firstCancel);
        Assert.False(secondCancel);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_ThrowNotSupported_When_NoStoreRegistered()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);

        var builder = services.AddMessageBus();
        var transport = new InMemoryMessagingTransport(static _ => { });
        builder.ConfigureMessageBus(b => b.AddTransport(transport));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        await using (provider)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var scheduledTime = timeProvider.GetUtcNow().AddMinutes(10);

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await bus.SchedulePublishAsync(
                    new SchedulingTestEvent { Payload = "no-store" },
                    scheduledTime,
                    CancellationToken.None));
        }
    }

    [Fact]
    public async Task Scheduling_Should_ThrowNotSupported_When_NoStoreRegistered()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);

        var builder = services.AddMessageBus();
        builder.AddEventHandler<SchedulingTestEventHandler>();
        var transport = new InMemoryMessagingTransport(static _ => { });
        builder.ConfigureMessageBus(b => b.AddTransport(transport));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        await using (provider)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await bus.PublishAsync(
                    new SchedulingTestEvent { Payload = "no-store" },
                    new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().AddSeconds(-1) },
                    CancellationToken.None));
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

    private static async Task<ServiceProvider> CreateBusWithSchedulingAsync(
        InMemoryScheduledMessageStore store,
        Action<IMessageBusHostBuilder> configure,
        TimeProvider? timeProvider = null)
    {
        var transport = new InMemoryMessagingTransport(static _ => { });
        var services = new ServiceCollection();
        services.AddSingleton(
            new ScheduledMessageStoreRegistration(transport, InMemoryScheduledMessageStore.TokenPrefix, _ => store));

        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        var builder = services.AddMessageBus();
        configure(builder);
        services.TryAddSingleton<ISchedulerSignal, TestSchedulerSignal>();
        builder.ConfigureMessageBus(b => b.AddTransport(transport));

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

    public sealed class InMemoryScheduledMessageStore : IScheduledMessageStore
    {
        public const string TokenPrefix = "in-memory:";

        private readonly ISchedulerSignal? _signal;
        private readonly ConcurrentDictionary<string, (MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> _entriesById = new();

        public InMemoryScheduledMessageStore(ISchedulerSignal? signal = null)
        {
            _signal = signal;
        }

        public ConcurrentBag<(MessageEnvelope Envelope, DateTimeOffset ScheduledTime)> Entries { get; } = [];

        public ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
        {
            var envelope = context.Envelope ?? throw new InvalidOperationException("Envelope is not set");
            var scheduledTime = envelope.ScheduledTime
                ?? throw new InvalidOperationException("Scheduled time is not set");
            var id = Guid.NewGuid().ToString();
            Entries.Add((envelope, scheduledTime));
            _entriesById[id] = (envelope, scheduledTime);
            _signal?.Notify(scheduledTime);
            var token = TokenPrefix + id;
            return ValueTask.FromResult(token);
        }

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            var value = token.StartsWith(TokenPrefix, StringComparison.Ordinal)
                ? token[TokenPrefix.Length..]
                : token;
            var removed = _entriesById.TryRemove(value, out _);
            return ValueTask.FromResult(removed);
        }

        public bool HasEntry(string id) => _entriesById.ContainsKey(id);

        public int TrackedCount => _entriesById.Count;
    }

    public sealed class TestSchedulerSignal : ISchedulerSignal
    {
        private int _signalCount;

        public int SignalCount => _signalCount;

        public void Notify(DateTimeOffset scheduledTime) => Interlocked.Increment(ref _signalCount);

        public Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
