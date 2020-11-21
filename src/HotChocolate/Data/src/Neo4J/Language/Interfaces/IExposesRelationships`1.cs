namespace HotChocolate.Data.Neo4J.Language
{
    public interface IExposesRelationships<out T> where T : IRelationshipPattern
    {
        /// <summary>
        /// Starts building an outgoing relationship to the other node.
        /// </summary>
        /// <param name="other">The other end of the outgoing relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        public T RelationshipTo(Node other, string[] types);

        /// <summary>
        /// Starts building an incoming relationship starting at the {@code other} {@link Node node}.
        /// </summary>
        /// <param name="other">The source of the incoming relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        public T RelationshipFrom(Node other, string[] types);

        /// <summary>
        /// Starts building an undirected relationship between this {@link Node node} and the {@code other}.
        /// </summary>
        /// <param name="other">The other end of the relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        public T RelationshipBetween(Node other, string[] types);
    }
}