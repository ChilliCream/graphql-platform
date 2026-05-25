namespace Mocha.Mediator;

/// <summary>
/// Provides diagnostic events that can be triggered by the mediator pipeline.
/// These events allow monitoring and instrumentation of message handling.
/// </summary>
/// <seealso cref="IMediatorDiagnosticEventListener"/>
public interface IMediatorDiagnosticEvents
{
    /// <summary>
    /// Called when a message begins executing. Returns a disposable scope.
    /// </summary>
    IDisposable Execute(Type messageType, Type responseType, object message);

    /// <summary>
    /// Called when an exception occurs during execution.
    /// </summary>
    void ExecutionError(Type messageType, Type responseType, object message, Exception exception);
}
