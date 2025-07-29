namespace HotChocolate.Fusion.Execution.Nodes;

public record ExecutionNodeResult(int Id, ExecutionStatus Status, TimeSpan Duration);
