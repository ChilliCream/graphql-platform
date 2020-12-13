namespace HotChocolate.Data.Neo4J.Language
{
    public class Skip : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Skip;
        private readonly IntegerLiteral _skipAmount;

        private Skip(IntegerLiteral skipAmount)
        {
            _skipAmount = skipAmount;
        }

        public static Skip Create(int value)
        {
            return new Skip(new IntegerLiteral(value));
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _skipAmount.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
