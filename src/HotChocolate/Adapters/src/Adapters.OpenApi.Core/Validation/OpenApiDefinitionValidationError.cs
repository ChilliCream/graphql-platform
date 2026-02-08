namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents a validation error that occurred while validating an OpenAPI definition.
/// </summary>
public sealed class OpenApiDefinitionValidationError(string message, IOpenApiDefinition definition)
{
    public string Message { get; } = message;

    public IOpenApiDefinition Definition { get; } = definition;

    public override string ToString()
    {
        return Message;
    }
}
