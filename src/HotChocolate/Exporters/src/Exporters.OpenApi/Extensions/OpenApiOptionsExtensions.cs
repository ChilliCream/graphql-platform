using Microsoft.AspNetCore.OpenApi;

namespace HotChocolate.Exporters.OpenApi.Extensions;

public static class OpenApiOptionsExtensions
{
    // TODO: Better name
    public static OpenApiOptions AddGraphQL(this OpenApiOptions options)
    {
        return options;
    }
}
