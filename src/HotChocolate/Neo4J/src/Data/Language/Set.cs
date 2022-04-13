namespace HotChocolate.Data.Neo4J.Language;

public class Set : Visitable, IUpdatingClause
{
    public Set(ExpressionList setItems)
    {
        SetItems = setItems;
    }

    public override ClauseKind Kind => ClauseKind.Set;
    public ExpressionList SetItems { get; }
    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        SetItems.Visit(cypherVisitor);
        cypherVisitor.Leave(this);
    }
}
