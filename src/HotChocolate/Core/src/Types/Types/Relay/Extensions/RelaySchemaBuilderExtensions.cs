using HotChocolate.Types.Relay;
using static HotChocolate.WellKnownContextData;

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
        => AddGlobalObjectIdentification(schemaBuilder, true);

    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    public static ISchemaBuilder AddGlobalObjectIdentification(
        this ISchemaBuilder schemaBuilder,
        bool registerNodeInterface)
    {
        schemaBuilder.SetContextData(GlobalIdSupportEnabled, 1);

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
        MutationPayloadOptions options = new();

        configureOptions?.Invoke(options);

        return schemaBuilder.AddQueryFieldToMutationPayloads(options);
    }

    private static ISchemaBuilder AddQueryFieldToMutationPayloads(
        this ISchemaBuilder schemaBuilder,
        MutationPayloadOptions options)
    {
        return schemaBuilder
            .SetMutationPayloadOptions(options)
            .TryAddTypeInterceptor<QueryFieldTypeInterceptor>();
    }
}
