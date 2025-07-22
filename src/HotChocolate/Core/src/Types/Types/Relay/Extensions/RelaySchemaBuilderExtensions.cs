using HotChocolate.Types.Relay;

#nullable enable

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

        return AddGlobalObjectIdentification(schemaBuilder, true);
    }

    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    public static ISchemaBuilder AddGlobalObjectIdentification(
        this ISchemaBuilder schemaBuilder,
        bool registerNodeInterface)
    {
        ArgumentNullException.ThrowIfNull(schemaBuilder);

        schemaBuilder.Features.Set(new NodeSchemaFeature());

        schemaBuilder.TryAddTypeInterceptor<NodeIdSerializerTypeInterceptor>();

        if (registerNodeInterface)
        {
            schemaBuilder
                .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
                .TryAddTypeInterceptor<NodeResolverTypeInterceptor>()
                .AddType<NodeType>();
        }

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
