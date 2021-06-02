namespace HotChocolate.Data.Neo4J.Language
{
    public class Skip : Visitable
    {
        public Skip(IntegerLiteral amount)
        {
            Amount = amount;
        }

        public Skip(int skipAmount)
        {
            Amount = new IntegerLiteral(skipAmount);
        }

        public override ClauseKind Kind => ClauseKind.Skip;

        public IntegerLiteral Amount { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Amount.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public static Skip Create(int value)
        {
            return new(new IntegerLiteral(value));
        }
    }
}
