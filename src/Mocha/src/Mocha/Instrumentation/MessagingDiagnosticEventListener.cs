using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A base class for diagnostic event listeners with default no-op implementations.
/// Extend this class and override the methods you need.
/// </summary>
/// <seealso cref="IMessagingDiagnosticEventListener"/>
public class MessagingDiagnosticEventListener : IMessagingDiagnosticEventListener
{
    protected MessagingDiagnosticEventListener() { }

    /// <summary>
    /// Gets a shared no-op <see cref="IDisposable"/> scope.
    /// Calling <see cref="IDisposable.Dispose"/> on this instance is safe and performs no operation.
    /// Use this as a default return value from diagnostic methods when no diagnostic activity is needed.
    /// </summary>
    protected internal static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    public virtual IDisposable Dispatch(IDispatchContext context) => EmptyScope;

    public virtual void DispatchError(IDispatchContext context, Exception exception) { }

    public virtual IDisposable Receive(IReceiveContext context) => EmptyScope;

    public virtual void ReceiveError(IReceiveContext context, Exception exception) { }

    public virtual IDisposable Consume(IConsumeContext context) => EmptyScope;

    public virtual void ConsumeError(IConsumeContext context, Exception exception) { }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose() { }
    }
}
