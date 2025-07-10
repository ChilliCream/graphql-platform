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
    /// The key to check if relay support is enabled.
    /// </summary>
    public const string IsRelaySupportEnabled = "HotChocolate.Relay.IsEnabled";

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
    /// The key to skip caching a query result.
    /// </summary>
    public const string SkipQueryCaching = "HotChocolate.Caching.SkipQueryCaching";

    /// <summary>
    /// Type key to access the paging arguments in the local resolver state.
    /// </summary>
    public const string PagingArguments = "HotChocolate.Types.PagingArguments";

    /// <summary>
    /// Type key to access the object type configuration hooks in the schema builder context data.
    /// </summary>
    public const string ObjectTypeConfigurations = "HotChocolate.Types.ObjectTypeConfigurations";

    /// <summary>
    /// Type key to access the node id result formatter on the descriptor context.
    /// </summary>
    public const string NodeIdResultFormatter = "HotChocolate.Relay.NodeIdResultFormatter";

    /// <summary>
    /// The key to access the paging observers stored on the local resolver state.
    /// </summary>
    public const string PagingObserver = "HotChocolate.Types.PagingObserver";

    /// <summary>
    /// The key to access the requirement syntax on an object field definition.
    /// </summary>
    public const string FieldRequirementSyntax = "HotChocolate.Types.ObjectField.Requirements.Syntax";

    /// <summary>
    /// The key to access the requirement entity type on an object field definition.
    /// </summary>
    public const string FieldRequirementsEntity = "HotChocolate.Types.ObjectField.Requirements.EntityType";

    /// <summary>
    /// The key to access the compiled requirements.
    /// </summary>
    public const string FieldRequirements = "HotChocolate.Types.ObjectField.Requirements";
}
