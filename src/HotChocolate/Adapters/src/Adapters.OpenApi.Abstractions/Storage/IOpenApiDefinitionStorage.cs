namespace HotChocolate.Adapters.OpenApi;

public interface IOpenApiDefinitionStorage
{
    /// <summary>
    /// Gets all definitions from the storage.
    /// </summary>
    ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event that is raised when the storage contents have changed.
    /// </summary>
    event EventHandler? Changed;
}
