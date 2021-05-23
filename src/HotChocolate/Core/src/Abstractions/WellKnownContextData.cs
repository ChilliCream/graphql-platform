namespace HotChocolate
{
    /// <summary>
    /// Provides keys for well-known context data.
    /// </summary>
    public static class WellKnownContextData
    {
        /// <summary>
        /// The key for storing the event message / event payload to the context data.
        /// </summary>
        public const string EventMessage = "HotChocolate.Execution.EventMessage";

        /// <summary>
        /// The key for storing the subscription object to the context data.
        /// </summary>
        public const string Subscription = "HotChocolate.Execution.Subscription";

        /// <summary>
        /// The key for storing the enable tracing flag to the context data.
        /// </summary>
        public const string EnableTracing = "HotChocolate.Execution.EnableTracing";

        /// <summary>
        /// The key for setting a flag the a document was saved to the persisted query storage.
        /// </summary>
        public const string DocumentSaved = "HotChocolate.Execution.DocumentSaved";

        /// <summary>
        /// The key for setting a flag that the execution had document validation errors.
        /// </summary>
        public const string ValidationErrors = "HotChocolate.Execution.ValidationErrors";

        /// <summary>
        /// The key for setting a flag that an operation was not allowed during request execution.
        /// </summary>
        public const string OperationNotAllowed = "HotChocolate.Execution.OperationNotAllowed";

        /// <summary>
        /// The key for setting a flag that introspection is allowed for this request.
        /// </summary>
        public const string IntrospectionAllowed = "HotChocolate.Execution.Introspection.Allowed";

        /// <summary>
        /// The key for setting a message that is being used when introspection is not allowed.
        /// </summary>
        public const string IntrospectionMessage = "HotChocolate.Execution.Introspection.Message";

        /// <summary>
        /// Signals that the complexity analysis shall be skipped.
        /// </summary>
        public const string SkipComplexityAnalysis = "HotChocolate.Execution.NoComplexityAnalysis";

        /// <summary>
        /// The key for setting the operation complexity.
        /// </summary>
        public const string OperationComplexity = "HotChocolate.Execution.OperationComplexity";

        /// <summary>
        /// The key for setting the maximum operation complexity.
        /// </summary>
        public const string MaximumAllowedComplexity = "HotChocolate.Execution.AllowedComplexity";

        /// <summary>
        /// Includes the query plan into the response.
        /// </summary>
        public const string IncludeQueryPlan = "HotChocolate.Execution.EmitQueryPlan";
    }
}
