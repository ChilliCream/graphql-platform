using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// This class can be used as a base class for <see cref="IExecutionDiagnosticEventListener"/>
/// implementations, so that they only have to override the methods they
/// are interested in instead of having to provide implementations for all of them.
/// </summary>
public class ExecutionDiagnosticEventListener : IExecutionDiagnosticEventListener
{
    protected ExecutionDiagnosticEventListener()
    {
    }

    /// <inheritdoc />
    public virtual bool EnableResolveFieldValue => false;

    /// <summary>
    /// A no-op activity scope that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    protected internal static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    /// <inheritdoc />
    public virtual IDisposable ExecuteRequest(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void RequestError(RequestContext context, Exception error)
    {
    }

    /// <inheritdoc />
    public virtual void RequestError(RequestContext context, IError error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ParseDocument(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ValidateDocument(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
    }

    /// <inheritdoc />
    public virtual void SubscriptionEventError(RequestContext context, ulong subscriptionId, Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable AnalyzeOperationCost(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void OperationCost(RequestContext context, double fieldCost, double typeCost)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable CoerceVariables(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable CompileOperation(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteOperation(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteStream(IOperation operation)
        => EmptyScope;

    public virtual IDisposable ExecuteDeferredTask()
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ResolveFieldValue(IMiddlewareContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ResolverError(IMiddlewareContext context, IError error)
    {
    }

    /// <inheritdoc />
    public virtual void ResolverError(RequestContext context, ISelection selection, IError error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable RunTask(IExecutionTask task)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void TaskError(IExecutionTask task, IError error)
    {
    }

    /// <inheritdoc />
    public virtual void StartProcessing(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void StopProcessing(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ExecuteSubscription(
        RequestContext context,
        ulong subscriptionId)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable OnSubscriptionEvent(
        RequestContext context,
        ulong subscriptionId)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void AddedDocumentToCache(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromCache(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromStorage(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void DocumentNotFoundInStorage(
        RequestContext context,
        OperationDocumentId documentId)
    {
    }

    /// <inheritdoc />
    public virtual void UntrustedDocumentRejected(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void AddedOperationToCache(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedOperationFromCache(RequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable DispatchBatch(RequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ExecutorCreated(string name, IRequestExecutor executor)
    {
    }

    /// <inheritdoc />
    public virtual void ExecutorEvicted(string name, IRequestExecutor executor)
    {
    }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
