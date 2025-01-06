namespace HotChocolate.Fusion.Planning.Nodes;

public abstract class PlanNode : IParentPlanNodeProvider
{
    public virtual PlanNode? Parent { get; internal set; }
}

public interface IParentPlanNodeProvider
{
    PlanNode? Parent { get; }
}
