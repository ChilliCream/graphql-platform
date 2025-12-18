namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents a validation error that occurred while validating an OpenAPI definition.
/// </summary>
public sealed class OpenApiDefinitionValidationError : IOpenApiError
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiDefinitionValidationError" />.
    /// </summary>
    public OpenApiDefinitionValidationError(string message, IOpenApiDefinition definition)
    {
        Message = message;
        Definition = definition;
    }

    /// <inheritdoc />
    public string Message { get; }

    /// <summary>
    /// Gets the definition where the error occurred.
    /// </summary>
    public IOpenApiDefinition Definition { get; }

    public override string ToString()
    {
        return Message;
    }
}
