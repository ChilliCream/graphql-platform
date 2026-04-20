using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ConcurrencyGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExecutionConcurrencyGate _gate;

    private ConcurrencyGateMiddleware(
        RequestDelegate next,
        ExecutionConcurrencyGate gate)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(gate);

        _next = next;
        _gate = gate;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Delegates to the schema-wide execution concurrency gate; see
        // ExecutionConcurrencyGate for ordering and timeout semantics.
        await _gate.WaitAsync(context.RequestAborted).ConfigureAwait(false);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (factoryContext, next) =>
            {
                var gate = factoryContext.SchemaServices.GetService<ExecutionConcurrencyGate>();

                if (gate is null or { IsEnabled: false })
                {
                    // No gate is configured — skip the middleware entirely so there is
                    // no per-request overhead on the hot path.
                    return context => next(context);
                }

                var middleware = new ConcurrencyGateMiddleware(next, gate);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.ConcurrencyGateMiddleware);
}
