namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A marker interface for things that expose methods to create new relationships to other elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExposesRelationship<T> where T: RelationshipPattern
    {
        /// <summary>
        /// Starts building an outgoing relationship to the {@code other} {@link Node node}.
        /// </summary>
        /// <param name="other">The other end of the outgoing relationship</param>
        /// <param name="types">The types to match</param>
        /// <returns>An ongoing relationship definition, that can be used to specify the type</returns>
        T RelationshipTo(Node other, params string[] types);

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        T RelationshipFrom(Node other, params string[] types);

        T RelationshipBetween(Node other, params string[] types);
    }
}
