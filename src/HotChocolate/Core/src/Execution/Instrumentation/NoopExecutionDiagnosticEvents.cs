using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

internal sealed class NoopExecutionDiagnosticEvents
    : IExecutionDiagnosticEvents
    , IDisposable
{
    public IDisposable ExecuteRequest(RequestContext context) => this;

    public void RequestError(RequestContext context, Exception exception)
    {
    }

    public IDisposable ParseDocument(RequestContext context) => this;

    public void SyntaxError(RequestContext context, IError error)
    {
    }

    public IDisposable ValidateDocument(RequestContext context) => this;

    public void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
    }

    public IDisposable AnalyzeOperationCost(RequestContext context) => this;

    public void OperationComplexityAnalyzerCompiled(RequestContext context)
    {
    }

    public void OperationCost(RequestContext context, double fieldCost, double typeCost)
    {
    }

    public IDisposable CoerceVariables(RequestContext context) => this;

    public IDisposable CompileOperation(RequestContext context) => this;

    public IDisposable ExecuteOperation(RequestContext context) => this;

    public IDisposable ExecuteStream(IOperation operation) => this;

    public IDisposable ExecuteDeferredTask() => this;

    public IDisposable ResolveFieldValue(IMiddlewareContext context) => this;

    public IDisposable RunTask(IExecutionTask task) => this;

    public void StartProcessing(RequestContext context)
    {
    }

    public void StopProcessing(RequestContext context)
    {
    }

    public IDisposable ExecuteSubscription(RequestContext context) => this;

    public IDisposable OnSubscriptionEvent(RequestContext context) => this;

    public void ExecutionError(
        RequestContext context,
        ErrorKind kind,
        IReadOnlyList<IError> errors,
        object? state)
    {
    }

    public void AddedDocumentToCache(RequestContext context)
    {
    }

    public void RetrievedDocumentFromCache(RequestContext context)
    {
    }

    public void RetrievedDocumentFromStorage(RequestContext context)
    {
    }

    public void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId)
    {
    }

    public void AddedOperationToCache(RequestContext context)
    {
    }

    public void RetrievedOperationFromCache(RequestContext context)
    {
    }

    public IDisposable DispatchBatch(RequestContext context) => this;

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
