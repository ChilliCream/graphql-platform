using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Models;

internal sealed class Operation
{
    private readonly List<OpenApiParameter> _parameters = new();

    public string OperationId { get; set; }

    public string? Description { get; set; }

    public string Path { get; set; }

    public HttpMethod Method { get; set; }

    public OpenApiOperation OpenApiOperation { get; set; }

    public IReadOnlyList<OpenApiParameter> Parameters => _parameters;

    public OpenApiRequestBody? RequestBody { get; set; }

    public OpenApiResponse? Response { get; set; }

    public Operation(string operationId, string path, HttpMethod method, OpenApiOperation openApiOperation)
    {
        OperationId = operationId;
        Path = path;
        Method = method;
        OpenApiOperation = openApiOperation;
    }

    public void AddParameter(OpenApiParameter parameter) => _parameters.Add(parameter);
}
