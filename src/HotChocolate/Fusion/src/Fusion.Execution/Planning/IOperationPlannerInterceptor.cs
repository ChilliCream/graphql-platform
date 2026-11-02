using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Provides a hook for custom components to add feature metadata to GraphQL
/// operation plans after the initial planning phase is completed.
/// </summary>
public interface IOperationPlannerInterceptor
{
    /// <summary>
    /// Called after the operation plan has been completed, allowing custom metadata
    /// to be added before execution begins.
    /// </summary>
    /// <param name="operationDocumentInfo">
    /// The operation document on which the plan was created.
    /// </param>
    /// <param name="operationPlan">
    /// The completed operation plan that can be enriched with metadata.
    /// </param>
    void OnAfterPlanCompleted(
        OperationDocumentInfo operationDocumentInfo,
        OperationPlan operationPlan);
}
