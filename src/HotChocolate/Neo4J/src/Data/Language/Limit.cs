namespace HotChocolate.Data.Neo4J.Language
{
    public class Limit : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Limit;
        private readonly IntegerLiteral _limitAmount;

        public Limit(IntegerLiteral limitAmount) {
            _limitAmount = limitAmount;
        }

        public Limit (int value)
        {
            _limitAmount = new IntegerLiteral(value);
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _limitAmount.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
