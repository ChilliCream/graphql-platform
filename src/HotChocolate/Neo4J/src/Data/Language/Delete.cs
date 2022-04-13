namespace HotChocolate.Data.Neo4J.Language;

public class Delete : Visitable, IUpdatingClause
{
    public Delete(ExpressionList deletedItems, bool detach = false)
    {
        DeletedItems = deletedItems;
        IsDetached = detach;
    }

    public override ClauseKind Kind => ClauseKind.Create;
    public ExpressionList DeletedItems { get; }
    public bool IsDetached { get; }
    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        DeletedItems.Visit(cypherVisitor);
        cypherVisitor.Leave(this);
    }
}
