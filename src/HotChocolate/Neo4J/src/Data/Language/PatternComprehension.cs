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
        public PatternComprehension(
            IPatternElement patternElement,
            Where? where,
            Expression? expression)
        {
            PatternElement = patternElement;
            Where = where;
            Expression = expression;
        }

        public PatternComprehension(IPatternElement patternElement, Expression? expression)
        {
            PatternElement = patternElement;
            Where = null;
            Expression = expression;
        }

        public PatternComprehension(IPatternElement patternElement)
        {
            PatternElement = patternElement;
            Where = null;
            Expression = null;
        }

        public override ClauseKind Kind => ClauseKind.PatternComprehension;

        public IPatternElement PatternElement { get;}

        public Where? Where { get;}

        public Expression? Expression { get;}

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            PatternElement.Visit(cypherVisitor);
            Where?.Visit(cypherVisitor);
            Operator.Pipe.Visit(cypherVisitor);
            Expression?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
