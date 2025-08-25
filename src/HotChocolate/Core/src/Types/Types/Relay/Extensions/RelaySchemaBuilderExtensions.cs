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
        configure?.Invoke(feature.Options);

        schemaBuilder.TryAddTypeInterceptor<NodeIdSerializerTypeInterceptor>();

        schemaBuilder
            .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
            .TryAddTypeInterceptor<NodeResolverTypeInterceptor>();

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
