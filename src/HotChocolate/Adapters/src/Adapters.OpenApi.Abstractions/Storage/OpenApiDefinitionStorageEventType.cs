namespace HotChocolate.Adapters.OpenApi.Storage;

/// <summary>
/// Defines the types of changes that can occur in OpenAPI definition storage.
/// </summary>
public enum OpenApiDefinitionStorageEventType
{
    /// <summary>A definition was added to or updated in storage.</summary>
    Updated,

    /// <summary>A definition was removed from storage.</summary>
    Removed
}
