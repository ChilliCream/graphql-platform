using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Mediator middleware that delegates to <see cref="IMediatorDiagnosticEvents"/>
/// to instrument message handling with diagnostic events.
/// </summary>
internal sealed class MediatorDiagnosticMiddleware(IMediatorDiagnosticEvents events)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        using var scope = events.Execute(context.MessageType, context.ResponseType, context.Message);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            events.ExecutionError(context.MessageType, context.ResponseType, context.Message, ex);
            throw;
        }
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var diagnosticEvents = context.Services.GetRequiredService<IMediatorDiagnosticEvents>();
                var middleware = new MediatorDiagnosticMiddleware(diagnosticEvents);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Instrumentation");
}
