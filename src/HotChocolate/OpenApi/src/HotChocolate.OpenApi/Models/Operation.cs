using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Models;

public class Operation
{
    public string OperationId { get; set; }

    public string Description { get; set; }

    public string Path { get; set; }

    public HttpMethod Method { get; set; }

    public OpenApiOperation OpenApiOperation { get; set; }

    public OpenApiRequestBody? Request { get; set; }

    public OpenApiResponse? Response { get; set; }

    public List<OpenApiParameter> Parameter { get; set; } = new();
}
