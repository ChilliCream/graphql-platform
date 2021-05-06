namespace HotChocolate.Data.Neo4J.Language
{
    public class Skip : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Skip;
        private readonly IntegerLiteral _skipAmount;

        public Skip(IntegerLiteral skipAmount)
        {
            _skipAmount = skipAmount;
        }

        public Skip(int skipAmount)
        {
            _skipAmount = new IntegerLiteral(skipAmount);
        }

        public static Skip Create(int value)
        {
            return new (new IntegerLiteral(value));
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _skipAmount.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
