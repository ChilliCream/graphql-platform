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
    /// The key for setting a flag the a document was saved to the persisted operation storage.
    /// </summary>
    public const string DocumentSaved = "HotChocolate.Execution.DocumentSaved";

    /// <summary>
    /// The key that specifies that the current context allows standard operations
    /// that are not known to the server.
    /// </summary>
    public const string NonPersistedOperationAllowed = "HotChocolate.Execution.NonPersistedOperationAllowed";

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
    public const string OperationSessionId = "HotChocolate.Execution.Transport.OperationSessionId";

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
    /// The key to get the serializer types.
    /// </summary>
    public const string SerializerTypes = "HotChocolate.Relay.SerializerTypes";

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

    /// <summary>
    /// The key to get check if a field is the node field.
    /// </summary>
    public const string IsNodeField = "HotChocolate.Relay.Node.IsNodeField";

    /// <summary>
    /// The key to get check if a field is the nodes field.
    /// </summary>
    public const string IsNodesField = "HotChocolate.Relay.Node.IsNodeField";

    /// <summary>
    /// The key to override the max allowed execution depth.
    /// </summary>
    public const string MaxAllowedExecutionDepth = "HotChocolate.Execution.MaxAllowedDepth";

    /// <summary>
    /// The key to skip the execution depth analysis.
    /// </summary>
    public const string SkipDepthAnalysis = "HotChocolate.Execution.SkipDepthAnalysis";

    /// <summary>
    /// The key to the name of the data field when using the mutation convention.
    /// </summary>
    public const string MutationConventionDataField = "HotChocolate.Types.Mutations.Conventions.DataField";

    /// <summary>
    /// The key to get the Cache-Control header value from the context data.
    /// </summary>
    public const string CacheControlHeaderValue = "HotChocolate.Caching.CacheControlHeaderValue";

    /// <summary>
    /// The key to get the Vary header value from the context data.
    /// </summary>
    public const string VaryHeaderValue = "HotChocolate.Caching.VaryHeaderValue";

    /// <summary>
    /// The key to to ski caching a query result.
    /// </summary>
    public const string SkipQueryCaching = "HotChocolate.Caching.SkipQueryCaching";

    /// <summary>
    /// The key to retrieve the cache constraints from the operation.
    /// </summary>
    public const string CacheControlConstraints = "HotChocolate.Caching.CacheControlConstraints";

    /// <summary>
    /// The key to retrieve the authorization options from the context.
    /// </summary>
    public const string AuthorizationOptions = "HotChocolate.Authorization.Options";

    /// <summary>
    /// The key to check if this schema contains request policies.
    /// </summary>
    public const string AuthorizationRequestPolicy = "HotChocolate.Authorization.RequestPolicy";

    /// <summary>
    /// The key to access the user state on the global context.
    /// </summary>
    public const string UserState = "HotChocolate.Authorization.UserState";

    /// <summary>
    /// The key to access the authorization handler on the global context.
    /// </summary>
    public const string AuthorizationHandler = "HotChocolate.Authorization.AuthorizationHandler";

    /// <summary>
    /// The key to access the authorization allowed flag on the member context.
    /// </summary>
    public const string AllowAnonymous = "HotChocolate.Authorization.AllowAnonymous";

    /// <summary>
    /// The key to access the tag options object.
    /// </summary>
    public const string TagOptions = "HotChocolate.Types.TagOptions";

    /// <summary>
    /// Type key to access the internal schema options.
    /// </summary>
    public const string InternalSchemaOptions = "HotChocolate.Types.InternalSchemaOptions";

    /// <summary>
    /// Type key to access the paging arguments in the local resolver state.
    /// </summary>
    public const string PagingArguments = "HotChocolate.Types.PagingArguments";

    /// <summary>
    /// Type key to access the paging options.
    /// </summary>
    public const string PagingOptions = "HotChocolate.Types.PagingOptions";

    /// <summary>
    /// Type key to access the object type configuration hooks in the schema builder context data.
    /// </summary>
    public const string ObjectTypeConfigurations = "HotChocolate.Types.ObjectTypeConfigurations";

    /// <summary>
    /// Type key to access the node id result formatter on the descriptor context.
    /// </summary>
    public const string NodeIdResultFormatter = "HotChocolate.Relay.NodeIdResultFormatter";

    /// <summary>
    /// Type key to access the pattern validation tasks.
    /// </summary>
    public const string PatternValidationTasks = "HotChocolate.Validation.PatternValidationTasks";

    /// <summary>
    /// The key to retrieve the cost metrics from the global state.
    /// </summary>
    public const string CostMetrics = "HotChocolate.CostAnalysis.CostMetricsKey";

    /// <summary>
    /// The key to set the flag that the cost should be reported in the response.
    /// </summary>
    public const string ReportCost = "HotChocolate.CostAnalysis.ReportCost";

    /// <summary>
    /// The key to set the flag that only the cost should be validated and the request should not be executed.
    /// </summary>
    public const string ValidateCost = "HotChocolate.CostAnalysis.ValidateCost";

    /// <summary>
    /// The key to access the paging observers stored on the local resolver state.
    /// </summary>
    public const string PagingObserver = "HotChocolate.Types.PagingObserver";

    /// <summary>
    /// The key to access the requirements syntax on an object field definition.
    /// </summary>
    public const string FieldRequirementsSyntax = "HotChocolate.Types.ObjectField.Requirements.Syntax";

    /// <summary>
    /// The key to access the requirements entity type on an object field definition.
    /// </summary>
    public const string FieldRequirementsEntity = "HotChocolate.Types.ObjectField.Requirements.EntityType";

    /// <summary>
    /// The key to access the compiled requirements.
    /// </summary>
    public const string FieldRequirements = "HotChocolate.Types.ObjectField.Requirements";

    /// <summary>
    /// The key to determine whether the request is a warmup request.
    /// </summary>
    public const string IsWarmupRequest = "HotChocolate.AspNetCore.Warmup.IsWarmupRequest";
}
