namespace HotChocolate.Data.Neo4J.Language;

public class Create : Visitable, IUpdatingClause
{
    public Create(Pattern pattern)
    {
        Pattern = pattern;
    }

    public override ClauseKind Kind => ClauseKind.Create;
    public Pattern Pattern { get; }
    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        Pattern.Visit(cypherVisitor);
        cypherVisitor.Leave(this);
    }
}
