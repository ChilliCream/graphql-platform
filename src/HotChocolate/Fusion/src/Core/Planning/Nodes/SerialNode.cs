namespace HotChocolate.Fusion.Planning;

internal sealed class SerialNode : QueryPlanNode
{
    public SerialNode(int id) : base(id) { }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Serial;
}
