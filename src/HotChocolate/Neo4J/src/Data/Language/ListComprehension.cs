namespace HotChocolate.Data.Neo4J.Language
{
    public class ListComprehension : Expression
    {
        private readonly SymbolicName _variable;
        private readonly Expression _expression;
        private readonly Where _where;
        public override ClauseKind Kind { get; }
    }
}
