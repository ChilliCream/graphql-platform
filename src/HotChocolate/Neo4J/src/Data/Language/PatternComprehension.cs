namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/PatternComprehension.html">
    /// PatternComprehension
    /// </a>
    /// and
    /// <a href="https://neo4j.com/docs/cypher-manual/current/syntax/lists/#cypher-pattern-comprehension">
    /// the corresponding cypher manual entry
    /// </a>.
    /// </summary>
    public class PatternComprehension : Expression
    {
        private readonly IPatternElement _patternElement;
        private readonly Where? _where;
        private readonly Expression? _expression;

        public PatternComprehension(
            IPatternElement patternElement,
            Where? where,
            Expression? expression)
        {
            _patternElement = patternElement;
            _where = where;
            _expression = expression;
        }

        public PatternComprehension(IPatternElement patternElement, Expression? expression)
        {
            _patternElement = patternElement;
            _where = null;
            _expression = expression;
        }

        public PatternComprehension(IPatternElement patternElement)
        {
            _patternElement = patternElement;
            _where = null;
            _expression = null;
        }

        public override ClauseKind Kind => ClauseKind.PatternComprehension;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _patternElement.Visit(cypherVisitor);
            _where?.Visit(cypherVisitor);
            Operator.Pipe.Visit(cypherVisitor);
            _expression?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
