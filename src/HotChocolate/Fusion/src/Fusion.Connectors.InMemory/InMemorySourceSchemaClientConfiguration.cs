namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Configuration for an in-memory source schema client that executes
/// GraphQL operations directly in-process.
/// </summary>
public sealed class InMemorySourceSchemaClientConfiguration : ISourceSchemaClientConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="InMemorySourceSchemaClientConfiguration"/>.
    /// </summary>
    /// <param name="name">The name of the source schema.</param>
    /// <param name="supportedOperations">The supported operation types.</param>
    public InMemorySourceSchemaClientConfiguration(
        string name,
        SupportedOperationType supportedOperations = SupportedOperationType.All)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        SupportedOperations = supportedOperations;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public SupportedOperationType SupportedOperations { get; }
}
