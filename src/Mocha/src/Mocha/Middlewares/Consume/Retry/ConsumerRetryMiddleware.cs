using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry with configurable backoff strategies
/// when transient failures occur. Each execution uses a fresh service scope and context,
/// while retry state is shared across the operation.
/// </summary>
internal sealed class ConsumerRetryMiddleware(
    ImmutableArray<ExceptionPolicyRule> exceptionPolicyRules,
    Consumer consumer,
    IConsumerExecutionStrategy executionStrategy)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        // Read delayed retry count from headers (set by redelivery middleware).
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue))
        {
            delayedRetryCount = RedeliveryExecutor.ParseDelayedRetryCount(headerValue);
        }

        // Expose retry state to handlers via features.
        var retryState = context.Features.GetOrSet<RetryFeature>();
        retryState.DelayedRetryCount = delayedRetryCount;
        retryState.ImmediateRetryCount = 0;

        await RetryExecutor.ExecuteAsync(
            exceptionPolicyRules,
            (next, context, retryState, consumer, executionStrategy),
            static (s) => s.executionStrategy.ExecuteAsync(
                s.context,
                ct => ExecuteAttemptAsync(s.next, s.context, s.retryState, s.consumer, ct)),
            onRetry: null,
            context.CancellationToken);
    }

    /// <summary>
    /// Executes one consumer attempt on a clone of the context in a new service scope.
    /// </summary>
    private static async ValueTask ExecuteAttemptAsync(
        ConsumerDelegate next,
        IConsumeContext context,
        RetryFeature retryState,
        Consumer consumer,
        CancellationToken cancellationToken)
    {
        await using var scope = context.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
        var pool = scope.ServiceProvider.GetRequiredService<IMessagingPools>().ReceiveContext;
        ReceiveContext? pooledContext = null;
        var attempt = context is ReceiveContext receiveContext
            ? pooledContext = receiveContext.CopyTo(pool.Get(), scope.ServiceProvider)
            : context.Clone(scope.ServiceProvider);

        try
        {
            // The receive pipeline sets CurrentConsumer on the receive context's consumer feature,
            // which the attempt reads through its clone. A batch context has no consumer feature,
            // so the attempt gets one of its own. Set() initializes pooled features, which clears
            // CurrentConsumer, so it is assigned after the feature is added.
            if (attempt.Features.Get<ReceiveConsumerFeature>() is null)
            {
                var consumerFeature = new ReceiveConsumerFeature();
                attempt.Features.Set(consumerFeature);
                consumerFeature.CurrentConsumer = consumer;
            }

            attempt.CancellationToken = cancellationToken;
            accessor.Context = attempt;
            await next(attempt);
        }
        catch
        {
            // Counts every failed attempt, including the ones the execution strategy repeats.
            retryState.ImmediateRetryCount++;
            throw;
        }
        finally
        {
            accessor.Context = null;

            if (pooledContext is not null)
            {
                pool.Return(pooledContext);
            }
        }
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var feature = context.GetExceptionPolicyFeature();
                var middleware = new ConsumerRetryMiddleware(
                    feature?.Rules.ToImmutableArray() ?? ImmutableArray<ExceptionPolicyRule>.Empty,
                    context.Consumer,
                    context.GetConsumerExecutionStrategy());

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Retry");
}

file static class Extensions
{
    /// <summary>
    /// Resolves the bus-level exception policy feature, if configured.
    /// </summary>
    public static ExceptionPolicyFeature? GetExceptionPolicyFeature(this ConsumerMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        if (busFeatures.TryGet(out ExceptionPolicyFeature? busFeature))
        {
            return busFeature;
        }

        return null;
    }

    /// <summary>
    /// Resolves the bus-level consumer execution strategy, falling back to direct execution.
    /// </summary>
    public static IConsumerExecutionStrategy GetConsumerExecutionStrategy(this ConsumerMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        return busFeatures.Get<ConsumerExecutionStrategyFeature>()?.Strategy
            ?? DirectConsumerExecutionStrategy.Instance;
    }
}
