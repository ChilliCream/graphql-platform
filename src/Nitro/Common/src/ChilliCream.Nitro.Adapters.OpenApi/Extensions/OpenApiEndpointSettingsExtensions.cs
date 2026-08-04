using ChilliCream.Nitro.Adapters.OpenApi.Serialization;
using HotChocolate.Adapters.OpenApi;

namespace ChilliCream.Nitro.Adapters.OpenApi.Extensions;

public static class OpenApiEndpointSettingsExtensions
{
    extension(OpenApiEndpointSettings)
    {
        public static OpenApiEndpointSettings From(OpenApiEndpointDefinition openApiEndpointDefinition)
            => new(
                openApiEndpointDefinition.Description,
                openApiEndpointDefinition.RouteParameters,
                openApiEndpointDefinition.QueryParameters,
                openApiEndpointDefinition.BodyVariableName);
    }
}
