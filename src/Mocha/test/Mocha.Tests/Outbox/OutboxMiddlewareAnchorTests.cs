using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Outbox;

public class OutboxMiddlewareAnchorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_Should_RunMiddlewareAnchoredAfterScheduling_When_MessageIsStored(
        bool anchorRegisteredFirst)
    {
        // arrange
        var outbox = new CapturingOutbox();
        await using var provider = await CreateProviderAsync(
            outbox,
            anchorRegisteredFirst,
            bus => bus.UseDispatch(SetHeader("after-scheduling"), after: DispatchMiddlewares.Scheduling.Key),
            static _ => { });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new AnchorEvent { Payload = "stored" }, CancellationToken.None);

        // assert
        var envelope = Assert.Single(outbox.Envelopes);
        Assert.Equal("after-scheduling", envelope.Headers!.GetValue("x-anchor"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_Should_RunMiddlewareAnchoredBeforeOutbox_When_MessageIsStored(
        bool anchorRegisteredFirst)
    {
        // arrange
        var outbox = new CapturingOutbox();
        await using var provider = await CreateProviderAsync(
            outbox,
            anchorRegisteredFirst,
            bus => bus.UseDispatch(SetHeader("before-outbox"), before: OutboxDispatchMiddlewares.Outbox.Key),
            static _ => { });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new AnchorEvent { Payload = "stored" }, CancellationToken.None);

        // assert
        var envelope = Assert.Single(outbox.Envelopes);
        Assert.Equal("before-outbox", envelope.Headers!.GetValue("x-anchor"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_Should_SendDirectly_When_DocumentedSkipOutboxSampleIsRegistered(
        bool anchorRegisteredFirst)
    {
        // arrange
        var outbox = new CapturingOutbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateProviderAsync(
            outbox,
            anchorRegisteredFirst,
            bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => context =>
                    {
                        if (context.Message is CacheInvalidatedEvent)
                        {
                            context.SkipOutbox();
                        }

                        return next(context);
                    },
                    "SkipOutboxForCacheEvents"),
                before: OutboxDispatchMiddlewares.Outbox.Key),
            builder =>
            {
                builder.Services.AddSingleton(recorder);
                builder.AddEventHandler<CacheInvalidatedEventHandler>();
            });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new CacheInvalidatedEvent { Key = "products" }, CancellationToken.None);
        await bus.PublishAsync(new AnchorEvent { Payload = "stored" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Equal("products", ((CacheInvalidatedEvent)Assert.Single(recorder.Messages)).Key);
        Assert.Equal(
            "stored",
            JsonSerializer.Deserialize<AnchorEvent>(Assert.Single(outbox.Envelopes).Body.Span)!.Payload);
    }

    [Fact]
    public async Task PublishAsync_Should_RejectMessage_When_MiddlewareClearsEnvelopeAfterSerialization()
    {
        // arrange
        var outbox = new CapturingOutbox();
        await using var provider = await CreateProviderAsync(
            outbox,
            anchorRegisteredFirst: false,
            bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => context =>
                    {
                        context.Envelope = null;
                        return next(context);
                    },
                    "ClearEnvelope"),
                after: DispatchMiddlewares.Serialization.Key),
            static _ => { });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await bus.PublishAsync(new AnchorEvent { Payload = "cleared" }, CancellationToken.None));

        // assert
        Assert.Equal("Envelope is not set", exception.Message);
        Assert.Empty(outbox.Envelopes);
    }

    private static DispatchMiddlewareConfiguration SetHeader(string value)
        => new(
            (_, next) => context =>
            {
                context.Headers.Set("x-anchor", value);
                return next(context);
            },
            "SetAnchorHeader");

    private static async Task<ServiceProvider> CreateProviderAsync(
        CapturingOutbox outbox,
        bool anchorRegisteredFirst,
        Action<IMessageBusBuilder> configureAnchor,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageOutbox>(outbox);
        var builder = services.AddMessageBus();
        configure(builder);

        if (anchorRegisteredFirst)
        {
            builder.ConfigureMessageBus(configureAnchor);
            builder.UseOutboxCore();
        }
        else
        {
            builder.UseOutboxCore();
            builder.ConfigureMessageBus(configureAnchor);
        }

        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        try
        {
            var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
            await runtime.StartAsync(CancellationToken.None);
            return provider;
        }
        catch
        {
            await provider.DisposeAsync();
            throw;
        }
    }

    public sealed class AnchorEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class CacheInvalidatedEvent
    {
        public required string Key { get; init; }
    }

    public sealed class CacheInvalidatedEventHandler(MessageRecorder recorder) : IEventHandler<CacheInvalidatedEvent>
    {
        public ValueTask HandleAsync(CacheInvalidatedEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    private sealed class CapturingOutbox : IMessageOutbox
    {
        public ConcurrentBag<MessageEnvelope> Envelopes { get; } = [];

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(envelope));
            return ValueTask.CompletedTask;
        }
    }
}
