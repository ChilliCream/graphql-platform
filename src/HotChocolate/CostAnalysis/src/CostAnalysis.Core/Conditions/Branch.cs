namespace HotChocolate.CostAnalysis;

/// <summary>
/// One condition-tree edge: the branch condition that must hold, and the id
/// of the node it leads to.
/// </summary>
internal readonly record struct Branch(BranchCondition Condition, int TargetNodeId);
