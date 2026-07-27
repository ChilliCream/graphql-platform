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

        // act & assert - dispose completes without throwing.
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

        // assert - bus is a DefaultMessageBus registered by AddMessageBus
        Assert.IsType<DefaultMessageBus>(bus);
    }

    [Fact]
    public void Runtime_Should_CreateConsumersPerBuild_When_ServiceCollectionIsBuiltTwice()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddConsumer(static () => ConsumerFactory.Subscribe<TestEventHandler, TestEvent>());
        builder.ConfigureMessageBus(b => b.AddTransport(new InMemoryMessagingTransport(_ => { })));

        // act
        var runtime1 = (MessagingRuntime)services.BuildServiceProvider().GetRequiredService<IMessagingRuntime>();
        var runtime2 = (MessagingRuntime)services.BuildServiceProvider().GetRequiredService<IMessagingRuntime>();

        // assert
        var consumer1 = Assert.Single(runtime1.Consumers, c => c.Identity == typeof(TestEventHandler));
        var consumer2 = Assert.Single(runtime2.Consumers, c => c.Identity == typeof(TestEventHandler));
        Assert.NotSame(consumer1, consumer2);
    }

    [Fact]
    public void AddMessage_Should_RegisterOnce_When_CalledTwiceForSameType()
    {
        // arrange & act
        // A generator-emitted AddMessage plus a user-written one (or two modules sharing a type) register the
        // same message twice. The second registration must be a no-op instead of throwing on the message-type
        // dictionary, and the type must resolve to a single MessageType.
        var runtime = CreateRuntime(b =>
        {
            b.AddMessage<TestEvent>();
            b.AddMessage<TestEvent>();
        });

        // assert
        Assert.Single(runtime.Messages.MessageTypes, m => m.RuntimeType == typeof(TestEvent));
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
