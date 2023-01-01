using System;
using HotChocolate.Types.Relay;
using static HotChocolate.WellKnownContextData;

#nullable enable

namespace HotChocolate;

public static class RelaySchemaBuilderExtensions
{
    /// <summary>
    /// Enables relay schema style.
    /// </summary>
    [Obsolete("Use AddGlobalObjectIdentification / AddQueryFieldToMutationPayloads")]
    public static ISchemaBuilder EnableRelaySupport(
        this ISchemaBuilder schemaBuilder,
        RelayOptions? options = null)
    {
        options ??= new();

        if (options.AddQueryFieldToMutationPayloads)
        {
            MutationPayloadOptions payloadOptions = new()
            {
                QueryFieldName = options.QueryFieldName,
                MutationPayloadPredicate = options.MutationPayloadPredicate
            };

            schemaBuilder.AddQueryFieldToMutationPayloads(payloadOptions);
        }

        return schemaBuilder
            .SetContextData(IsRelaySupportEnabled, 1)
            .AddGlobalObjectIdentification();
    }

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
