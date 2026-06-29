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

    [Fact]
    public async Task ScheduledMessage_Should_PreservePayload_When_PooledContextIsReusedBeforeDueTime()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var greetingResult = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var noiseSent = new SemaphoreSlim(0);

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(greetingResult);
        services.AddSingleton(noiseSent);
        services.AddMessageBus()
            .AddEventHandler<GreetingHandler>()
            .AddEventHandler<NoiseHandler>()
            .AddInMemory();

        var provider = services.BuildServiceProvider();
        await using var _ = provider;

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(ct);
        foreach (var svc in provider.GetServices<IHostedService>())
        {
            await svc.StartAsync(ct);
        }

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - schedule greeting 1 minute out
        await bus.SchedulePublishAsync(new Greeting("hello"), time.GetUtcNow().AddMinutes(1), ct);

        // publish several noise messages to reuse the pooled dispatch context and overwrite its writer buffer
        for (var i = 0; i < 5; i++)
        {
            await bus.PublishAsync(new Noise($"overwrite-{i}-padding-padding-padding-padding-padding-padding"), ct);
            Assert.True(await noiseSent.WaitAsync(TimeSpan.FromSeconds(10), ct));
        }

        // advance time past the scheduled due time
        time.Advance(TimeSpan.FromMinutes(2));

        // assert - greeting delivered with original payload intact
        var received = await greetingResult.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        Assert.Equal("hello", received);
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

    private sealed record Greeting(string Text);

    private sealed record Noise(string Content);

    private sealed record Ping;

    private sealed class GreetingHandler(TaskCompletionSource<string> result) : IEventHandler<Greeting>
    {
        public ValueTask HandleAsync(Greeting message, CancellationToken cancellationToken)
        {
            result.TrySetResult(message.Text);
            return default;
        }
    }

    private sealed class NoiseHandler(SemaphoreSlim noiseSent) : IEventHandler<Noise>
    {
        public ValueTask HandleAsync(Noise message, CancellationToken cancellationToken)
        {
            noiseSent.Release();
            return default;
        }
    }

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
