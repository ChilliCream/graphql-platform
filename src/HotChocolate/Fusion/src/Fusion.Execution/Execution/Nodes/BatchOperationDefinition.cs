using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed record BatchOperationDefinition(
    int Id,
    OperationSourceText Operation,
    string? SchemaName,
    SelectionPath[] Targets,
    SelectionPath Source,
    OperationRequirement[] Requirements,
    string[] ForwardedVariables,
    ResultSelectionSet ResultSelectionSet,
    ExecutionNodeCondition[] Conditions,
    int? BatchingGroupId,
    bool RequiresFileUpload)
    : OperationDefinition(
        Id,
        Operation,
        SchemaName,
        Source,
        Requirements,
        ForwardedVariables,
        ResultSelectionSet,
        Conditions,
        BatchingGroupId,
        RequiresFileUpload);
