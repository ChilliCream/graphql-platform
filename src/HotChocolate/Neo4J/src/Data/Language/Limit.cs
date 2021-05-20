namespace HotChocolate.Data.Neo4J.Language
{
    public class Limit : Visitable
    {
        private readonly IntegerLiteral _limitAmount;

        public Limit(IntegerLiteral limitAmount)
        {
            _limitAmount = limitAmount;
        }

        public Limit(int value)
        {
            _limitAmount = new IntegerLiteral(value);
        }

        public override ClauseKind Kind => ClauseKind.Limit;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _limitAmount.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
