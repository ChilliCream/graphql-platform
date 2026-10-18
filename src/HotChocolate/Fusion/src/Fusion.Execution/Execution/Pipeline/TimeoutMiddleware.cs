using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class TimeoutMiddleware
{
    private const int MaximumRetainedTimeoutSources = 256;

    private readonly RequestDelegate _next;
    private readonly ObjectPool<RequestTimeoutSource> _timeoutSources;
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
        _timeout = options.ExecutionTimeout;

        var poolProvider = new DefaultObjectPoolProvider { MaximumRetained = MaximumRetainedTimeoutSources };
        _timeoutSources = poolProvider.Create(new RequestTimeoutSource.Policy(schema.GetCancellationToken()));
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // if the debugger is attached we will skip the current middleware.
        if (Debugger.IsAttached)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var requestAborted = context.RequestAborted;
        var timeout = _timeoutSources.Get();
        timeout.Start(requestAborted, _timeout);

        try
        {
            // Replace the request abort cancellation token with the token of the timeout source.
            // That now tracks our request timeout as well as the outer request
            // cancellation token and the schema shutdown.
            context.RequestAborted = timeout.Token;

            await _next(context).ConfigureAwait(false);

            if (timeout.IsTimedOut)
            {
                context.Result = ErrorHelper.RequestTimeout(_timeout);
            }
        }
        catch (OperationCanceledException)
        {
            // if its not the timeout that canceled we will let somebody else handle this.
            if (!timeout.IsTimedOut)
            {
                throw;
            }

            context.Result = ErrorHelper.RequestTimeout(_timeout);
        }
        finally
        {
            // The timeout is meant for the request processing itself which is finished at this
            // stage, so it is stopped in any case.
            timeout.StopTimeout();

            // If we return a stream we want to keep the token alive until the stream is completed.
            // By doing so we allow the outer cancellation token to still signal to resolvers
            // that the request was canceled.
            if (context.Result is IResponseStream stream)
            {
                stream.RegisterForCleanup(() => _timeoutSources.Return(timeout));
            }
            else
            {
                context.RequestAborted = requestAborted;
                _timeoutSources.Return(timeout);
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
