using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// Provides diagnostic events specific to GraphQL Fusion execution.
/// Extends the core execution diagnostic events with fusion-specific operations
/// such as operation planning, node execution, and source schema interactions.
/// </summary>
public interface IFusionExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents
{
    /// <summary>
    /// Called when the operation is being planned.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <param name="operationId">
    /// The operation unique identifier.
    ///
    /// When the GraphQL operation document contains a single operation,
    /// the operation ID is the same as the document ID.
    ///
    /// When there are multiple operations in the document, it's a combination
    /// of the document ID and the operation name that is being planned.
    /// </param>
    /// <returns>
    /// Returns a scope that is disposed when the planning process is completed.
    /// </returns>
    IDisposable PlanOperation(
        RequestContext context,
        string operationId);

    /// <summary>
    /// Called when the operation plan is stored in the in-memory cache.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <param name="operationId">
    /// The unique identifier of the operation plan being cached.
    /// </param>
    void AddedOperationPlanToCache(
        RequestContext context,
        string operationId);

    /// <summary>
    /// Called when the operation plan was resolved from the in-memory cache.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <param name="operationId">
    /// The unique identifier of the operation plan retrieved from cache.
    /// </param>
    void RetrievedOperationPlanFromCache(
        RequestContext context,
        string operationId);

    /// <summary>
    /// Called when an error occurs during operation planning.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <param name="operationId">
    /// The unique identifier of the operation plan retrieved from cache.
    /// </param>
    /// <param name="error">
    /// The exception that occurred during planning.
    /// </param>
    void PlanOperationError(
        RequestContext context,
        string operationId,
        Exception error);

    /// <summary>
    /// Called when executing an operation plan node that handles Relay-style query nodes.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The node field execution node being executed.
    /// </param>
    /// <returns>
    /// Returns a scope that is disposed when the node field execution is completed.
    /// </returns>
    IDisposable ExecuteNodeFieldNode(
        OperationPlanContext context,
        NodeFieldExecutionNode node);

    /// <summary>
    /// Called when executing an operation plan node that fetches data from a source schema.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The operation execution node being executed.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema being queried.
    /// </param>
    /// <returns>
    /// Returns a scope that is disposed when the operation node execution is completed.
    /// </returns>
    IDisposable ExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName);

    /// <summary>
    /// Called when executing an operation plan node that subscribes to a source schema subscription.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The subscription execution node being executed.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema providing the subscription.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for the subscription instance.
    /// </param>
    /// <returns>
    /// Returns a scope that is disposed when the subscription node execution is completed.
    /// </returns>
    IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName,
        ulong subscriptionId);

    /// <summary>
    /// Called when executing an operation plan node that handles GraphQL introspection fields.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The introspection execution node being executed.
    /// </param>
    /// <returns>
    /// Returns a scope that is disposed when the introspection node execution is completed.
    /// </returns>
    IDisposable ExecuteIntrospectionNode(
        OperationPlanContext context,
        IntrospectionExecutionNode node);

    /// <summary>
    /// Called when a general error occurs in an execution node context.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node where the error occurred.
    /// </param>
    /// <param name="error">
    /// The exception that occurred during execution.
    /// </param>
    void ExecutionNodeError(
        OperationPlanContext context,
        ExecutionNode node,
        Exception error);

    /// <summary>
    /// Called when a transport error occurs while communicating with a source schema.
    /// This includes network errors, timeouts, and other communication failures.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node that was attempting to communicate with the source schema.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema that could not be reached.
    /// </param>
    /// <param name="error">
    /// The transport exception that occurred.
    /// </param>
    void SourceSchemaTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error);

    /// <summary>
    /// Called when an error occurs while storing source schema responses
    /// in the result store of the request.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node that was storing the response.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema whose response could not be stored.
    /// </param>
    /// <param name="error">
    /// The exception that occurred during storage.
    /// </param>
    void SourceSchemaStoreError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception error);

    /// <summary>
    /// Called when GraphQL errors are present in the source schema result.
    /// These are application-level errors returned by the source schema,
    /// not transport or communication errors.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node that received the erroneous response.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema that returned the errors.
    /// </param>
    /// <param name="errors">
    /// The collection of GraphQL errors from the source schema response.
    /// </param>
    void SourceSchemaResultError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        IReadOnlyList<IError> errors);

    /// <summary>
    /// Called when a transport error occurs while communicating with a source schema
    /// during subscription operations. This includes connection drops, network timeouts,
    /// and other communication failures specific to real-time subscriptions.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node that was storing the response.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema whose response could not be stored.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for the subscription instance.
    /// </param>
    /// <param name="exception">
    /// The transport exception that occurred.
    /// </param>
    void SubscriptionTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception);

    /// <summary>
    /// Called when an error occurs while processing a subscription event result.
    /// This covers errors in event handling, data transformation, or result generation
    /// for subscription responses.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node that was storing the response.
    /// </param>
    /// <param name="schemaName">
    /// The name of the source schema whose response could not be stored.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for the subscription instance.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred during event processing.
    /// </param>
    void SubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception);
}
