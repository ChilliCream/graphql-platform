using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Default no-op implementation of <see cref="IBusDiagnosticObserver"/> used as a fallback
/// when no telemetry or diagnostic observer has been configured for the message bus.
/// </summary>
/// <remarks>
/// All observation methods return a lightweight no-op disposable, and all error handlers
/// are empty. This avoids null checks in the pipeline while incurring minimal overhead.
/// Access the shared instance via <see cref="Instance"/>.
/// </remarks>
/// <seealso cref="IBusDiagnosticObserver"/>
/// <seealso cref="OpenTelemetryDiagnosticObserver"/>
internal sealed class NoOpBusDiagnosticObserver : IBusDiagnosticObserver
{
    /// <summary>
    /// Returns a no-op disposable scope; no dispatch telemetry is recorded.
    /// </summary>
    /// <param name="context">The dispatch context for the outgoing message.</param>
    /// <returns>A no-op <see cref="IDisposable"/> that performs no action on disposal.</returns>
    public IDisposable Dispatch(IDispatchContext context)
    {
        return NoOpDisposable.Instance;
    }

    /// <summary>
    /// Returns a no-op disposable scope; no receive telemetry is recorded.
    /// </summary>
    /// <param name="context">The receive context for the incoming message.</param>
    /// <returns>A no-op <see cref="IDisposable"/> that performs no action on disposal.</returns>
    public IDisposable Receive(IReceiveContext context)
    {
        return NoOpDisposable.Instance;
    }

    /// <summary>
    /// Returns a no-op disposable scope; no consume telemetry is recorded.
    /// </summary>
    /// <param name="context">The consume context for the message being processed.</param>
    /// <returns>A no-op <see cref="IDisposable"/> that performs no action on disposal.</returns>
    public IDisposable Consume(IConsumeContext context)
    {
        return NoOpDisposable.Instance;
    }

    /// <summary>
    /// Called when an error occurs during the receive pipeline; intentionally does nothing.
    /// </summary>
    /// <param name="context">The receive context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    public void OnReceiveError(IReceiveContext context, Exception exception) { }

    /// <summary>
    /// Called when an error occurs during the dispatch pipeline; intentionally does nothing.
    /// </summary>
    /// <param name="context">The dispatch context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    public void OnDispatchError(IDispatchContext context, Exception exception) { }

    /// <summary>
    /// Called when an error occurs during the consume pipeline; intentionally does nothing.
    /// </summary>
    /// <param name="context">The consume context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    public void OnConsumeError(IConsumeContext context, Exception exception) { }

    /// <summary>
    /// Lightweight disposable that performs no action on disposal, used by the no-op observer.
    /// </summary>
    private sealed class NoOpDisposable : IDisposable
    {
        /// <summary>
        /// Performs no action. Exists solely to satisfy the <see cref="IDisposable"/> contract.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Gets a new instance of the no-op disposable.
        /// </summary>
        public static NoOpDisposable Instance => new();
    }

    /// <summary>
    /// Gets the shared singleton instance of the no-op diagnostic observer.
    /// </summary>
    public static NoOpBusDiagnosticObserver Instance => field ??= new();
}
