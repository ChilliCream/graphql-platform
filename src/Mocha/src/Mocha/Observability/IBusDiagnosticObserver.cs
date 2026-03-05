using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Observes diagnostic events across the dispatch, receive, and consume stages of the messaging pipeline.
/// </summary>
/// <remarks>
/// Implementations can collect telemetry, traces, or metrics at each pipeline stage. The <c>Dispatch</c>,
/// <c>Receive</c>, and <c>Consume</c> methods return an <see cref="IDisposable"/> whose disposal marks the
/// end of the observed scope, enabling duration measurement and resource cleanup.
/// </remarks>
public interface IBusDiagnosticObserver
{
    /// <summary>
    /// Begins observing a dispatch (outbound send/publish) operation.
    /// </summary>
    /// <param name="context">The dispatch context for the outgoing message.</param>
    /// <returns>A disposable scope that ends observation when disposed.</returns>
    IDisposable Dispatch(IDispatchContext context);

    /// <summary>
    /// Begins observing a receive (inbound message arrival) operation.
    /// </summary>
    /// <param name="context">The receive context for the incoming message.</param>
    /// <returns>A disposable scope that ends observation when disposed.</returns>
    IDisposable Receive(IReceiveContext context);

    /// <summary>
    /// Begins observing a consume (consumer processing) operation.
    /// </summary>
    /// <param name="context">The consume context for the message being processed by a consumer.</param>
    /// <returns>A disposable scope that ends observation when disposed.</returns>
    IDisposable Consume(IConsumeContext context);

    /// <summary>
    /// Called when an error occurs during the receive pipeline.
    /// </summary>
    /// <param name="context">The receive context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    void OnReceiveError(IReceiveContext context, Exception exception);

    /// <summary>
    /// Called when an error occurs during the dispatch pipeline.
    /// </summary>
    /// <param name="context">The dispatch context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    void OnDispatchError(IDispatchContext context, Exception exception);

    /// <summary>
    /// Called when an error occurs during the consume pipeline.
    /// </summary>
    /// <param name="context">The consume context in which the error occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    void OnConsumeError(IConsumeContext context, Exception exception);
}
