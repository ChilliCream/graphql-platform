namespace HotChocolate.Fusion.Planning;

internal sealed class Sequence : QueryPlanNode
{
    public Sequence(int id) : base(id) { }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Sequence;
}
