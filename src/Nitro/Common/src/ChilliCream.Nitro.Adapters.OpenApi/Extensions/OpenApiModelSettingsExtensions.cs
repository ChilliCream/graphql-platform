using ChilliCream.Nitro.Adapters.OpenApi.Serialization;
using HotChocolate.Adapters.OpenApi;

namespace ChilliCream.Nitro.Adapters.OpenApi.Extensions;

public static class OpenApiModelSettingsExtensions
{
    extension(OpenApiModelSettings)
    {
        public static OpenApiModelSettings From(OpenApiModelDefinition openApiModelDefinition)
            => new(openApiModelDefinition.Description);
    }
}
