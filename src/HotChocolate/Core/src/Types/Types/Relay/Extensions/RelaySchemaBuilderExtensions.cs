using HotChocolate.Types.Relay;

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
                .SetContextData(RelayConstants.IsRelaySupportEnabled, 1)
                .TryAddTypeInterceptor<NodeFieldTypeInterceptor>()
                .AddType<NodeType>();
    }
}
