using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly record struct ConditionalPolicyExecutionTarget(
    PolicyExecutionTarget Target,
    ImmutableArray<PolicyTargetOccurrence> Occurrences,
    string? FieldName);

internal readonly record struct PolicyTargetOccurrence(
    ExecutionNodeCondition[] Conditions,
    bool GateEligible);
