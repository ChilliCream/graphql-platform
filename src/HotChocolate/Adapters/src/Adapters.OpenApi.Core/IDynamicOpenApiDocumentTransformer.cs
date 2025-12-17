namespace HotChocolate.Adapters.OpenApi;

public interface IDynamicOpenApiDocumentTransformer
{
    void AddDefinitions(
        OpenApiEndpointDefinition[] endpoints,
        OpenApiModelDefinition[] models,
        ISchemaDefinition schema);
}
