namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents a validation error that occurred while validating an OpenAPI definition.
/// </summary>
public sealed class OpenApiDefinitionValidationError(string message)
{
    public string Message { get; } = message;

    public override string ToString()
    {
        return Message;
    }
}
