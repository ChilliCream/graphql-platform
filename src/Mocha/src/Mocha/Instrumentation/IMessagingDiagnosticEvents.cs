using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides diagnostic events that can be triggered by the messaging pipeline.
/// These events allow monitoring and instrumentation of dispatch, receive, and consume operations.
/// </summary>
/// <seealso cref="IMessagingDiagnosticEventListener"/>
public interface IMessagingDiagnosticEvents
{
    /// <summary>
    /// Called when a message begins dispatching. Returns a disposable scope.
    /// </summary>
    IDisposable Dispatch(IDispatchContext context);

    /// <summary>
    /// Called when an exception occurs during dispatch.
    /// </summary>
    void DispatchError(IDispatchContext context, Exception exception);

    /// <summary>
    /// Called when a message begins being received. Returns a disposable scope.
    /// </summary>
    IDisposable Receive(IReceiveContext context);

    /// <summary>
    /// Called when an exception occurs during receive.
    /// </summary>
    void ReceiveError(IReceiveContext context, Exception exception);

    /// <summary>
    /// Called when a message begins being consumed. Returns a disposable scope.
    /// </summary>
    IDisposable Consume(IConsumeContext context);

    /// <summary>
    /// Called when an exception occurs during consume.
    /// </summary>
    void ConsumeError(IConsumeContext context, Exception exception);
}
