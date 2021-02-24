namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/PatternComprehension.html">PatternComprehension</a>
    /// and <a href="https://neo4j.com/docs/cypher-manual/current/syntax/lists/#cypher-pattern-comprehension">the corresponding cypher manual entry</a>.
    /// </summary>
    public class PatternComprehension : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.PatternComprehension;

        private readonly PatternElement _patternElement;
        private readonly Where _where;
        private readonly Expression _expression;

        private PatternComprehension(PatternElement patternElement, Where where, Expression expression)
        {
            _patternElement = patternElement;
            _where = where;
            _expression = expression;
        }



        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _patternElement.Visit(visitor);
            _where?.Visit(visitor);
            Operator.Pipe.Visit(visitor);
            _expression.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
