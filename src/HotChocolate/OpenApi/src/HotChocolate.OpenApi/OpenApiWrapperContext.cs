using HotChocolate.Execution.Configuration;
using HotChocolate.OpenApi.Models;
using HotChocolate.Skimmed;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperContext
{
    public OpenApiDocument OpenApiDocument { get; }
    public Dictionary<string, Operation> Operations { get; } = new();

    public Dictionary<string, InputObjectType> OperationInputTypeLookup { get; } = new();
    public Dictionary<string, INamedType> OperationPayloadTypeLookup { get; } = new();

    public Skimmed.Schema SkimmedSchema { get; } = new();

    public OpenApiWrapperContext(OpenApiDocument openApiDocument)
    {
        OpenApiDocument = openApiDocument;
    }
}
