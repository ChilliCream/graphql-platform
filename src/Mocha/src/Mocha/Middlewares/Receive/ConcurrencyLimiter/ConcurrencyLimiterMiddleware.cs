using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A receive middleware that limits the number of messages processed concurrently using a semaphore.
/// </summary>
/// <param name="maxConcurrency">The maximum number of messages that can be processed concurrently.</param>
public sealed class ConcurrencyLimiterMiddleware(int maxConcurrency) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(maxConcurrency, maxConcurrency);

    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        await _semaphore.WaitAsync(context.CancellationToken);

        try
        {
            await next(context);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                // Feature values are resolved from endpoint -> transport -> bus to support overrides.
                var enabled = context.GetConfiguration(f => f.Enabled) ?? true;

                if (!enabled)
                {
                    return next;
                }

                var maxConcurrency =
                    context.GetConfiguration(f => f.MaxConcurrency);

                if (maxConcurrency is null)
                {
                    return next;
                }

                var middleware = new ConcurrencyLimiterMiddleware(maxConcurrency.Value);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "ConcurrencyLimiter");
}

file static class Extensions
{
    /// <summary>
    /// Resolves configuration with the most specific scope taking precedence.
    /// </summary>
    public static T? GetConfiguration<T>(
        this ReceiveMiddlewareFactoryContext context,
        Func<ConcurrencyLimiterFeature, T> selector)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        return context.Endpoint.Features.GetFeatureValue(selector)
            ?? context.Transport.Features.GetFeatureValue(selector)
            ?? busFeatures.GetFeatureValue(selector);
    }

    private static T? GetFeatureValue<T>(this IFeatureCollection features, Func<ConcurrencyLimiterFeature, T?> selector)
    {
        if (features.TryGet(out ConcurrencyLimiterFeature? feature))
        {
            return selector(feature);
        }

        return default;
    }
}
