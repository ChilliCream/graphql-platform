using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class AggregateFusionExecutionDiagnosticEvents(
    IFusionExecutionDiagnosticEventListener[] listeners)
    : IFusionExecutionDiagnosticEvents
{
    public IDisposable ExecuteRequest(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteRequest(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void RequestError(RequestContext context, Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RequestError(context, error);
        }
    }

    public void RequestError(RequestContext context, IError error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RequestError(context, error);
        }
    }

    public IDisposable ParseDocument(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ParseDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ValidateDocument(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ValidateDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ValidationErrors(context, errors);
        }
    }

    public IDisposable CoerceVariables(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].CoerceVariables(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void AddedDocumentToCache(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].AddedDocumentToCache(context);
        }
    }

    public void RetrievedDocumentFromCache(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RetrievedDocumentFromCache(context);
        }
    }

    public void RetrievedDocumentFromStorage(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RetrievedDocumentFromStorage(context);
        }
    }

    public void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].DocumentNotFoundInStorage(context, documentId);
        }
    }

    public void UntrustedDocumentRejected(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].UntrustedDocumentRejected(context);
        }
    }

    public IDisposable PlanOperation(RequestContext context, string operationPlanId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].PlanOperation(context, operationPlanId);
        }

        return new AggregateActivityScope(scopes);
    }

    public void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].AddedOperationPlanToCache(context, operationPlanId);
        }
    }

    public void RetrievedOperationPlanFromCache(RequestContext context, string operationPlanId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RetrievedOperationPlanFromCache(context, operationPlanId);
        }
    }

    public void PlanOperationError(RequestContext context, string operationId, Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].PlanOperationError(context, operationId, error);
        }
    }

    public IDisposable EvaluateRequestPolicies(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].EvaluateRequestPolicies(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void PolicyEvaluated(
        RequestContext context,
        string policyName,
        bool denied,
        TimeSpan duration)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].PolicyEvaluated(context, policyName, denied, duration);
        }
    }

    public void PolicySlotDenied(
        RequestContext context,
        string slotVariableName,
        string policyExpression,
        string typeName,
        string? fieldName,
        PolicyDenialBehavior behavior,
        string? reason,
        Guid reasonId,
        string? subjectId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].PolicySlotDenied(
                context,
                slotVariableName,
                policyExpression,
                typeName,
                fieldName,
                behavior,
                reason,
                reasonId,
                subjectId);
        }
    }

    public IDisposable ExecuteOperation(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteOperation(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteOperationNode(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteApolloOperationExecutionNode(
        OperationPlanContext context,
        ApolloOperationExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteApolloOperationExecutionNode(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteOperationBatchNode(
        OperationPlanContext context,
        OperationBatchExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteOperationBatchNode(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteApolloOperationBatchExecutionNode(
        OperationPlanContext context,
        ApolloOperationBatchExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteApolloOperationBatchExecutionNode(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSourceSchemaRequest(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSourceSchemaRequest(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSourceSchemaRequest(
        OperationPlanContext context,
        ApolloOperationExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSourceSchemaRequest(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSourceSchemaRequest(
        OperationPlanContext context,
        ApolloOperationBatchExecutionNode node,
        string schemaName)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSourceSchemaRequest(context, node, schemaName);
        }

        return new AggregateActivityScope(scopes);
    }

    public void SourceSchemaTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].SourceSchemaTransportError(context, node, schemaName, error);
        }
    }

    public void SourceSchemaStoreError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].SourceSchemaStoreError(context, node, schemaName, error);
        }
    }

    public void ExecutionNodeError(OperationPlanContext context, ExecutionNode node, Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutionNodeError(context, node, error);
        }
    }

    public IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSubscription(context, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSubscriptionNode(context, node, schemaName, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public void SubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].SubscriptionEventError(
                context,
                node,
                schemaName,
                subscriptionId,
                exception);
        }
    }

    public IDisposable OnSubscriptionEvent(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].OnSubscriptionEvent(
                context,
                node,
                schemaName,
                subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public void SubscriptionEventDelivered(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].SubscriptionEventDelivered(
                context,
                node,
                schemaName,
                subscriptionId);
        }
    }

    public IDisposable ExecuteNodeFieldNode(OperationPlanContext context, NodeFieldExecutionNode node)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteNodeFieldNode(context, node);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteIntrospectionNode(OperationPlanContext context, IntrospectionExecutionNode node)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteIntrospectionNode(context, node);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ExecutorCreated(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutorCreated(name, executor);
        }
    }

    public void ExecutorEvicted(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutorEvicted(name, executor);
        }
    }

    public void PolicyCompilationError(string policyName, Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].PolicyCompilationError(policyName, error);
        }
    }

    public void PolicyUpdateError(Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].PolicyUpdateError(error);
        }
    }

    public void ConfigurationReadError(Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ConfigurationReadError(error);
        }
    }

    private sealed class AggregateActivityScope(IDisposable[] scopes) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var scope in scopes)
                {
                    scope.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
