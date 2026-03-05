namespace HotChocolate.Adapters.OpenApi;

public interface IDynamicOpenApiDocumentTransformer
{
    void AddDefinitions(
        OpenApiEndpointDefinition[] endpoints,
        OpenApiModelDefinition[] models,
        IDictionary<string, OpenApiModelDefinition> modelsByName,
        ISchemaDefinition schema);
}
