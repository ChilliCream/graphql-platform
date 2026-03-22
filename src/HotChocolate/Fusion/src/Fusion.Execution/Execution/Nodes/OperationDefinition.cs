using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract record OperationDefinition(
    int Id,
    OperationSourceText Operation,
    string? SchemaName,
    SelectionPath Source,
    OperationRequirement[] Requirements,
    string[] ForwardedVariables,
    ResultSelectionSet ResultSelectionSet,
    ExecutionNodeCondition[] Conditions,
    int? BatchingGroupId,
    bool RequiresFileUpload)
{
    /// <summary>
    /// Gets the execution nodes that depend on this operation definition
    /// to be completed before they can be executed.
    /// </summary>
    public ExecutionNode[] Dependents { get; set; } = [];

    /// <summary>
    /// Gets the execution nodes that this operation definition depends on.
    /// </summary>
    public ExecutionNode[] Dependencies { get; set; } = [];
}
