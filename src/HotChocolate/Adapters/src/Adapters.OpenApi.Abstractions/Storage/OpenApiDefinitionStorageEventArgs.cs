namespace HotChocolate.Adapters.OpenApi.Storage;

/// <summary>
/// Event arguments for OpenAPI definition storage changes.
/// </summary>
/// <param name="Name">The name (id) of the definition that changed.</param>
/// <param name="Type">The type of change that occurred.</param>
/// <param name="Definition">The definition. Required for Updated, null for Removed.</param>
public record OpenApiDefinitionStorageEventArgs(
    string Name,
    OpenApiDefinitionStorageEventType Type,
    IOpenApiDefinition? Definition = null);
