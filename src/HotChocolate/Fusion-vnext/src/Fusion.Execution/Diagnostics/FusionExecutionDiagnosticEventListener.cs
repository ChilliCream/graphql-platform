using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// This class can be used as a base class for <see cref="IFusionExecutionDiagnosticEventListener"/>
/// implementations, so that they only have to override the methods they
/// are interested in instead of having to provide implementations for all of them.
/// </summary>
public class FusionExecutionDiagnosticEventListener : IFusionExecutionDiagnosticEventListener
{
    /// <summary>
    /// Initializes a new instance of <see cref="FusionExecutionDiagnosticEventListener"/>.
    /// </summary>
    protected FusionExecutionDiagnosticEventListener() { }

    /// <summary>
    /// A no-op activity scope that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    protected internal static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    /// <inheritdoc />
    public virtual IDisposable ExecuteRequest(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ParseDocument(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ValidateDocument(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable CoerceVariables(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteOperation(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteSubscription(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable OnSubscriptionEvent(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual void ExecutionError(RequestContext context, ErrorKind kind, IReadOnlyList<IError> errors, object? state = null) { }

    /// <inheritdoc />
    public virtual void AddedDocumentToCache(RequestContext context) { }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromCache(RequestContext context) { }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromStorage(RequestContext context) { }

    /// <inheritdoc />
    public virtual void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId) { }

    /// <inheritdoc />
    public virtual void ExecutorCreated(string name, IRequestExecutor executor) { }

    /// <inheritdoc />
    public virtual void ExecutorEvicted(string name, IRequestExecutor executor) { }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose() { }
    }
}
