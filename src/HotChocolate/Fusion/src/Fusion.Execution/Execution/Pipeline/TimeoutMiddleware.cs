using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using static System.Threading.CancellationTokenSource;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class TimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FusionSchemaDefinition _schema;
    private readonly TimeSpan _timeout;

    private TimeoutMiddleware(
        RequestDelegate next,
        FusionSchemaDefinition schema,
        FusionRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _schema = schema;
        _timeout = options.ExecutionTimeout;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // if the debugger is attached we will skip the current middleware.
        if (Debugger.IsAttached)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        using var timeout = new CancellationTokenSource(_timeout);

        // We do not dispose the combined token in this middleware at all times.
        // The `Dispose` is handled in the finally block.
        var combined = CreateLinkedTokenSource(context.RequestAborted, timeout.Token, _schema.GetCancellationToken());

        try
        {
            // Replace the request abort cancellation token with the newly created combined
            // token. That now tracks our request timeout as well as the outer request
            // cancellation token.
            context.RequestAborted = combined.Token;

            await _next(context).ConfigureAwait(false);

            if (timeout.IsCancellationRequested)
            {
                context.Result = ErrorHelper.RequestTimeout(_timeout);
            }
        }
        catch (OperationCanceledException)
        {
            // if its not the timeout that canceled we will let somebody else handle this.
            if (!timeout.IsCancellationRequested)
            {
                throw;
            }

            context.Result = ErrorHelper.RequestTimeout(_timeout);
        }
        finally
        {
            // If we return a stream we want to keep the combined token alive until
            // the stream is completed. By doing so we allow the initial cancellation token
            // to still signal to resolvers that the request was canceled.
            //
            // In the case of a stream it is still ok that we disposed the timeout since the
            // timeout is meant for the request processing itself which is finished at this
            // stage.
            if (context.Result is IResponseStream stream)
            {
                stream.RegisterForCleanup(combined);
            }
            else
            {
                // if the result is not a stream than we can safely dispose.
                combined.Dispose();
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var schema = (FusionSchemaDefinition)fc.Schema;
                var options = fc.SchemaServices.GetRequiredService<FusionRequestOptions>();
                var middleware = new TimeoutMiddleware(next, schema, options);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.TimeoutMiddleware);
}
