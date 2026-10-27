using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class TimeoutMiddleware
{
    private const int MaxRetainedTimeoutSources = 256;

    private static readonly Action<object?> s_cancel =
        static state => Unsafe.As<CancellationTokenSource>(state)!.Cancel();

    private readonly RequestDelegate _next;
    private readonly FusionSchemaDefinition _schema;
    private readonly TimeSpan _timeout;
    private readonly bool _setsDeadline;
    private readonly ObjectPool<CancellationTokenSource> _timeoutSources;

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
        _setsDeadline = _timeout <= ExecutionDeadline.MaxTimeout;
        _timeoutSources = new DefaultObjectPool<CancellationTokenSource>(
            new DefaultPooledObjectPolicy<CancellationTokenSource>(),
            MaxRetainedTimeoutSources);
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
        var schemaAborted = _schema.GetCancellationToken();

        // The pooled source is canceled by the request timeout, by the outer request
        // cancellation token, or by the schema shutdown token.
        var timeout = _timeoutSources.Get();
        CancellationTokenRegistration requestAbortedRegistration = default;
        CancellationTokenRegistration schemaAbortedRegistration = default;
        ExecutionDeadline? deadline = null;

        try
        {
            timeout.CancelAfter(_timeout);
            requestAbortedRegistration = requestAborted.UnsafeRegister(s_cancel, timeout);
            schemaAbortedRegistration = schemaAborted.UnsafeRegister(s_cancel, timeout);

            // Replace the request abort cancellation token with the pooled token. That now
            // tracks our request timeout as well as the outer request cancellation token.
            context.RequestAborted = timeout.Token;

            // The pooled token has the timeout as its deadline until this middleware returns.
            if (_setsDeadline)
            {
                deadline = context.Features.GetOrSet<ExecutionDeadline>();
                deadline.Set(timeout.Token);
            }

            await _next(context).ConfigureAwait(false);

            if (IsTimedOut(timeout, requestAborted, schemaAborted))
            {
                context.Result = ErrorHelper.RequestTimeout(_timeout);
            }
        }
        catch (OperationCanceledException)
        {
            // if its not the timeout that canceled we will let somebody else handle this.
            if (!IsTimedOut(timeout, requestAborted, schemaAborted))
            {
                throw;
            }

            context.Result = ErrorHelper.RequestTimeout(_timeout);
        }
        finally
        {
            deadline?.Clear();

            if (context.Result is IResponseStream stream)
            {
                // A stream keeps the source until it is disposed. The source stays linked to the
                // outer request and the schema but no longer times out.
                timeout.CancelAfter(Timeout.Infinite);
                stream.RegisterForCleanup(
                    new StreamCancellationLease(
                        timeout,
                        requestAbortedRegistration,
                        schemaAbortedRegistration));
            }
            else
            {
                // Restore the original token before the source can be reused by another request.
                context.RequestAborted = requestAborted;

                // Dispose the registrations before the reset so no late cancellation reaches a reused source.
                requestAbortedRegistration.Dispose();
                schemaAbortedRegistration.Dispose();

                if (timeout.TryReset())
                {
                    _timeoutSources.Return(timeout);
                }
                else
                {
                    timeout.Dispose();
                }
            }
        }
    }

    private static bool IsTimedOut(
        CancellationTokenSource timeout,
        CancellationToken requestAborted,
        CancellationToken schemaAborted)
        => timeout.IsCancellationRequested
            && !requestAborted.IsCancellationRequested
            && !schemaAborted.IsCancellationRequested;

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

    private sealed class StreamCancellationLease(
        CancellationTokenSource source,
        CancellationTokenRegistration requestAbortedRegistration,
        CancellationTokenRegistration schemaAbortedRegistration)
        : IDisposable
    {
        public void Dispose()
        {
            requestAbortedRegistration.Dispose();
            schemaAbortedRegistration.Dispose();
            source.Dispose();
        }
    }
}
