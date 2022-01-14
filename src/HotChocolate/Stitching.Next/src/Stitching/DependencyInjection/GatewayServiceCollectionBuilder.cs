using System;
using HotChocolate;
using HotChocolate.Stitching.SchemaBuilding;

namespace Microsoft.Extensions.DependencyInjection;

public static class GatewayServiceCollectionBuilder
{
    public static IGatewayBuilder AddGraphQLGateway(
        this IServiceCollection services,
        NameString schemaName = default)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return new DefaultGatewayBuilder(services.AddGraphQL(schemaName));
    }
}