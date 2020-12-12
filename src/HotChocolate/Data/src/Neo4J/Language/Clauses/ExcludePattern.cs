namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Used to create patterns excluded in a where clause via Not}.
    /// </summary>
    public class ExcludePattern : Condition
    {
        public new ClauseKind Kind { get; } = ClauseKind.ExcludePattern;

        private readonly PatternElement _patternElement;

        public ExcludePattern(PatternElement patternElement)
        {
            _patternElement = patternElement;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            Operator.Not.Visit(visitor);
            _patternElement.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
