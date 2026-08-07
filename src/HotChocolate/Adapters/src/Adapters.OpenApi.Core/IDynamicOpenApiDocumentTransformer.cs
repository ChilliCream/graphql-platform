namespace HotChocolate.Adapters.OpenApi;

internal interface IDynamicOpenApiDocumentTransformer
{
    void AddDefinitions(
        OpenApiEndpointDefinition[] endpoints,
        OpenApiModelDefinition[] models,
        IDictionary<string, OpenApiModelDefinition> modelsByName,
        ISchemaDefinition schema);
}
