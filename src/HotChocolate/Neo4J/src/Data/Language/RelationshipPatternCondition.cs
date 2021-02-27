namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Internal wrapper for marking a path pattern as a condition.
    /// </summary>
    class RelationshipPatternCondition : Condition
    {
        public override ClauseKind Kind => ClauseKind.RelationshipPatternCondition;

        private readonly IRelationshipPattern _pattern;

        public RelationshipPatternCondition(IRelationshipPattern pattern)
        {
            _pattern = pattern;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _pattern.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
