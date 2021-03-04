namespace StrawberryShake
{
    /// <summary>
    /// Specifies the GraphQL request strategy.
    /// </summary>
    public enum RequestStrategy
    {
        /// <summary>
        /// The full GraphQL query is send.
        /// </summary>
        Default,

        /// <summary>
        /// An id is send representing the query that is stored on the server.
        /// </summary>
        PersistedQuery,

        /// <summary>
        /// The full GraphQL query is only send if the server has not yet stored the
        /// persisted query.
        /// </summary>
        AutomaticPersistedQuery
    }
}
