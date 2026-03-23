using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mocha.Mediator;

namespace MediatorShowcase;

// ──────────────────────────────────────────────────
// Logging Middleware (cross-cutting, all messages)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that logs and times every message passing through the pipeline.
/// Applies to all commands, queries, and notifications automatically.
/// </summary>
public static class LoggingMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.Logging");

                return ctx =>
                {
                    var messageTypeName = ctx.MessageType.Name;
                    logger.LogInformation("[Pipeline] Handling {MessageType}...", messageTypeName);

                    var sw = Stopwatch.StartNew();
                    var task = next(ctx);

                    if (task.IsCompletedSuccessfully)
                    {
                        sw.Stop();
                        logger.LogInformation(
                            "[Pipeline] Handled {MessageType} in {ElapsedMs}ms",
                            messageTypeName, sw.ElapsedMilliseconds);
                        return default;
                    }

                    return Awaited(task, sw, logger, messageTypeName);

                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static async ValueTask Awaited(
                        ValueTask t, Stopwatch sw, ILogger log, string msgType)
                    {
                        await t.ConfigureAwait(false);
                        sw.Stop();
                        log.LogInformation(
                            "[Pipeline] Handled {MessageType} in {ElapsedMs}ms",
                            msgType, sw.ElapsedMilliseconds);
                    }
                };
            },
            "Logging");
}

// ──────────────────────────────────────────────────
// Validation Middleware (message-specific pre-check)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that validates PlaceOrderCommand before the handler runs.
/// Demonstrates message-type-specific pre-processing.
/// </summary>
public static class PlaceOrderValidationMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.Validation");

                return ctx =>
                {
                    if (ctx.Message is PlaceOrderCommand order)
                    {
                        logger.LogInformation(
                            "[PreProcessor] Validating order: {Quantity}x {Product}",
                            order.Quantity, order.ProductName);

                        if (order.Quantity <= 0)
                        {
                            throw new ArgumentException("Quantity must be greater than zero.");
                        }
                    }

                    return next(ctx);
                };
            },
            "Validation");
}

// ──────────────────────────────────────────────────
// Auditing Middleware (post-processing)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that audits PlaceOrderCommand results after the handler runs.
/// Demonstrates message-type-specific post-processing.
/// </summary>
public static class PlaceOrderAuditMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.Audit");

                return ctx =>
                {
                    var task = next(ctx);

                    if (task.IsCompletedSuccessfully)
                    {
                        LogResult(ctx, logger);
                        return default;
                    }

                    return Awaited(task, ctx, logger);

                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static async ValueTask Awaited(
                        ValueTask t, IMediatorContext ctx, ILogger log)
                    {
                        await t.ConfigureAwait(false);
                        LogResult(ctx, log);
                    }

                    static void LogResult(IMediatorContext ctx, ILogger log)
                    {
                        if (ctx.Result is OrderResult result)
                        {
                            log.LogInformation(
                                "[PostProcessor] Order {OrderId} confirmed with total {Total:C}",
                                result.OrderId, result.Total);
                        }
                    }
                };
            },
            "Audit");
}

// ──────────────────────────────────────────────────
// Exception Handling Middleware
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that catches InvalidOperationException from RiskyCommand
/// and returns a fallback response instead of propagating the exception.
/// </summary>
public static class ExceptionHandlingMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.ExceptionHandler");

                return ctx =>
                {
                    try
                    {
                        var task = next(ctx);

                        if (task.IsCompletedSuccessfully)
                        {
                            return default;
                        }

                        return Awaited(task, ctx, logger);
                    }
                    catch (InvalidOperationException ex) when (ctx.Message is RiskyCommand)
                    {
                        HandleException(ctx, ex, logger);
                        return default;
                    }

                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static async ValueTask Awaited(
                        ValueTask t, IMediatorContext ctx, ILogger log)
                    {
                        try
                        {
                            await t.ConfigureAwait(false);
                        }
                        catch (InvalidOperationException ex) when (ctx.Message is RiskyCommand)
                        {
                            HandleException(ctx, ex, log);
                        }
                    }

                    static void HandleException(
                        IMediatorContext ctx, InvalidOperationException ex, ILogger log)
                    {
                        log.LogWarning(
                            "[ExceptionHandler] Caught {ExceptionType}: {Message}",
                            ex.GetType().Name, ex.Message);

                        ctx.Result = "Recovered gracefully from error.";
                    }
                };
            },
            "ExceptionHandler");
}
