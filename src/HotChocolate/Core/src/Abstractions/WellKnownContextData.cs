namespace HotChocolate;

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
    /// The key allows users to override the status code behavior of the default
    /// HTTP response formatter.
    /// </summary>
    public const string HttpStatusCode = "HotChocolate.Execution.Transport.HttpStatusCode";

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

    /// <summary>
    /// The key for setting resolver configurations.
    /// </summary>
    public const string ResolverConfigs = "HotChocolate.Types.ResolverConfigs";

    /// <summary>
    /// The key for setting resolver types.
    /// </summary>
    public const string ResolverTypes = "HotChocolate.Types.ResolverTypes";

    /// <summary>
    /// The key for setting runtime types.
    /// </summary>
    public const string RuntimeTypes = "HotChocolate.Types.RuntimeTypes";

    /// <summary>
    /// The key for setting root instances.
    /// </summary>
    public const string RootInstance = "HotChocolate.Types.RootInstance";

    /// <summary>
    /// The key identifies the resolver scope on the local context.
    /// </summary>
    public const string ResolverServiceScope = "HotChocolate.Resolvers.ServiceScope";

    /// <summary>
    /// The key to the current executor.
    /// </summary>
    public const string RequestExecutor = "HotChocolate.Execution.RequestExecutor";

    /// <summary>
    /// The key to the current schema name.
    /// </summary>
    public const string SchemaName = "HotChocolate.SchemaName";

    /// <summary>
    /// The key to the current schema.
    /// </summary>
    public const string Schema = "HotChocolate.Schema";

    /// <summary>
    /// The key to the schema building directives.
    /// </summary>
    public const string SchemaDirectives = "HotChocolate.Schema.Building.Directives";

    /// <summary>
    /// The key to the optional schema documents.
    /// </summary>
    public const string SchemaDocuments = "HotChocolate.Schema.Building.Documents";

    /// <summary>
    /// The key to get the user provided transport operation session id when executing
    /// GraphQL over Websocket.
    /// </summary>
    public const string OperationSessionId ="HotChocolate.Execution.Transport.OperationSessionId";

    /// <summary>
    /// The key to get the deferred task ID on the scoped context data.
    /// </summary>
    public const string DeferredResultId = "HotChocolate.Execution.Defer.ResultId";

    /// <summary>
    /// The key to overwrite the root type instance for a request.
    /// </summary>
    public const string InitialValue = "HotChocolate.Execution.InitialValue";

    /// <summary>
    /// The key to lookup significant results that were removed during execution.
    /// </summary>
    public const string RemovedResults = "HotChocolate.Execution.RemovedResults";

    /// <summary>
    /// The key to lookup result sets that expect data patches.
    /// </summary>
    public const string ExpectedPatches = "HotChocolate.Execution.ExpectedPatches";

    /// <summary>
    /// The key to the patch ID of a result set. The patch ID references the result into which
    /// the result set containing the patch ID shall be patched into.
    /// </summary>
    public const string PatchId = "HotChocolate.Execution.PatchId";

    /// <summary>
    /// The key to get the type discovery interceptors from the schema context data.
    /// </summary>
    public const string TypeDiscoveryHandlers = "HotChocolate.Execution.TypeDiscoveryHandlers";

    /// <summary>
    /// The key to get the node resolvers.
    /// </summary>
    public const string NodeResolver = "HotChocolate.Relay.Node.Resolver";

    /// <summary>
    /// The key to check if relay support is enabled.
    /// </summary>
    public const string IsRelaySupportEnabled = "HotChocolate.Relay.IsEnabled";

    /// <summary>
    /// The key to check if the global identification spec is enabled.
    /// </summary>
    public const string GlobalIdSupportEnabled = "HotChocolate.Relay.GlobalId";

    /// <summary>
    /// The key to get the node id from the context data.
    /// </summary>
    public const string NodeId = "HotChocolate.Relay.Node.Id";

    /// <summary>
    /// The key to get the internal id from the context data.
    /// </summary>
    public const string InternalId = "HotChocolate.Relay.Node.Id.InternalId";

    /// <summary>
    /// The key to get the id type name from the context data.
    /// </summary>
    public const string InternalTypeName = "HotChocolate.Relay.Node.Id.InternalTypeName";

    /// <summary>
    /// The key to get the id type from the context data.
    /// </summary>
    public const string InternalType = "HotChocolate.Relay.Node.Id.InternalType";

    /// <summary>
    /// The key to get the IdValue object from the context data.
    /// </summary>
    public const string IdValue = "HotChocolate.Relay.Node.Id.Value";
}
