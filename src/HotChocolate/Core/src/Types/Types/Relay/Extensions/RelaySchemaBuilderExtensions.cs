using HotChocolate.Features;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;

namespace HotChocolate;

public static class RelaySchemaBuilderExtensions
{
    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    public static ISchemaBuilder AddGlobalObjectIdentification(
        this ISchemaBuilder schemaBuilder)
    {
        ArgumentNullException.ThrowIfNull(schemaBuilder);

        return AddGlobalObjectIdentification(schemaBuilder, null);
    }

    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    public static ISchemaBuilder AddGlobalObjectIdentification(
        this ISchemaBuilder schemaBuilder,
        Action<GlobalObjectIdentificationOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(schemaBuilder);

        var feature = schemaBuilder.Features.GetOrSet(new NodeSchemaFeature());

        schemaBuilder.TryAddTypeInterceptor<NodeIdSerializerTypeInterceptor>();

        configure?.Invoke(feature.Options);

        // TODO: Move this and only do if RegisterNodeInterface is true
        schemaBuilder
            .TryAddTypeInterceptor(new NodeFieldTypeInterceptor(feature.Options))
            .TryAddTypeInterceptor<NodeResolverTypeInterceptor>()
            .AddType<NodeType>();

        return schemaBuilder;
    }

    /// <summary>
    /// Enables rewriting of mutation payloads to provide access to a query root field.
    /// </summary>
    public static ISchemaBuilder AddQueryFieldToMutationPayloads(
        this ISchemaBuilder schemaBuilder,
        Action<MutationPayloadOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(schemaBuilder);

        return schemaBuilder
            .ModifyMutationPayloadOptions(configureOptions)
            .TryAddTypeInterceptor<QueryFieldTypeInterceptor>();
    }
}

// TODO: Move
public sealed class GlobalObjectIdentificationOptions
{
    /// <summary>
    /// Specifies if the node interface and the node field shall be registered with the schema.
    /// </summary>
    public bool RegisterNodeInterface { get; set; } = true;

    /// <summary>
    /// Defines if the schema building process shall validate that all nodes are resolvable through `node`.
    /// </summary>
    public bool EnsureAllNodesCanBeResolved { get; set; } = true;

    /// <summary>
    /// Specifies the maximum allowed nodes that can be fetched at once through the nodes field.
    /// </summary>
    public int MaxAllowedNodeBatchSize { get; set; } = 50;

    /// <summary>
    /// Specifies whether a plural <c>Query.nodes</c> field shall be added to the schema.
    /// </summary>
    public bool AddNodesField { get; set; } = true;

    /// <summary>
    /// Specifies whether the <c>Query.node</c> field should be annotated
    /// with the composite schema <c>@lookup</c> directive.
    /// </summary>
    /// <remarks>
    /// This is necessary if you want to enable the global object identification
    /// integration in the Fusion gateway.
    /// </remarks>
    public bool MarkNodeFieldAsLookup { get; set; }
}
