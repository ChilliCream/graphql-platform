namespace HotChocolate.Types
{
    /// <summary>
    /// These constants represent well-known names for the operation types.
    /// </summary>
    public static class OperationTypeNames
    {
        /// <summary>
        /// The well-known name for the query type.
        /// </summary>
        public const string Query = nameof(Query);

        /// <summary>
        /// The well-known name for the mutation type.
        /// </summary>
        public const string Mutation = nameof(Mutation);

        /// <summary>
        /// The well-known name for the subscription type.
        /// </summary>
        public const string Subscription = nameof(Subscription);
    }
}
