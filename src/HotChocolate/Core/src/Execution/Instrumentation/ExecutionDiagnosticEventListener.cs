using HotChocolate.Execution.Processing;
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
    public virtual IDisposable ExecuteRequest(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void RequestError(IRequestContext context, Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ParseDocument(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void SyntaxError(IRequestContext context, IError error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ValidateDocument(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable AnalyzeOperationCost(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void OperationCost(IRequestContext context, double fieldCost, double typeCost)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable CoerceVariables(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable CompileOperation(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteOperation(IRequestContext context)
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
    public virtual void ResolverError(IRequestContext context, ISelection selection, IError error)
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
    public virtual void StartProcessing(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void StopProcessing(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ExecuteSubscription(
        ISubscription subscription)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable OnSubscriptionEvent(
        SubscriptionEventContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void SubscriptionEventResult(
        SubscriptionEventContext context,
        IOperationResult result)
    {
    }

    /// <inheritdoc />
    public virtual void SubscriptionEventError(
        SubscriptionEventContext context,
        Exception exception)
    {
    }

    /// <inheritdoc />
    public void SubscriptionEventError(
        ISubscription subscription,
        Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual void SubscriptionTransportError(
        ISubscription subscription,
        Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual void AddedDocumentToCache(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromCache(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromStorage(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void DocumentNotFoundInStorage(
        IRequestContext context,
        OperationDocumentId documentId)
    {
    }

    /// <inheritdoc />
    public virtual void AddedOperationToCache(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual void RetrievedOperationFromCache(IRequestContext context)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable DispatchBatch(IRequestContext context)
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
