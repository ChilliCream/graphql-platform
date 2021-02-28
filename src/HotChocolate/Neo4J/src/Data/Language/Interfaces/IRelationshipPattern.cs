namespace HotChocolate.Data.Neo4J.Language
{
    public interface IRelationshipPattern : IPatternElement, IExposesRelationships<RelationshipChain>
    {
        /// <summary>
        /// Turns the pattern into a named chain of relationships.
        /// </summary>
        /// <param name="name">The name to be used.</param>
        /// <returns>A named relationship that can be chained with more relationship definitions.</returns>
        IExposesRelationships<RelationshipChain> Named(string name);

        /// <summary>
        /// Transform this pattern into a condition. All names of the patterns must be known upfront in the final statement,
        /// as PatternExpressions are not allowed to introduce new variables.
        /// </summary>
        /// <returns>A condition based on this pattern.</returns>
        Condition AsCondition();
    }
}
