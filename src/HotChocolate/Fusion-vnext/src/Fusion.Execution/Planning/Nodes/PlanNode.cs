namespace HotChocolate.Fusion.Planning;

public abstract class PlanNode
{
    public PlanNode? Parent { get; internal set; }
}
