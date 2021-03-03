namespace HotChocolate.Data.Neo4J.Language
{
    public interface IRelationshipPattern : IPatternElement, IExposesRelationships<RelationshipChain>
    {
        /// <summary>
        /// Transform this pattern into a condition. All names of the patterns must be known upfront in the final statement,
        /// as PatternExpressions are not allowed to introduce new variables.
        /// </summary>
        /// <returns>A condition based on this pattern.</returns>
        Condition AsCondition();
    }
}
