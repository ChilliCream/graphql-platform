namespace HotChocolate.Data.Neo4J.Language
{
    public class Limit : Visitable
    {
        public Limit(IntegerLiteral amount)
        {
            Amount = amount;
        }

        public Limit(int amount)
        {
            Amount = new IntegerLiteral(amount);
        }

        public override ClauseKind Kind => ClauseKind.Limit;

        public IntegerLiteral Amount { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Amount.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
