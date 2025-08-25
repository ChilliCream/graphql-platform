using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Provides a hook for custom components to add feature metadata to GraphQL
/// operation plans /// after the initial planning phase is completed.
/// </summary>
public interface IOperationPlannerInterceptor
{
    /// <summary>
    /// Called after the operation plan has been completed, allowing custom metadata
    /// to be added before execution begins.
    /// </summary>
    /// <param name="plan">
    /// The completed operation plan that can be enriched with metadata.
    /// </param>
    void OnAfterPlanCompleted(OperationPlan plan);
}
