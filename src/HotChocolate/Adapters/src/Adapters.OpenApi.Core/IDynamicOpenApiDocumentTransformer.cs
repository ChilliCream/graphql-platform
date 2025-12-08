namespace HotChocolate.Adapters.OpenApi;

public interface IDynamicOpenApiDocumentTransformer
{
    void AddDocuments(
        OpenApiOperationDocument[] operations,
        OpenApiFragmentDocument[] fragments,
        ISchemaDefinition schema);
}
