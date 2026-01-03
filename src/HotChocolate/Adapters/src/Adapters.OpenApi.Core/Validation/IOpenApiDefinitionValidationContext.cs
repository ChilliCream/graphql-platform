namespace HotChocolate.Adapters.OpenApi;

public interface IOpenApiDefinitionValidationContext
{
    ISchemaDefinition Schema { get; }
}
