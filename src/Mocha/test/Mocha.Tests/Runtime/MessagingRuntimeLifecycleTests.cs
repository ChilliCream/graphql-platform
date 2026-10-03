using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MessagingRuntimeLifecycleTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DisposeAsync_Should_Complete_When_RuntimeNeverStarted()
    {
        // arrange
        var provider = CreateProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        await provider.DisposeAsync();

        // assert
        Describe(runtime).MatchInlineSnapshot(
            """
            {
              "Runtime": false,
              "Transports": false,
              "ReceiveEndpoints": false
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_BeIdempotent_When_RuntimeAlreadyStopped()
    {
        // arrange
        await using var provider = CreateProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        await runtime.StopAsync(TestContext.Current.CancellationToken);

        // act
        await runtime.StopAsync(TestContext.Current.CancellationToken);

        // assert
        Describe(runtime).MatchInlineSnapshot(
            """
            {
              "Runtime": false,
              "Transports": false,
              "ReceiveEndpoints": false
            }
            """);
    }

    [Fact]
    public async Task StartAsync_Should_ThrowObjectDisposedException_When_RuntimeDisposedTwice()
    {
        // arrange
        await using var provider = CreateProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);

        // act
        await runtime.DisposeAsync();
        await runtime.DisposeAsync();

        // assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask());
    }

    [Theory]
    [InlineData("runtime")]
    [InlineData("transport")]
    [InlineData("endpoint")]
    public async Task StartAsync_Should_RejectStart_When_ComponentWasStopped(string component)
    {
        // arrange
        await using var provider = CreateProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        await runtime.StopAsync(TestContext.Current.CancellationToken);

        // act
        var exception = await Record.ExceptionAsync(() => component switch
        {
            "runtime" => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask(),
            "transport" => runtime.Transports.Single().StartAsync(runtime, TestContext.Current.CancellationToken).AsTask(),
            _ => runtime.Transports.Single().ReceiveEndpoints.First().StartAsync(runtime, TestContext.Current.CancellationToken).AsTask()
        });

        // assert
        Assert.IsType<InvalidOperationException>(exception);
        Describe(runtime).MatchInlineSnapshot(
            """
            {
              "Runtime": false,
              "Transports": false,
              "ReceiveEndpoints": false
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_StopRuntime_When_CalledWhileStartIsInProgress()
    {
        // arrange
        using var startInProgress = new ManualResetEventSlim();
        using var releaseStart = new ManualResetEventSlim();
        var services = new ServiceCollection();
        services.AddMessageBus().AddEventHandler<TestEventHandler>().AddInMemory()
            .ConfigureMessageBus(b => b.ConfigureServices(s =>
                s.AddTransient<ILogger<ReceiveEndpoint>>(_ =>
                {
                    // the start of the first endpoint blocks until the test releases it
                    if (!startInProgress.IsSet)
                    {
                        startInProgress.Set();
                        releaseStart.Wait(s_timeout, TestContext.Current.CancellationToken);
                    }

                    return NullLogger<ReceiveEndpoint>.Instance;
                })));
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var start = Task.Run(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask(),
            TestContext.Current.CancellationToken);
        Assert.True(startInProgress.Wait(s_timeout, TestContext.Current.CancellationToken), "The start should be in progress");

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        var stopCompletedDuringStart = await Task.WhenAny(stop, Task.Delay(200, TestContext.Current.CancellationToken)) == stop;
        releaseStart.Set();
        await Task.WhenAll(start, stop).WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            StopCompletedDuringStart = stopCompletedDuringStart,
            RuntimeStarted = runtime.IsStarted,
            StartedEndpoints = runtime.Transports.SelectMany(t => t.ReceiveEndpoints).Count(e => e.IsStarted)
        }.MatchInlineSnapshot(
            """
            {
              "StopCompletedDuringStart": false,
              "RuntimeStarted": false,
              "StartedEndpoints": 0
            }
            """);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddMessageBus().AddEventHandler<TestEventHandler>().AddInMemory();
        return services.BuildServiceProvider();
    }

    private static RuntimeState Describe(MessagingRuntime runtime)
        => new(
            runtime.IsStarted,
            runtime.Transports.All(t => t.IsStarted),
            runtime.Transports.SelectMany(t => t.ReceiveEndpoints).All(e => e.IsStarted));

    private sealed record RuntimeState(bool Runtime, bool Transports, bool ReceiveEndpoints);

    public sealed class TestEvent
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            return default;
        }
    }
}
