using HotChocolate.OpenApi.Models;
using HotChocolate.Skimmed;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperContext
{
    private readonly Dictionary<OpenApiSchema, SchemaTypeInfo> _schemaTypeInfos = new();

    public OpenApiDocument OpenApiDocument { get; }
    public Dictionary<string, Operation> Operations { get; } = new();
    public Dictionary<string, InputObjectType> OperationInputTypeLookup { get; } = new();
    public Dictionary<string, INamedType> OperationPayloadTypeLookup { get; } = new();

    public Skimmed.Schema SkimmedSchema { get; } = new();

    public OpenApiWrapperContext(OpenApiDocument openApiDocument)
    {
        OpenApiDocument = openApiDocument;
    }

    public SchemaTypeInfo GetSchemaTypeInfo(OpenApiSchema schema)
    {
       if (_schemaTypeInfos.TryGetValue(schema, out var typeInfo))
       {
           return typeInfo;
       }

       var newTypeInfo = new SchemaTypeInfo(schema);
       _schemaTypeInfos.Add(schema, newTypeInfo);
       return newTypeInfo;
    }
}
