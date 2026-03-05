using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MessagingRuntimeTests
{
    [Fact]
    public void Runtime_Should_NotBeStarted_When_JustBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<TestEventHandler>());

        // assert
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task Runtime_Should_BeStarted_When_StartAsyncCalled()
    {
        // arrange
        await using var provider = await CreateBusAsync(b => b.AddEventHandler<TestEventHandler>());

        // act
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // assert
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task Runtime_Should_CompleteWithoutError_When_DisposeAsyncCalled()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<TestEventHandler>());

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);

        // act & assert — dispose completes without throwing.
        // No observable state change beyond clean disposal; the runtime
        // does not expose a "disposed" flag.
        await runtime.DisposeAsync();
    }

    [Fact]
    public async Task DefaultMessageBus_Should_BeRegistered_When_BuiltWithAddMessageBus()
    {
        // arrange
        await using var provider = await CreateBusAsync(b => b.AddEventHandler<TestEventHandler>());

        using var scope = provider.CreateScope();

        // act
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // assert — bus is a DefaultMessageBus registered by AddMessageBus
        Assert.IsType<DefaultMessageBus>(bus);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
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
