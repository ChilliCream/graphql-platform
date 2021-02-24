namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A marker interface for things that expose methods to create new relationships to other elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExposesRelationships<out T> where T: IRelationshipPattern
    {
        /// <summary>
        /// Starts building an outgoing relationship to the {@code other} {@link Node node}.
        /// </summary>
        /// <param name="other">The other end of the outgoing relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        T RelationshipTo(Node other, params string[] types);

        /// <summary>
        /// Starts building an incoming relationship starting at the other node.
        /// </summary>
        /// <param name="other">The source of the incoming relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        T RelationshipFrom(Node other, params string[] types);

        /// <summary>
        /// Starts building an undirected relationship between this Node and the other.
        /// </summary>
        /// <param name="other">The other end of the relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        T RelationshipBetween(Node other, params string[] types);
    }
}
