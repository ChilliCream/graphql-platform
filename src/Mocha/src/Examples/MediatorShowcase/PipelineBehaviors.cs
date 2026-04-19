using System.Diagnostics;
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

                return async ctx =>
                {
                    var messageTypeName = ctx.MessageType.Name;
                    logger.LogInformation("[Pipeline] Handling {MessageType}...", messageTypeName);

                    var sw = Stopwatch.StartNew();
                    await next(ctx);
                    sw.Stop();

                    logger.LogInformation(
                        "[Pipeline] Handled {MessageType} in {ElapsedMs}ms",
                        messageTypeName, sw.ElapsedMilliseconds);
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

                return async ctx =>
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

                    await next(ctx);
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

                return async ctx =>
                {
                    await next(ctx);

                    if (ctx.Result is OrderResult result)
                    {
                        logger.LogInformation(
                            "[PostProcessor] Order {OrderId} confirmed with total {Total:C}",
                            result.OrderId, result.Total);
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

                return async ctx =>
                {
                    try
                    {
                        await next(ctx);
                    }
                    catch (InvalidOperationException ex) when (ctx.Message is RiskyCommand)
                    {
                        logger.LogWarning(
                            "[ExceptionHandler] Caught {ExceptionType}: {Message}",
                            ex.GetType().Name, ex.Message);

                        ctx.Result = "Recovered gracefully from error.";
                    }
                };
            },
            "ExceptionHandler");
}
