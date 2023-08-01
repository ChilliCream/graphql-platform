using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Models;

internal sealed class Operation
{
    public string OperationId { get; set; }

    public string Description { get; set; }

    public string Path { get; set; }

    public HttpMethod Method { get; set; }

    public OpenApiOperation OpenApiOperation { get; set; }


    public List<Argument>? Arguments { get; set; }

    public OpenApiResponse? Response { get; set; }
}
