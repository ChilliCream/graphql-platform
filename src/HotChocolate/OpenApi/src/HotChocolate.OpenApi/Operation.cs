using System.Runtime.InteropServices;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;
/// <summary>
/// Container class which represents all necessary data
/// of a rest operation
/// </summary>
internal sealed class Operation(string operationId, string path, HttpMethod method, OpenApiOperation openApiOperation)
{
    private readonly List<OpenApiParameter> _parameters = [];

    /// <summary>
    /// Id of the operation
    /// </summary>
    public string OperationId { get; set; } = operationId;

    /// <summary>
    /// An optional description of the operation
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Relative url path e.g. /pets
    /// </summary>
    public string Path { get; set; } = path;

    /// <summary>
    /// Http method e.g. POST
    /// </summary>
    public HttpMethod Method { get; set; } = method;

    /// <summary>
    /// Direct reference to the deserialized <see cref="OpenApiOperation"/>
    /// </summary>
    public OpenApiOperation OpenApiOperation { get; set; } = openApiOperation;

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

    /// <summary>
    /// Adds a parameter to the parameter list
    /// </summary>
    /// <param name="parameter"></param>
    public void AddParameter(OpenApiParameter parameter) => _parameters.Add(parameter);

    internal ref OpenApiParameter GetParameterRef()
        => ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(_parameters));
}
