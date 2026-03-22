using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class BatchOperationDefinition : OperationDefinition
{
    private readonly SelectionPath[] _targets;

    internal BatchOperationDefinition(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath[] targets,
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
        _targets = targets;
    }

    /// <summary>
    /// Gets the paths to the selection sets for which this batch operation
    /// fetches data. Each target corresponds to one of the merged operations.
    /// </summary>
    public ReadOnlySpan<SelectionPath> Targets => _targets;
}
