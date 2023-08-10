using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Models;
/// <summary>
/// Container class which represents all necessary data
/// of a rest operation
/// </summary>
internal sealed class Operation
{
    private readonly List<OpenApiParameter> _parameters = new();

    /// <summary>
    /// Id of the operation
    /// </summary>
    public string OperationId { get; set; }

    /// <summary>
    /// An optional description of the operation
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Relative url path e.g. /pets
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Http method e.g. POST
    /// </summary>
    public HttpMethod Method { get; set; }

    /// <summary>
    /// Direct reference to the deserialized <see cref="OpenApiOperation"/>
    /// </summary>
    public OpenApiOperation OpenApiOperation { get; set; }

    /// <summary>
    /// A list of all parameter which can be part of url query, header, cookie
    /// </summary>
    public IReadOnlyList<OpenApiParameter> Parameters => _parameters;

    /// <summary>
    /// Optional request body e.g some json for post operations
    /// </summary>
    public OpenApiRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Direct reference to the deserialized <see cref="OpenApiResponse"/>
    /// </summary>
    public OpenApiResponse? Response { get; set; }

    public Operation(string operationId, string path, HttpMethod method, OpenApiOperation openApiOperation)
    {
        OperationId = operationId;
        Path = path;
        Method = method;
        OpenApiOperation = openApiOperation;
    }

    /// <summary>
    /// Adds a parameter to the parameter list
    /// </summary>
    /// <param name="parameter"></param>
    public void AddParameter(OpenApiParameter parameter) => _parameters.Add(parameter);
}
