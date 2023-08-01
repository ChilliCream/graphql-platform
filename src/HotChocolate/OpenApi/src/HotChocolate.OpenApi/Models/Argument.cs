using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Models;

internal sealed class Argument
{
    public OpenApiParameter? Parameter { get; set; }

    public OpenApiRequestBody? RequestBody { get; set; }
}
