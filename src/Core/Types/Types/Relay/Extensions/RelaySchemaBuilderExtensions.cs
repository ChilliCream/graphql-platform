using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Relay;

namespace HotChocolate
{
    public static class IdSchemaBuilderExtensions
    {
        public static ISchemaBuilder EnableRelaySupport(
            this ISchemaBuilder schemaBuilder)
        {
            return schemaBuilder
                .UseGlobalObjectIdentifier()
                .SetContextData(RelayConstants.IsRelaySupportEnabled, 1);
        }

        public static ISchemaBuilder UseGlobalObjectIdentifier(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Use<IdMiddleware>();
        }
    }
}
