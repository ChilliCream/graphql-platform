using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Relay;

namespace HotChocolate
{
    public static class IdMiddlewareConfigurationExtensions
    {
        public static ISchemaConfiguration UseGlobalObjectIdentifier(
            this ISchemaConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return configuration.Use<IdMiddleware>();
        }
    }
}
