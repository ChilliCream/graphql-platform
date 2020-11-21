namespace HotChocolate.Data.Neo4J.Language
{
    public class Skip : Visitable
    {
        private readonly NumberLiteral _skipAmount;

        private Skip(NumberLiteral skipAmount)
        {
            _skipAmount = skipAmount;
        }

        public static Skip Create(int value)
        {
            return new Skip(new NumberLiteral(value));
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _skipAmount.Visit(visitor);
            visitor.Leave(this);
        }
    }
}