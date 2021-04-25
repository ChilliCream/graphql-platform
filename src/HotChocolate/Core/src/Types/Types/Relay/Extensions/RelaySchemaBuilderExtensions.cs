using HotChocolate.Types.Relay;
using static HotChocolate.Types.WellKnownContextData;

#nullable enable

namespace HotChocolate
{
    public static class IdSchemaBuilderExtensions
    {
        /// <summary>
        /// Enables relay schema style.
        /// </summary>
        public static ISchemaBuilder EnableRelaySupport(
            this ISchemaBuilder schemaBuilder,
            RelayOptions? options = null)
        {
            options ??= new();

            if (options.AddQueryFieldToMutationPayloads)
            {
                schemaBuilder.TryAddTypeInterceptor<QueryFieldTypeInterceptor>();
            }

            return schemaBuilder
                .SetContextData(IsRelaySupportEnabled, 1)
                .SetRelayOptions(options)
                .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
                .AddType<NodeType>();
        }
    }
}
