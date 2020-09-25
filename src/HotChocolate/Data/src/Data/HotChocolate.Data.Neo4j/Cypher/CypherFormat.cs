namespace HotChocolate.Data.Neo4j.CypherBuilder
{
    /// <summary>
    /// Cypher formatting options
    /// </summary>
    public enum CypherFormat
    {
        /// <summary>
        /// A single line of cypher query
        /// </summary>
        SingleLine,
        /// <summary>
        /// Multi line of cypher query
        /// </summary>
        MultiLine,
        /// <summary>
        /// Multi line of cypher query with some density on where and set properties
        /// </summary>
        MultiLineDense
    }
}
