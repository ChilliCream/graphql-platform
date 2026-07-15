using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly record struct ConditionalPolicyExecutionTarget(
    PolicyExecutionTarget Target,
    ExecutionNodeCondition[] Conditions);
