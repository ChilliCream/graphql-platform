namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents the initialization options for a request executor.
/// </summary>
public struct RequestExecutorInitializationOptions
{
    /// <summary>
    /// Gets or sets the warmup task that shall be executed on a new executor.
    /// </summary>
    public Func<IRequestExecutor, CancellationToken, Task>? Warmup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the warmup task shall be executed after eviction and
    /// keep executor in-memory.
    /// </summary>
    public bool KeepWarm { get; set; }

    /// <summary>
    /// Gets or sets the schema file initialization options.
    /// </summary>
    public SchemaFileInitializationOptions WriteSchemaFile { get; set; }
}
