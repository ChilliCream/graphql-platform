namespace HotChocolate.Data.Neo4J.Language
{
    public class Limit : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Limit;

        private readonly IntegerLiteral _limitAmount;

        private Limit(IntegerLiteral limitAmount) {
            _limitAmount = limitAmount;
        }

        public static Limit Create(int value)
        {
            return new (new IntegerLiteral(value));
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _limitAmount.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
