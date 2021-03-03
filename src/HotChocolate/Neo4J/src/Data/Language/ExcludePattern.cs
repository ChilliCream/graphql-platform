namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Used to create patterns excluded in a where clause via Not}.
    /// </summary>
    public class ExcludePattern : Condition
    {
        public override ClauseKind Kind => ClauseKind.ExcludePattern;

        private readonly IPatternElement _patternElement;

        public ExcludePattern(IPatternElement patternElement)
        {
            _patternElement = patternElement;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Operator.Not.Visit(cypherVisitor);
            _patternElement.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
