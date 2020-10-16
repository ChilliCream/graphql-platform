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
            this ISchemaBuilder schemaBuilder) =>
            schemaBuilder
                .SetContextData(IsRelaySupportEnabled, 1)
                .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
                .AddType<NodeType>();
    }
}
