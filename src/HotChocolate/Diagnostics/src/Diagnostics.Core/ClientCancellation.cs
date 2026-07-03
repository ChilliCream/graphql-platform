using HotChocolate.Execution;

namespace HotChocolate.Diagnostics;

/// <summary>
/// Helpers to detect whether a GraphQL operation ended because the caller
/// intentionally cancelled it (for example the browser tab was closed or the
/// connection was dropped) rather than because of a server-side failure.
/// </summary>
/// <remarks>
/// Per the OpenTelemetry semantic conventions an intentional caller cancellation
/// is not an error: the span status is left <c>Unset</c> and no
/// <c>error.type</c> is reported. This is deliberately distinct from a
/// server-side execution timeout, which remains an error.
/// </remarks>
internal static class ClientCancellation
{
    /// <summary>
    /// Determines whether the request ended because the caller cancelled it.
    /// </summary>
    /// <remarks>
    /// A client/caller cancellation surfaces as an <see cref="OperationResult"/>
    /// whose first error carries <see cref="ErrorCodes.Execution.Canceled"/>
    /// (<c>HC0049</c>). A server-side execution timeout instead carries
    /// <see cref="ErrorCodes.Execution.Timeout"/> (<c>HC0045</c>) and is therefore
    /// not treated as a client cancellation.
    /// </remarks>
    public static bool IsClientCanceled(RequestContext context)
        => context.Result is OperationResult result && IsClientCanceled(result);

    /// <summary>
    /// Determines whether the given result represents a caller cancellation.
    /// </summary>
    /// <remarks>
    /// A client/caller cancellation surfaces as an <see cref="OperationResult"/>
    /// whose first error carries <see cref="ErrorCodes.Execution.Canceled"/>
    /// (<c>HC0049</c>). A server-side execution timeout instead carries
    /// <see cref="ErrorCodes.Execution.Timeout"/> (<c>HC0045</c>) and is therefore
    /// not treated as a client cancellation.
    /// </remarks>
    public static bool IsClientCanceled(OperationResult result)
        => result is { Errors: [{ Code: ErrorCodes.Execution.Canceled }, ..] };
}
