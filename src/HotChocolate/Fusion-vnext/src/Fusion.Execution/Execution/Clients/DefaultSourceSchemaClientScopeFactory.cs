using HotChocolate.Caching.Memory;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class DefaultSourceSchemaClientScopeFactory : ISourceSchemaClientScopeFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DefaultSourceSchemaClientScopeFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClientFactory = httpClientFactory;
    }

    public ISourceSchemaClientScope CreateScope(ISchemaDefinition schemaDefinition)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinition);

        if (schemaDefinition is not FusionSchemaDefinition schema)
        {
            throw new ArgumentException(
                "The schema definition must be a FusionSchemaDefinition.",
                nameof(schemaDefinition));
        }

        var operationCache = schema.Services.GetRequiredService<Cache<string>>();
        return new DefaultSourceSchemaClientScope(schema, _httpClientFactory, operationCache);
    }
}
