using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class SingleOperationDefinition : OperationDefinition
{
    internal SingleOperationDefinition(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload)
        : base(
            id,
            operation,
            schemaName,
            source,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            conditions,
            requiresFileUpload)
    {
        Target = target;
    }

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target { get; }
}
