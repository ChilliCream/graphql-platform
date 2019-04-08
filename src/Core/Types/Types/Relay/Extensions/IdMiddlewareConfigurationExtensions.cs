using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Relay;

namespace HotChocolate
{
    public static class IdMiddlewareConfigurationExtensions
    {
        public static IMiddlewareConfiguration UseGlobalObjectIdentifier(
            this IMiddlewareConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return configuration.Use<IdMiddleware>();
        }
    }
}
