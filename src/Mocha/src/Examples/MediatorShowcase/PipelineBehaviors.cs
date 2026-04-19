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
internal sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        var messageTypeName = context.MessageType.Name;
        logger.LogInformation("[Pipeline] Handling {MessageType}...", messageTypeName);

        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        logger.LogInformation(
            "[Pipeline] Handled {MessageType} in {ElapsedMs}ms",
            messageTypeName, sw.ElapsedMilliseconds);
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILogger<LoggingMiddleware>>();
                var middleware = new LoggingMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Logging");
}

// ──────────────────────────────────────────────────
// Command Audit Middleware (compile-time scoped to commands)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that audits every write operation. Demonstrates compile-time filtering by
/// message kind: queries and notifications are skipped at startup with
/// <see cref="MediatorMiddlewareFactoryContextExtensions.IsQuery"/> and
/// <see cref="MediatorMiddlewareFactoryContextExtensions.IsNotification"/>, so this middleware
/// is only compiled into command pipelines.
/// </summary>
internal sealed class CommandAuditMiddleware(ILogger<CommandAuditMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        logger.LogInformation("[Audit] Executing command {CommandType}", context.MessageType.Name);
        await next(context);
        logger.LogInformation("[Audit] Completed command {CommandType}", context.MessageType.Name);
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Compile-time filter: skip queries and notifications - audit only commands
                if (factoryCtx.IsQuery() || factoryCtx.IsNotification())
                {
                    return next;
                }

                var logger = factoryCtx.Services.GetRequiredService<ILogger<CommandAuditMiddleware>>();
                var middleware = new CommandAuditMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "CommandAudit");
}

// ──────────────────────────────────────────────────
// Validation Middleware (compile-time scoped to PlaceOrderCommand)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that validates <see cref="PlaceOrderCommand"/> before the handler runs.
/// Demonstrates compile-time filtering: the factory inspects the message type and returns
/// <c>next</c> for unrelated pipelines, so this middleware is only compiled into the
/// PlaceOrderCommand pipeline - zero runtime cost everywhere else.
/// </summary>
internal sealed class PlaceOrderValidationMiddleware(ILogger<PlaceOrderValidationMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        // Safe to cast - compile-time filter guarantees the message type
        var order = (PlaceOrderCommand)context.Message;

        logger.LogInformation(
            "[PreProcessor] Validating order: {Quantity}x {Product}",
            order.Quantity, order.ProductName);

        if (order.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        await next(context);
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Compile-time filter: skip every pipeline whose message is not PlaceOrderCommand
                if (!factoryCtx.IsMessageAssignableTo<PlaceOrderCommand>())
                {
                    return next;
                }

                var logger = factoryCtx.Services.GetRequiredService<ILogger<PlaceOrderValidationMiddleware>>();
                var middleware = new PlaceOrderValidationMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Validation");
}

// ──────────────────────────────────────────────────
// Auditing Middleware (compile-time scoped to OrderResult responses)
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that audits any handler returning an <see cref="OrderResult"/> after it runs.
/// Demonstrates response-type-based compile-time filtering with
/// <see cref="MediatorMiddlewareFactoryContextExtensions.IsResponseAssignableTo{T}"/>.
/// </summary>
internal sealed class PlaceOrderAuditMiddleware(ILogger<PlaceOrderAuditMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        await next(context);

        if (context.Result is OrderResult result)
        {
            logger.LogInformation(
                "[PostProcessor] Order {OrderId} confirmed with total {Total:C}",
                result.OrderId, result.Total);
        }
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Compile-time filter: only commands/queries that return OrderResult
                if (!factoryCtx.IsResponseAssignableTo<OrderResult>())
                {
                    return next;
                }

                var logger = factoryCtx.Services.GetRequiredService<ILogger<PlaceOrderAuditMiddleware>>();
                var middleware = new PlaceOrderAuditMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Audit");
}

// ──────────────────────────────────────────────────
// Exception Handling Middleware
// ──────────────────────────────────────────────────

/// <summary>
/// Middleware that catches <see cref="InvalidOperationException"/> from <see cref="RiskyCommand"/>
/// and returns a fallback response instead of propagating the exception.
/// </summary>
internal sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex) when (context.Message is RiskyCommand)
        {
            logger.LogWarning(
                "[ExceptionHandler] Caught {ExceptionType}: {Message}",
                ex.GetType().Name, ex.Message);

            context.Result = "Recovered gracefully from error.";
        }
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILogger<ExceptionHandlingMiddleware>>();
                var middleware = new ExceptionHandlingMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "ExceptionHandler");
}
