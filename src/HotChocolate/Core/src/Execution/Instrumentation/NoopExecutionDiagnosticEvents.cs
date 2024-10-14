using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

internal sealed class NoopExecutionDiagnosticEvents
    : IExecutionDiagnosticEvents
    , IDisposable
{
    public IDisposable ExecuteRequest(IRequestContext context) => this;

    public void RequestError(IRequestContext context, Exception exception)
    {
    }

    public IDisposable ParseDocument(IRequestContext context) => this;

    public void SyntaxError(IRequestContext context, IError error)
    {
    }

    public IDisposable ValidateDocument(IRequestContext context) => this;

    public void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
    }

    public IDisposable AnalyzeOperationCost(IRequestContext context) => this;

    public void OperationComplexityAnalyzerCompiled(IRequestContext context)
    {
    }

    public void OperationCost(IRequestContext context, double fieldCost, double typeCost)
    {
    }

    public IDisposable CoerceVariables(IRequestContext context) => this;

    public IDisposable CompileOperation(IRequestContext context) => this;

    public IDisposable ExecuteOperation(IRequestContext context) => this;

    public IDisposable ExecuteStream(IOperation operation) => this;

    public IDisposable ExecuteDeferredTask() => this;

    public IDisposable ResolveFieldValue(IMiddlewareContext context) => this;

    public void ResolverError(IMiddlewareContext context, IError error)
    {
    }

    public void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
    }

    public IDisposable RunTask(IExecutionTask task) => this;

    public void TaskError(IExecutionTask task, IError error)
    {
    }

    public void StartProcessing(IRequestContext context)
    {
    }

    public void StopProcessing(IRequestContext context)
    {
    }

    public IDisposable ExecuteSubscription(ISubscription subscription) => this;

    public IDisposable OnSubscriptionEvent(SubscriptionEventContext context) => this;

    public void SubscriptionEventResult(SubscriptionEventContext context, IOperationResult result)
    {
    }

    public void SubscriptionEventError(SubscriptionEventContext context, Exception exception)
    {
    }

    public void SubscriptionEventError(ISubscription subscription, Exception exception)
    {
    }

    public void SubscriptionTransportError(ISubscription subscription, Exception exception)
    {
    }

    public void AddedDocumentToCache(IRequestContext context)
    {
    }

    public void RetrievedDocumentFromCache(IRequestContext context)
    {
    }

    public void RetrievedDocumentFromStorage(IRequestContext context)
    {
    }

    public void DocumentNotFoundInStorage(IRequestContext context, OperationDocumentId documentId)
    {
    }

    public void AddedOperationToCache(IRequestContext context)
    {
    }

    public void RetrievedOperationFromCache(IRequestContext context)
    {
    }

    public IDisposable DispatchBatch(IRequestContext context) => this;

    public void ExecutorCreated(string name, IRequestExecutor executor)
    {
    }

    public void ExecutorEvicted(string name, IRequestExecutor executor)
    {
    }

    public void Dispose()
    {
    }
}
