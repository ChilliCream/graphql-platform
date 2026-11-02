using HotChocolate.Language;

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
    /// <param name="onError">
    /// The error handling mode requested by the source schema.
    /// </param>
    public InMemorySourceSchemaClientConfiguration(
        string name,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        ErrorHandlingMode? onError = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        SupportedOperations = supportedOperations;
        OnError = onError;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public SupportedOperationType SupportedOperations { get; }

    /// <summary>
    /// Gets the error handling mode requested by the source schema.
    /// </summary>
    public ErrorHandlingMode? OnError { get; }
}
