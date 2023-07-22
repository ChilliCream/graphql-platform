using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

internal readonly record struct NodeAndStep(QueryPlanNode Node, ExecutionStep Step);
