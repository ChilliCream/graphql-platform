using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Tests;

public sealed class ConsumerResilienceMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Resilience_Should_UseFreshCompleteAttemptEnvironment_When_StrategyRetries()
    {
        // arrange
        var capture = new ResilienceCapture();
        capture.FailuresBeforeSuccess = 3;
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        services.AddScoped<ResilienceProbe>();
        services.AddDbContext<ResilienceDbContext>(options =>
            options
                .UseSqlite("Data Source=:memory:")
                .ReplaceService<IExecutionStrategyFactory, RetryExecutionStrategyFactory>());
        var builder = services.AddMessageBus()
            .AddEntityFramework<ResilienceDbContext>(p => p.UseResilience())
            .AddResilience(p => p.On<RetryExecutionException>().Retry(1))
            .AddConsumer<ResilienceConsumer>();
        builder.ConfigureMessageBus(b => b.UseReceive(new ReceiveMiddlewareConfiguration(
            (_, next) => async context =>
            {
                context.Features.Set(capture.ReceiveFeature);
                await next(context);
            },
            "ReceiveFeature")));
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new ResilienceMessage { Value = "original" },
            CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await capture.Disposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(
            (4, 4, 4),
            (
                capture.DbContextIds.Distinct().Count(),
                capture.ProbeIds.Distinct().Count(),
                capture.DisposedProbeIds.Distinct().Count()));
        Assert.Equal(
            [(true, false, true), (true, false, true), (true, false, true), (true, false, true)],
            capture.AccessorWasValid
                .Zip(capture.FeatureWasPresent, capture.ReceiveFeatureWasShared)
                .Select(static values => (values.First, values.Second, values.Third)));
        Assert.Equal([0, 1, 2, 3], capture.ImmediateRetryCounts);
        Assert.Equal([true, true, true, true], capture.ConsumerWasPresent);
    }

    [Fact]
    public async Task Resilience_Should_PreserveTypedBatchContextAndFreshScope_When_StrategyRetries()
    {
        // arrange
        var capture = new ResilienceCapture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        services.AddScoped<ResilienceProbe>();
        services.AddDbContext<ResilienceDbContext>(options =>
            options
                .UseSqlite("Data Source=:memory:")
                .ReplaceService<IExecutionStrategyFactory, RetryExecutionStrategyFactory>());
        var builder = services.AddMessageBus()
            .AddEntityFramework<ResilienceDbContext>(p => p.UseResilience())
            .AddBatchHandler<BatchResilienceHandler>(options => options.MaxBatchSize = 1);
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new ResilienceMessage { Value = "original" },
            CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await capture.Disposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(
            (2, 2),
            (capture.DbContextIds.Distinct().Count(), capture.ProbeIds.Distinct().Count()));
        Assert.Equal(
            [(true, false, 0, true), (true, false, 1, true)],
            capture.AccessorWasValid
                .Select((value, index) =>
                    (
                        value,
                        capture.FeatureWasPresent[index],
                        capture.ImmediateRetryCounts[index],
                        capture.ConsumerWasPresent[index])));
    }

    [Theory]
    [InlineData("EntityFrameworkTransaction", "Inbox", "Custom")]
    [InlineData("Inbox", "Custom", "EntityFrameworkTransaction")]
    [InlineData("EntityFrameworkTransaction", "Custom", "Inbox")]
    [InlineData("Custom", "Inbox", "EntityFrameworkTransaction")]
    [InlineData("Inbox", "EntityFrameworkTransaction", "Custom")]
    [InlineData("Custom", "EntityFrameworkTransaction", "Inbox")]
    public async Task Pipeline_Should_PreserveRegistrationOrder_When_UsingPersistenceMiddlewareKeys(
        string first,
        string second,
        string third)
    {
        // arrange
        var capture = new MiddlewareOrderCapture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        var builder = services.AddMessageBus()
            .AddEventHandler<MiddlewareOrderHandler>();
        builder.ConfigureMessageBus(b =>
        {
            b.UseConsume(CreateOrderMiddleware(first));
            b.UseConsume(CreateOrderMiddleware(second));
            b.UseConsume(CreateOrderMiddleware(third));
        });
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new MiddlewareOrderMessage(), CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(
            [first, second, third, "Handler"],
            capture.Entries);
    }

    private static ConsumerMiddlewareConfiguration CreateOrderMiddleware(string key)
        => new(
            (_, next) => context => InvokeOrderMiddlewareAsync(key, context, next),
            key);

    private static async ValueTask InvokeOrderMiddlewareAsync(
        string key,
        IConsumeContext context,
        ConsumerDelegate next)
    {
        context.Services.GetRequiredService<MiddlewareOrderCapture>().Entries.Add(key);
        await next(context);
    }

    public sealed class ResilienceDbContext(DbContextOptions<ResilienceDbContext> options)
        : DbContext(options);

    public sealed class RetryExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
        : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryExecutionStrategy(dependencies);
    }

    private sealed class RetryExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception)
            => exception is RetryExecutionException;
    }

    public sealed class ResilienceConsumer(
        ResilienceDbContext dbContext,
        ResilienceProbe probe,
        ConsumeContextAccessor accessor,
        ResilienceCapture capture)
        : IConsumer<ResilienceMessage>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ResilienceMessage> context)
        {
            var receiveFeature = context.Features.Get<SharedReceiveFeature>();
            capture.ReceiveFeatureWasShared.Add(
                ReferenceEquals(receiveFeature, capture.ReceiveFeature)
                && receiveFeature.Value == capture.Attempts);
            receiveFeature!.Value++;
            capture.DbContextIds.Add(dbContext.ContextId.InstanceId);
            capture.ProbeIds.Add(probe.Id);
            capture.AccessorWasValid.Add(
                accessor.Context?.Services == context.Services
                    && ReferenceEquals(
                        context.Services.GetRequiredService<ResilienceDbContext>(),
                        dbContext));
            capture.ImmediateRetryCounts.Add(context.Features.Get<RetryFeature>()!.ImmediateRetryCount);
            capture.ConsumerWasPresent.Add(
                context.Features.Get<ReceiveConsumerFeature>()?.CurrentConsumer is not null);
            capture.FeatureWasPresent.Add(
                context.Features.Get<ResilienceAttemptFeature>() is not null);
            context.Features.Set(new ResilienceAttemptFeature());

            if (Interlocked.Increment(ref capture.Attempts) <= capture.FailuresBeforeSuccess)
            {
                throw new RetryExecutionException();
            }

            capture.Completed.TrySetResult();
            return default;
        }
    }

    public sealed class ResilienceProbe(ResilienceCapture capture) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose()
        {
            capture.DisposedProbeIds.Add(Id);

            if (capture.DisposedProbeIds.Count == capture.FailuresBeforeSuccess + 1)
            {
                capture.Disposed.TrySetResult();
            }
        }
    }

    public sealed class BatchResilienceHandler(
        ResilienceDbContext dbContext,
        ResilienceProbe probe,
        ConsumeContextAccessor accessor,
        ResilienceCapture capture)
        : IBatchEventHandler<ResilienceMessage>
    {
        public ValueTask HandleAsync(
            IMessageBatch<ResilienceMessage> batch,
            CancellationToken cancellationToken)
        {
            var context = accessor.Context!;
            capture.DbContextIds.Add(dbContext.ContextId.InstanceId);
            capture.ProbeIds.Add(probe.Id);
            capture.AccessorWasValid.Add(
                context is IBatchConsumeContext<ResilienceMessage>
                    && ReferenceEquals(
                        context.Services.GetRequiredService<ResilienceDbContext>(),
                        dbContext));
            capture.ImmediateRetryCounts.Add(context.Features.Get<RetryFeature>()!.ImmediateRetryCount);
            capture.ConsumerWasPresent.Add(
                context.Features.Get<ReceiveConsumerFeature>()?.CurrentConsumer is not null);
            capture.FeatureWasPresent.Add(
                context.Features.Get<ResilienceAttemptFeature>() is not null);
            context.Features.Set(new ResilienceAttemptFeature());

            if (Interlocked.Increment(ref capture.Attempts) <= capture.FailuresBeforeSuccess)
            {
                throw new RetryExecutionException();
            }

            capture.Completed.TrySetResult();
            return default;
        }
    }

    public sealed class ResilienceCapture
    {
        public SharedReceiveFeature ReceiveFeature { get; } = new();

        public List<bool> ReceiveFeatureWasShared { get; } = [];

        public List<Guid> DbContextIds { get; } = [];

        public List<Guid> ProbeIds { get; } = [];

        public List<Guid> DisposedProbeIds { get; } = [];

        public List<bool> AccessorWasValid { get; } = [];

        public List<bool> FeatureWasPresent { get; } = [];

        public List<int> ImmediateRetryCounts { get; } = [];

        public List<bool> ConsumerWasPresent { get; } = [];

        public TaskCompletionSource Completed { get; } = new();

        public TaskCompletionSource Disposed { get; } = new();

        public int Attempts;

        public int FailuresBeforeSuccess { get; set; } = 1;
    }

    public sealed class MiddlewareOrderHandler(MiddlewareOrderCapture capture)
        : IEventHandler<MiddlewareOrderMessage>
    {
        public ValueTask HandleAsync(
            MiddlewareOrderMessage message,
            CancellationToken cancellationToken)
        {
            capture.Entries.Add("Handler");
            capture.Completed.TrySetResult();
            return default;
        }
    }

    public sealed class MiddlewareOrderCapture
    {
        public List<string> Entries { get; } = [];

        public TaskCompletionSource Completed { get; } = new();
    }

    public sealed class ResilienceMessage
    {
        public required string Value { get; set; }
    }

    public sealed class MiddlewareOrderMessage;

    private sealed class ResilienceAttemptFeature;

    public sealed class SharedReceiveFeature
    {
        public int Value { get; set; }
    }

    private sealed class RetryExecutionException : Exception;
}
