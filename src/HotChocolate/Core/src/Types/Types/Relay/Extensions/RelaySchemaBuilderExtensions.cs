using HotChocolate.Types.Relay;
using static HotChocolate.Types.WellKnownContextData;

namespace HotChocolate
{
    public static class IdSchemaBuilderExtensions
    {
        /// <summary>
        /// Enables relay schema style.
        /// </summary>
        public static ISchemaBuilder EnableRelaySupport(
            this ISchemaBuilder schemaBuilder,
            RelayOptions options = null) =>
            schemaBuilder
                .SetContextData(IsRelaySupportEnabled, 1)
                .SetRelayOptions(options ?? new RelayOptions())
                .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
                .TryAddTypeInterceptor<QueryFieldTypeInterceptor>()
                .AddType<NodeType>();
    }
}
