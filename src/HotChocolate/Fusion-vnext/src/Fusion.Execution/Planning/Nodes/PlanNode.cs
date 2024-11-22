namespace HotChocolate.Fusion.Planning.Nodes;

public abstract class PlanNode
{
    public PlanNode? Parent { get; internal set; }
}
