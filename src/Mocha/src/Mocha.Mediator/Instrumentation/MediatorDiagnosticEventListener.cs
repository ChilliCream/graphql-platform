namespace Mocha.Mediator;

/// <summary>
/// A base class for diagnostic event listeners with default no-op implementations.
/// Extend this class and override the methods you need.
/// </summary>
/// <seealso cref="IMediatorDiagnosticEventListener"/>
public class MediatorDiagnosticEventListener : IMediatorDiagnosticEventListener
{
    protected MediatorDiagnosticEventListener() { }

    /// <summary>
    /// Gets a shared no-op <see cref="IDisposable"/> scope.
    /// Calling <see cref="IDisposable.Dispose"/> on this instance is safe and performs no operation.
    /// Use this as a default return value from <see cref="Execute"/> when no diagnostic activity is needed.
    /// </summary>
    protected internal static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    public virtual IDisposable Execute(Type messageType, Type responseType, object message) => EmptyScope;

    public virtual void ExecutionError(Type messageType, Type responseType, object message, Exception exception) { }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose() { }
    }
}
