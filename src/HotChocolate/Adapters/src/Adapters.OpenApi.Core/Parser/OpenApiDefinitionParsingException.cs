namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// The exception that is thrown when parsing an OpenAPI definition fails.
/// </summary>
public sealed class OpenApiDefinitionParsingException(string message) : Exception(message);
