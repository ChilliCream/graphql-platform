using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Mocha.Transport.InMemory.Tests.Scheduling;

public class InMemorySchedulingTests
{
    [Fact]
    public async Task ScheduledMessage_Should_NotDeliver_Before_DueTime()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (provider, time, recorder) = await StartAsync(ct);
        await using var _ = provider;
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - schedule 1 minute out, advance only 30 seconds
        await bus.SchedulePublishAsync(new Ping(), time.GetUtcNow().AddMinutes(1), ct);
        time.Advance(TimeSpan.FromSeconds(30));

        // assert - not delivered within a short grace period
        Assert.False(await recorder.Signal.WaitAsync(TimeSpan.FromMilliseconds(300), ct));
    }

    [Fact]
    public async Task ScheduledMessage_Should_Deliver_After_DueTime()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (provider, time, recorder) = await StartAsync(ct);
        await using var _ = provider;
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SchedulePublishAsync(new Ping(), time.GetUtcNow().AddMinutes(1), ct);
        time.Advance(TimeSpan.FromMinutes(2));

        // assert
        Assert.True(await recorder.Signal.WaitAsync(TimeSpan.FromSeconds(10), ct));
    }

    [Fact]
    public async Task ScheduledMessage_Should_NotDeliver_When_Cancelled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (provider, time, recorder) = await StartAsync(ct);
        await using var _ = provider;
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await bus.SchedulePublishAsync(new Ping(), time.GetUtcNow().AddMinutes(1), ct);
        Assert.NotNull(result.Token);
        await bus.CancelScheduledMessageAsync(result.Token, ct);
        time.Advance(TimeSpan.FromMinutes(2));

        // assert
        Assert.False(await recorder.Signal.WaitAsync(TimeSpan.FromMilliseconds(300), ct));
    }

    private static async Task<(ServiceProvider provider, FakeTimeProvider time, PingRecorder recorder)> StartAsync(
        CancellationToken cancellationToken)
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var recorder = new PingRecorder();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(recorder);
        services.AddMessageBus()
            .AddEventHandler<PingHandler>()
            .AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(cancellationToken);
        foreach (var svc in provider.GetServices<IHostedService>())
        {
            await svc.StartAsync(cancellationToken);
        }

        return (provider, time, recorder);
    }

    private sealed record Ping;

    private sealed class PingRecorder
    {
        public SemaphoreSlim Signal { get; } = new(0);
    }

    private sealed class PingHandler(PingRecorder recorder) : IEventHandler<Ping>
    {
        public ValueTask HandleAsync(Ping message, CancellationToken cancellationToken)
        {
            recorder.Signal.Release();
            return default;
        }
    }
}
