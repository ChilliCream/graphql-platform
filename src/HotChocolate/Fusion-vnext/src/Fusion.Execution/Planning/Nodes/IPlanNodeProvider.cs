namespace HotChocolate.Fusion.Planning.Nodes;

public interface IPlanNodeProvider
{
    public IReadOnlyList<PlanNode> Nodes { get; }

    public void AddChildNode(PlanNode node);
}
