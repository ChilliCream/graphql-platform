using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class NoopFusionExecutionDiagnosticEvents : IFusionExecutionDiagnosticEvents, IDisposable
{
    public IDisposable ExecuteRequest(RequestContext context) => this;

    public IDisposable ParseDocument(RequestContext context) => this;

    public IDisposable ValidateDocument(RequestContext context) => this;

    public IDisposable CoerceVariables(RequestContext context) => this;

    public IDisposable ExecuteOperation(RequestContext context) => this;

    public IDisposable ExecuteSubscription(RequestContext context) => this;

    public IDisposable OnSubscriptionEvent(RequestContext context) => this;

    public void ExecutionError(RequestContext context, ErrorKind kind, IReadOnlyList<IError> errors, object? state = null) { }

    public void AddedDocumentToCache(RequestContext context) { }

    public void RetrievedDocumentFromCache(RequestContext context) { }

    public void RetrievedDocumentFromStorage(RequestContext context) { }

    public void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId) { }

    public void ExecutorCreated(string name, IRequestExecutor executor) { }

    public void ExecutorEvicted(string name, IRequestExecutor executor) { }

    public void Dispose() { }
}
