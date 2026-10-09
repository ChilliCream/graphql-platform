using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// Helpers to detect whether a gateway operation ended because the caller
/// intentionally cancelled it (for example the browser tab was closed or the
/// connection was dropped) rather than because of a server-side failure.
/// </summary>
/// <remarks>
/// Per the OpenTelemetry semantic conventions an intentional caller cancellation
/// is not an error: the span status is left <c>Unset</c> and no
/// <c>error.type</c> is reported. This is deliberately distinct from a
/// server-side execution timeout, which remains an error.
/// </remarks>
internal static class FusionClientCancellation
{
    /// <summary>
    /// Determines whether the request ended because the caller cancelled it.
    /// </summary>
    /// <remarks>
    /// A caller cancellation is recognized either from the finished result
    /// (an <see cref="ErrorCodes.Execution.Canceled"/> result, shared with the
    /// non-federated detection in <see cref="ClientCancellation"/>) or, while
    /// the operation is still unwinding, from the request abort token. A
    /// server-side execution timeout also cancels <see cref="RequestContext.RequestAborted"/>,
    /// but it leaves the transport's own abort token
    /// (<see cref="HttpContext.RequestAborted"/>) untouched. When an HTTP
    /// transport is present its abort token is therefore the authoritative
    /// signal and is used to tell a dropped connection apart from a timeout;
    /// without an HTTP transport the request abort token is the only available
    /// signal.
    /// </remarks>
    public static bool IsClientCanceled(RequestContext context)
    {
        if (ClientCancellation.IsClientCanceled(context))
        {
            return true;
        }

        if (!context.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        return !context.Features.TryGet<HttpContext>(out var httpContext)
            || httpContext.RequestAborted.IsCancellationRequested;
    }
}
