namespace HotChocolate.Data.Neo4J.Language;

public class Remove : Visitable, IUpdatingClause
{
    public Remove(ExpressionList removeItems)
    {
        RemoveItems = removeItems;
    }

    public override ClauseKind Kind => ClauseKind.Remove;
    public ExpressionList RemoveItems { get; }
    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        RemoveItems.Visit(cypherVisitor);
        cypherVisitor.Leave(this);
    }
}
