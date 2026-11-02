using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class ConcurrencyLimiterConfigurationExtensionsTests
{
    [Fact]
    public async Task AddConcurrencyLimiter_Should_ConfigureMiddleware_When_CalledOnHost()
    {
        // arrange & act
        await using var provider = await CreateBusAsync(builder =>
        {
            builder.AddConcurrencyLimiter(options =>
            {
                options.Enabled = true;
                options.MaxConcurrency = 5;
            });
            builder.AddEventHandler<NormalEventHandler>();
        });

        // assert - configuration was accepted and runtime started successfully
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task AddConcurrencyLimiter_Should_AllowMultipleCalls()
    {
        // arrange & act
        await using var provider = await CreateBusAsync(builder =>
        {
            builder.AddConcurrencyLimiter(options => options.Enabled = true);
            builder.AddConcurrencyLimiter(options => options.MaxConcurrency = 10);
            builder.AddEventHandler<NormalEventHandler>();
        });

        // assert - multiple limiter configurations accepted; runtime started
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class TestEvent
    {
        public required string Data { get; init; }
    }

    public sealed class NormalEventHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }
}
