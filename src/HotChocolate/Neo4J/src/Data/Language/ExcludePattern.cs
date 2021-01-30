namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Used to create patterns excluded in a where clause via Not}.
    /// </summary>
    public class ExcludePattern : Condition
    {
        public override ClauseKind Kind => ClauseKind.ExcludePattern;

        private readonly PatternElement _patternElement;

        public ExcludePattern(PatternElement patternElement)
        {
            _patternElement = patternElement;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            Operator.Not.Visit(visitor);
            _patternElement.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
