using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
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
    public virtual void RequestError(RequestContext context, Exception error)
    {
    }

    /// <inheritdoc />
    public virtual void RequestError(RequestContext context, IError error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ParseDocument(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ValidateDocument(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable CoerceVariables(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual void AddedDocumentToCache(RequestContext context) { }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromCache(RequestContext context) { }

    /// <inheritdoc />
    public virtual void RetrievedDocumentFromStorage(RequestContext context) { }

    /// <inheritdoc />
    public virtual void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId) { }

    /// <inheritdoc />
    public virtual void UntrustedDocumentRejected(RequestContext context) { }

    /// <inheritdoc />
    public virtual IDisposable PlanOperation(RequestContext context, string operationPlanId)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void AddedOperationPlanToCache(RequestContext context, string operationPlanId) { }

    /// <inheritdoc />
    public virtual void RetrievedOperationPlanFromCache(RequestContext context, string operationPlanId) { }

    /// <inheritdoc />
    public virtual void PlanOperationError(
        OperationPlanContext context,
        ExecutionNode node,
        Exception error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ExecuteOperation(RequestContext context) => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void SourceSchemaTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
    }

    /// <inheritdoc />
    public virtual void SourceSchemaStoreError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
    }

    /// <inheritdoc />
    public virtual void SourceSchemaResultError(
        OperationPlanContext context,
        ExecutionNode node, string schemaName,
        IReadOnlyCollection<IError> errors)
    {
    }

    /// <inheritdoc />
    public virtual void ExecutionNodeError(
        OperationPlanContext context,
        ExecutionNode node,
        Exception error)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId)
        => EmptyScope;

    public IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public virtual void SubscriptionTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual void SubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual IDisposable ExecuteNodeFieldNode(OperationPlanContext context, NodeFieldExecutionNode node)
        => EmptyScope;

    /// <inheritdoc />
    public virtual IDisposable ExecuteIntrospectionNode(OperationPlanContext context, IntrospectionExecutionNode node)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void ExecutorCreated(string name, IRequestExecutor executor) { }

    /// <inheritdoc />
    public virtual void ExecutorEvicted(string name, IRequestExecutor executor) { }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose() { }
    }
}
