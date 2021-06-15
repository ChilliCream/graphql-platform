namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Internal wrapper for marking a path pattern as a condition.
    /// </summary>
    public class RelationshipPatternCondition : Condition
    {
        public RelationshipPatternCondition(IRelationshipPattern pattern)
        {
            Pattern = pattern;
        }

        public override ClauseKind Kind => ClauseKind.RelationshipPatternCondition;

        public IRelationshipPattern Pattern { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Pattern.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
