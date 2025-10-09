namespace HotChocolate.ModelContextProtocol.Diagnostics;

/// <summary>
/// This class can be used as a base class for <see cref="IMcpDiagnosticEventListener"/>
/// implementations, so that they only have to override the methods they
/// are interested in instead of having to provide implementations for all of them.
/// </summary>
public class McpDiagnosticEventListener : IMcpDiagnosticEventListener
{
    /// <summary>
    /// A no-op activity scope that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    private static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    public virtual IDisposable InitializeTools() => EmptyScope;

    public virtual IDisposable UpdateTools() => EmptyScope;

    public virtual void ValidationErrors(IReadOnlyList<IError> errors)
    {
    }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
