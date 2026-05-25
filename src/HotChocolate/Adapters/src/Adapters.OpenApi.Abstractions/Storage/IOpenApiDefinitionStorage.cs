using HotChocolate.Adapters.OpenApi.Storage;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Provides access to OpenAPI definitions with change notification support.
/// Implementations can retrieve definitions from various sources (file system, database, etc.).
/// The Hot Chocolate OpenAPI adapter will observe the <see cref="IOpenApiDefinitionStorage"/>
/// and when changes are detected will phase in new definitions, update definitions, or phase out
/// definitions that have been removed from the storage.
/// </summary>
public interface IOpenApiDefinitionStorage
    : IObservable<OpenApiDefinitionStorageEventArgs>
{
    /// <summary>
    /// Gets all definitions from the storage.
    /// </summary>
    ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
}
