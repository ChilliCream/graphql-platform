using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal readonly record struct ConditionedSelectionSet(
    SelectionSet SelectionSet,
    ExecutionNodeCondition[] Conditions);
