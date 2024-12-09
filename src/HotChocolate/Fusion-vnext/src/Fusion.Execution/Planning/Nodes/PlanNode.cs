namespace HotChocolate.Fusion.Planning.Nodes;

public abstract class PlanNode
{
    public virtual PlanNode? Parent { get; internal set; }
}
