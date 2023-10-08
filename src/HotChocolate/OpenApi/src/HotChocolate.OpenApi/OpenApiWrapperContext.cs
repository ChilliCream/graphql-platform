using HotChocolate.Skimmed;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperContext( string clientName, OpenApiDocument openApiDocument)
{
    private readonly Dictionary<OpenApiSchema, SchemaTypeInfo> _schemaTypeInfos = new();

    public OpenApiDocument OpenApiDocument { get; } = openApiDocument;
    public string ClientName { get; } = clientName;
    public Dictionary<string, Operation> Operations { get; } = new();
    public Dictionary<string, InputObjectType> OperationInputTypeLookup { get; } = new();
    public Dictionary<string, INamedType> OperationPayloadTypeLookup { get; } = new();
    public Skimmed.Schema MutableSchema { get; } = new();

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
