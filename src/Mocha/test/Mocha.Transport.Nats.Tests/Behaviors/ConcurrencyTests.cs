using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record SlowWorkItem(int Sequence);

public sealed record SerialWorkItem(int Sequence);

public sealed record LimitedWorkItem(int Sequence);

[Collection(JetStreamCollection.Name)]
public class ConcurrencyTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(90);
    private const int MessageCount = 20;

    [Fact]
    public async Task Handler_Should_AllowParallelism_When_MaxConcurrencyGreaterThanOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = CreateBuilder(tracker, recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<SlowWorkItemHandler>()
            .AddNats(nats => nats
                .StreamName("e2e-parallel")
                .Endpoint("parallel-ep")
                .Handler<SlowWorkItemHandler>()
                .MaxConcurrency(5));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await PublishAsync(host, i => new SlowWorkItem(i), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(s_timeout, expectedCount: MessageCount),
                $"Only {recorder.Messages.Count} of {MessageCount} messages were handled in time.");

            Assert.True(
                tracker.PeakConcurrency > 1,
                $"Expected parallel handling, but peak concurrency was {tracker.PeakConcurrency}.");

            Assert.True(
                tracker.PeakConcurrency <= 5,
                $"Expected peak concurrency of at most 5, but it was {tracker.PeakConcurrency}.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = CreateBuilder(tracker, recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<SerialWorkItemHandler>()
            .AddNats(nats => nats
                .StreamName("e2e-serial")
                .Endpoint("serial-ep")
                .Handler<SerialWorkItemHandler>()
                .MaxConcurrency(1));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await PublishAsync(host, i => new SerialWorkItem(i), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(s_timeout, expectedCount: MessageCount),
                $"Only {recorder.Messages.Count} of {MessageCount} messages were handled in time.");

            Assert.Equal(1, tracker.PeakConcurrency);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_ConcurrencyLimiterConfigured()
    {
        // arrange
        // The bus-level concurrency limiter sits in front of the endpoint's own parallelism, so the
        // tighter of the two wins.
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = CreateBuilder(tracker, recorder);
        builder.Services
            .AddMessageBus()
            .AddConcurrencyLimiter(o => o.MaxConcurrency = 1)
            .AddEventHandler<LimitedWorkItemHandler>()
            .AddNats(nats => nats
                .StreamName("e2e-limited")
                .Endpoint("limited-ep")
                .Handler<LimitedWorkItemHandler>()
                .MaxConcurrency(5));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await PublishAsync(host, i => new LimitedWorkItem(i), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(s_timeout, expectedCount: MessageCount),
                $"Only {recorder.Messages.Count} of {MessageCount} messages were handled in time.");

            Assert.Equal(1, tracker.PeakConcurrency);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private HostApplicationBuilder CreateBuilder(ConcurrencyTracker tracker, MessageRecorder recorder)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(tracker);
        builder.Services.AddSingleton(recorder);

        return builder;
    }

    private static async Task PublishAsync(
        IHost host,
        Func<int, object> factory,
        CancellationToken cancellationToken)
    {
        var bus = host.Services.GetRequiredService<IMessageBus>();

        for (var i = 0; i < MessageCount; i++)
        {
            await bus.PublishAsync(factory(i), cancellationToken);
        }
    }

    public sealed class SlowWorkItemHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<SlowWorkItem>
    {
        public async ValueTask HandleAsync(SlowWorkItem message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(500, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }

    public sealed class SerialWorkItemHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<SerialWorkItem>
    {
        public async ValueTask HandleAsync(SerialWorkItem message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(50, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }

    public sealed class LimitedWorkItemHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<LimitedWorkItem>
    {
        public async ValueTask HandleAsync(LimitedWorkItem message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(50, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
