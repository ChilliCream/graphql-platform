using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Scheduling;

public class CancelScheduledMessageTests
{
    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_TokenIsNull()
    {
        await using var provider = await CreateBusAsync();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.CancelScheduledMessageAsync(null!, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_TokenIsEmpty()
    {
        await using var provider = await CreateBusAsync();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.CancelScheduledMessageAsync("", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_NoStoreRegistered()
    {
        await using var provider = await CreateBusAsync();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.CancelScheduledMessageAsync(
            "some-provider:some-value",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_DelegateToStore_When_TokenPrefixMatches()
    {
        var spy = new SpyScheduledMessageStore();
        await using var provider = await CreateBusAsync(spy);
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.CancelScheduledMessageAsync(
            "test-provider:my-cancel-value",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal("test-provider:my-cancel-value", spy.LastCancelledValue);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReturnFalse_When_TokenPrefixIsUnknown()
    {
        var spy = new SpyScheduledMessageStore();
        await using var provider = await CreateBusAsync(spy);
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await bus.CancelScheduledMessageAsync(
            "unknown:my-cancel-value",
            CancellationToken.None);

        Assert.False(result);
        Assert.Null(spy.LastCancelledValue);
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        SpyScheduledMessageStore? spyStore = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddEventHandler<StubEventHandler>();
        builder.AddInMemory();

        if (spyStore is not null)
        {
            services.AddScoped(_ => spyStore);
            services.AddSingleton(
                new ScheduledMessageStoreRegistration(
                    typeof(InMemoryMessagingTransport),
                    SpyScheduledMessageStore.TokenPrefix,
                    typeof(SpyScheduledMessageStore)));
        }

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private sealed class SpyScheduledMessageStore : IScheduledMessageStore
    {
        public const string TokenPrefix = "test-provider:";

        public string? LastCancelledValue { get; private set; }

        public ValueTask<string> PersistAsync(
            IDispatchContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult("test-provider:test-id");

        public ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
        {
            LastCancelledValue = token;
            return ValueTask.FromResult(true);
        }
    }

    private sealed class StubEvent;

    private sealed class StubEventHandler : IEventHandler<StubEvent>
    {
        public ValueTask HandleAsync(StubEvent message, CancellationToken cancellationToken) =>
            default;
    }
}
