using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Infrastructure;

/// <summary>
/// Every bus operation rents a <see cref="DispatchContext"/> from the pool and has to return it, so
/// that a steady stream of messages does not allocate a context per message.
/// </summary>
public class DispatchContextPoolingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ReplyAsync_Should_ReturnPooledContext_When_ReplySucceeds()
    {
        // arrange
        var pool = new CountingDispatchContextPool();
        var services = new ServiceCollection();
        services.AddSingleton<ObjectPool<DispatchContext>>(pool);
        var builder = services.AddMessageBus();
        builder.AddRequestHandler<PoolingRequestHandler>();
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - the request rents a context, and the reply rents another on the consumer side
        var response = await bus.RequestAsync(new PoolingRequest(), CancellationToken.None);

        // the reply is dispatched on the consumer's flow, so let it unwind before counting
        var deadline = DateTime.UtcNow + s_timeout;
        while (pool.Rented != pool.Returned && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.NotNull(response);
        Assert.Equal(pool.Rented, pool.Returned);
    }

    [Fact]
    public async Task ReplyAsync_Should_PropagateAndReturnPooledContext_When_DispatchThrows()
    {
        // The pooled context is returned from a finally rather than a catch, so the exception has to
        // keep propagating to the caller exactly as it did when the catch rethrew it.

        // arrange
        var pool = new CountingDispatchContextPool();
        var services = new ServiceCollection();
        services.AddSingleton<ObjectPool<DispatchContext>>(pool);
        var builder = services.AddMessageBus();
        builder.AddRequestHandler<PoolingRequestHandler>();
        builder.ConfigureMessageBus(b => b.UseDispatch(ThrowOnReplyMiddleware.Create()));
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var replyAddress = runtime.Transports.Single().ReplyReceiveEndpoint!.Source.Address!;

        var options = new ReplyOptions
        {
            Headers = [],
            CorrelationId = Guid.NewGuid().ToString(),
            ReplyAddress = replyAddress
        };

        // act & assert - the dispatch failure still surfaces to the caller
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await bus.ReplyAsync(new PoolingResponse(), options, CancellationToken.None));

        Assert.Equal("dispatch failed", ex.Message);

        // assert - and the context was still handed back
        Assert.Equal(pool.Rented, pool.Returned);
    }

    private sealed class ThrowOnReplyMiddleware
    {
        public static DispatchMiddlewareConfiguration Create()
            => new(
                static (_, next) => ctx =>
                    ctx.Headers.Get(MessageHeaders.MessageKind) == MessageKind.Reply
                        ? throw new InvalidOperationException("dispatch failed")
                        : next(ctx),
                "ThrowOnReply");
    }

    private sealed class CountingDispatchContextPool : ObjectPool<DispatchContext>
    {
        private readonly DispatchContextPool _inner = new();
        private int _rented;
        private int _returned;

        public int Rented => Volatile.Read(ref _rented);

        public int Returned => Volatile.Read(ref _returned);

        public override DispatchContext Get()
        {
            Interlocked.Increment(ref _rented);
            return _inner.Get();
        }

        public override void Return(DispatchContext obj)
        {
            Interlocked.Increment(ref _returned);
            _inner.Return(obj);
        }
    }

    public sealed record PoolingRequest : IEventRequest<PoolingResponse>;

    public sealed record PoolingResponse;

    public sealed class PoolingRequestHandler : IEventRequestHandler<PoolingRequest, PoolingResponse>
    {
        public ValueTask<PoolingResponse> HandleAsync(
            PoolingRequest request,
            CancellationToken cancellationToken)
            => new(new PoolingResponse());
    }
}
